using Egocarib.AutoMapMarkers.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Egocarib.AutoMapMarkers.Waypoints
{
    /// <summary>
    /// Read-only, client-side access to the vanilla waypoint map layer.
    /// </summary>
    /// <remarks>
    /// The client instance of <see cref="WaypointMapLayer"/> keeps the player's own waypoints in its
    /// public "ownWaypoints" list, populated whenever the server sends a waypoint sync. Reading it
    /// lets the client-side command path deduplicate markers and resolve waypoint indices for
    /// deletion without any server-side mod component.
    ///
    /// Because the server only re-syncs after it has processed our chat command, waypoints we just
    /// asked for are invisible here for a short while. <see cref="AddPending"/> covers that window.
    /// </remarks>
    public static class ClientWaypointAccess
    {
        /// <summary>
        /// A waypoint the client has requested but which the server has not synced back yet.
        /// </summary>
        public class PendingWaypoint
        {
            public Vec3d Position;
            public string Title;
            public string Icon;
            public int? Color;
            public long ExpiresAtMs;
        }

        private const long PendingLifetimeMs = 30000;

        private static WaypointMapLayer cachedLayer;
        private static readonly List<PendingWaypoint> pendingWaypoints = new List<PendingWaypoint>();
        private static readonly Dictionary<string, long> pendingDeletionKeys = new Dictionary<string, long>();
        private static bool layerFailureLogged;

        /// <summary>
        /// Stable identity for a waypoint across server re-syncs, which replace the Waypoint objects
        /// wholesale. Guid is server-assigned; waypoints from old saves may not have one.
        /// </summary>
        public static string KeyOf(Waypoint waypoint)
        {
            if (waypoint == null)
                return null;
            if (!string.IsNullOrEmpty(waypoint.Guid))
                return waypoint.Guid;
            return $"{waypoint.Position?.X}/{waypoint.Position?.Y}/{waypoint.Position?.Z}/{waypoint.Title}";
        }

        /// <summary>
        /// Locates the client-side waypoint map layer. Uses the same lookup as
        /// MapMarkerGUI.LoadColorOptions and WaypointUtil.
        /// </summary>
        public static WaypointMapLayer GetLayer()
        {
            if (cachedLayer != null)
                return cachedLayer;

            ICoreClientAPI capi = MapMarkerMod.CoreClientAPI;
            if (capi == null)
                return null;

            List<MapLayer> mapLayers = capi.ModLoader?.GetModSystem<WorldMapManager>()?.MapLayers;
            cachedLayer = mapLayers?.FirstOrDefault((MapLayer l) => l is WaypointMapLayer) as WaypointMapLayer;

            if (cachedLayer == null && !layerFailureLogged)
            {
                layerFailureLogged = true;
                MessageUtil.LogError("Unable to locate the game's waypoint map layer. Client-side map markers"
                    + " will still be created, but duplicate detection and marker deletion won't work.");
            }

            return cachedLayer;
        }

        /// <summary>
        /// Returns the waypoints owned by this player, in the same order the server keeps them.
        /// That order is what the "/waypoint remove [index]" command indexes into.
        /// Returns an empty list if the waypoint layer isn't reachable.
        /// </summary>
        public static List<Waypoint> GetOwnWaypoints()
        {
            var result = new List<Waypoint>();

            ICoreClientAPI capi = MapMarkerMod.CoreClientAPI;
            string playerUid = capi?.World?.Player?.PlayerUID;
            List<Waypoint> ownWaypoints = GetLayer()?.ownWaypoints;
            if (playerUid == null || ownWaypoints == null)
                return result;

            foreach (Waypoint waypoint in ownWaypoints)
            {
                if (waypoint != null && waypoint.OwningPlayerUid == playerUid)
                    result.Add(waypoint);
            }

            return result;
        }

        /// <summary>
        /// Records that we have asked the server to delete a waypoint, but the deletion hasn't been
        /// reflected in a sync yet.
        /// </summary>
        public static void AddPendingDeletion(Waypoint waypoint)
        {
            ICoreClientAPI capi = MapMarkerMod.CoreClientAPI;
            string key = KeyOf(waypoint);
            if (capi == null || key == null)
                return;

            pendingDeletionKeys[key] = capi.World.ElapsedMilliseconds + PendingLifetimeMs;
        }

        /// <summary>
        /// True if we have already asked the server to delete this waypoint. Deletion commands are
        /// queued and sent at a fixed rate, so without this a later request could target a waypoint
        /// that is already on its way out, or compute an index the earlier deletion invalidates.
        /// </summary>
        public static bool IsPendingDeletion(Waypoint waypoint)
        {
            string key = KeyOf(waypoint);
            if (key == null || !pendingDeletionKeys.TryGetValue(key, out long expiresAt))
                return false;

            ICoreClientAPI capi = MapMarkerMod.CoreClientAPI;
            if (capi == null || capi.World.ElapsedMilliseconds > expiresAt)
            {
                pendingDeletionKeys.Remove(key);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Records a waypoint we just requested, so that a second detection arriving before the
        /// server's sync doesn't create a duplicate.
        /// </summary>
        public static void AddPending(Vec3d position, string title, string icon, int? color)
        {
            ICoreClientAPI capi = MapMarkerMod.CoreClientAPI;
            if (capi == null || position == null)
                return;

            pendingWaypoints.Add(new PendingWaypoint
            {
                Position = position.Clone(),
                Title = title,
                Icon = icon,
                Color = color,
                ExpiresAtMs = capi.World.ElapsedMilliseconds + PendingLifetimeMs
            });
        }

        /// <summary>
        /// Pending waypoints that are still unconfirmed. Expired entries, and entries the server has
        /// since echoed back, are dropped as a side effect.
        /// </summary>
        public static List<PendingWaypoint> GetPending()
        {
            ICoreClientAPI capi = MapMarkerMod.CoreClientAPI;
            if (capi == null || pendingWaypoints.Count == 0)
                return pendingWaypoints;

            long now = capi.World.ElapsedMilliseconds;
            List<Waypoint> ownWaypoints = GetOwnWaypoints();

            pendingWaypoints.RemoveAll(pending =>
                pending.ExpiresAtMs <= now
                || ownWaypoints.Any(wp => wp.Position != null
                    && wp.Position.SquareDistanceTo(pending.Position) < 1.0
                    && WaypointUtil.MatchingWaypointTitle(wp.Title, pending.Title)));

            return pendingWaypoints;
        }

        /// <summary>
        /// Forgets any pending entry near the given position, used after a deletion request so a
        /// re-created marker isn't suppressed by its own stale pending record.
        /// </summary>
        public static void DropPendingNear(Vec3d position, double radius)
        {
            if (position == null)
                return;

            pendingWaypoints.RemoveAll(pending =>
                Math.Max(
                    Math.Abs(pending.Position.X - position.X),
                    Math.Abs(pending.Position.Z - position.Z)) < radius);
        }

        /// <summary>
        /// Clears cached state. Called when the mod is disposed.
        /// </summary>
        public static void Reset()
        {
            cachedLayer = null;
            pendingWaypoints.Clear();
            pendingDeletionKeys.Clear();
            layerFailureLogged = false;
        }
    }
}
