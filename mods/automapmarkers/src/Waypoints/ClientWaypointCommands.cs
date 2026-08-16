using Egocarib.AutoMapMarkers.Patches;
using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static Egocarib.AutoMapMarkers.Settings.MapMarkerConfig.Settings;

namespace Egocarib.AutoMapMarkers.Waypoints
{
    /// <summary>
    /// Creates and deletes waypoints from the client alone, using the vanilla waypoint chat commands.
    /// This is the path used when the mod isn't installed on the server.
    /// </summary>
    /// <remarks>
    /// Waypoints are owned by the server and there is no client-authoritative API for them, but the
    /// game's own "Add waypoint" dialog creates them by sending a chat command - so the same commands
    /// work from a mod with nothing installed server-side:
    ///
    ///     /waypoint addati [icon] =[x] =[y] =[z] [pinned] [color] [title]
    ///     /waypoint remove [index]
    ///
    /// The "=" prefix marks the coordinate as absolute rather than relative to world spawn.
    ///
    /// This mirrors the decision logic of <see cref="WaypointUtil"/>, which does the same job on the
    /// server when the mod is installed there.
    /// </remarks>
    public static class ClientWaypointCommands
    {
        private const int SendIntervalMs = 150;
        private const int MaxQueuedCommands = 50;

        private class QueuedCommand
        {
            public string Command;
            public bool SuppressServerReply;
        }

        private static readonly List<QueuedCommand> commandQueue = new List<QueuedCommand>();
        private static long tickListenerId;
        private static bool queueOverflowLogged;

        /// <summary>
        /// Starts the outgoing command queue. Chat isn't a bulk transport, and a single scythe swing
        /// can produce several markers at once, so commands are drained at a fixed rate.
        /// </summary>
        public static void Initialize(ICoreClientAPI capi)
        {
            if (capi == null)
                return;

            // Deferred to LevelFinalize because there is no world to tick against yet at mod startup.
            capi.Event.LevelFinalize += () =>
            {
                if (tickListenerId == 0)
                    tickListenerId = capi.Event.RegisterGameTickListener(DrainQueue, SendIntervalMs);
            };
        }

        public static void Dispose(ICoreClientAPI capi)
        {
            if (capi != null && tickListenerId != 0)
                capi.Event.UnregisterGameTickListener(tickListenerId);
            tickListenerId = 0;
            commandQueue.Clear();
            queueOverflowLogged = false;
        }

        /// <summary>
        /// Parses the provided map marker settings and determines whether a new waypoint should be
        /// added at the specified coordinates, then asks the server to create it via chat command.
        /// Client-side counterpart of <see cref="WaypointUtil.AddWaypoint"/>.
        /// </summary>
        public static void AddWaypoint(Vec3d position, AutoMapMarkerSetting settings, bool sendChatMessageToPlayer, string dynamicTitleComponent, bool includeCoordinates)
        {
            ICoreClientAPI capi = MapMarkerMod.CoreClientAPI;
            if (capi == null)
                return;
            if (position == null || settings == null)
            {
                MessageUtil.LogError("Unable to create map marker - missing position or settings data.");
                return;
            }
            if (!settings.Enabled)
            {
                return;
            }

            string title = WaypointUtil.FormatDynamicTitle(settings.MarkerTitle, dynamicTitleComponent);

            if (includeCoordinates)
            {
                Vec3d playerFriendlyPos = position.Clone().Sub(capi.World.DefaultSpawnPosition.AsBlockPos);
                title = $"{title}  [{playerFriendlyPos.XInt}, {position.YInt}, {playerFriendlyPos.ZInt}]";
            }

            title = SanitizeTitle(title);
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(settings.MarkerIcon))
            {
                MessageUtil.LogError("Unable to create map marker - missing title or icon.");
                return;
            }

            string icon = settings.MarkerIcon.Trim();
            if (icon.Any(char.IsWhiteSpace))
            {
                MessageUtil.LogError($"Unable to create map marker - icon name \"{icon}\" contains whitespace.");
                return;
            }

            string color = SanitizeColor(settings);
            if (color == null)
            {
                MessageUtil.LogError("Unable to create map marker - invalid color.");
                return;
            }

            if (IsCoveredByExistingWaypoint(position, title, icon, settings))
            {
                return;
            }

            // "/waypoint addati [icon] =[x] =[y] =[z] [pinned] [color] [title]"
            string command = string.Format(CultureInfo.InvariantCulture,
                "/waypoint addati {0} ={1} ={2} ={3} {4} {5} {6}",
                icon,
                position.X.ToString("0.##", CultureInfo.InvariantCulture),
                position.Y.ToString("0.##", CultureInfo.InvariantCulture),
                position.Z.ToString("0.##", CultureInfo.InvariantCulture),
                settings.MarkerPinned,  // "True"/"False", matching what the game's own dialog sends
                color,
                title);

            ClientWaypointAccess.AddPending(position, title, icon, settings.MarkerColorInteger);
            Enqueue(command, sendChatMessageToPlayer);
        }

        /// <summary>
        /// Finds the waypoint nearest to the specified position (or the player's current location if
        /// null), optionally filtering by title pattern and max radius, then asks the server to
        /// delete it. Client-side counterpart of <see cref="WaypointUtil.DeleteNearestWaypoint"/>.
        /// </summary>
        public static void DeleteNearestWaypoint(bool sendChatMessageToPlayer,
            Vec3d targetPosition = null, string titlePattern = null, double maxRadius = double.MaxValue)
        {
            ICoreClientAPI capi = MapMarkerMod.CoreClientAPI;
            if (capi?.World?.Player?.Entity == null)
                return;

            Vec3d searchPosition = targetPosition ?? capi.World.Player.Entity.Pos.XYZ;
            List<Waypoint> ownWaypoints = ClientWaypointAccess.GetOwnWaypoints();

            double closestWpDistance = double.MaxValue;
            int closestWpIndex = -1;

            for (int i = 0; i < ownWaypoints.Count; i++)
            {
                Waypoint wp = ownWaypoints[i];
                if (wp.Position == null)
                    continue;

                if (ClientWaypointAccess.IsPendingDeletion(wp))
                    continue;  // Already queued for removal - don't delete it twice

                if (titlePattern != null && (wp.Title == null || !wp.Title.Contains(titlePattern)))
                    continue;

                double thisDist = searchPosition.DistanceTo(wp.Position);
                if (thisDist > maxRadius)
                    continue;

                if (thisDist < closestWpDistance)
                {
                    closestWpDistance = thisDist;
                    closestWpIndex = i;
                }
            }

            if (closestWpIndex < 0)
            {
                if (titlePattern == null && maxRadius == double.MaxValue)
                {
                    MessageUtil.Log("Tried to delete the nearest map marker, but no waypoints owned by this player were found.");
                }
                return;
            }

            Waypoint target = ownWaypoints[closestWpIndex];

            // The server indexes into this same owner-filtered list, but it will have processed any
            // deletion we queued earlier first, and every removal shifts the later indices down one.
            int commandIndex = closestWpIndex;
            for (int i = 0; i < closestWpIndex; i++)
            {
                if (ClientWaypointAccess.IsPendingDeletion(ownWaypoints[i]))
                    commandIndex--;
            }

            ClientWaypointAccess.AddPendingDeletion(target);
            ClientWaypointAccess.DropPendingNear(target.Position, 1.0);
            Enqueue($"/waypoint remove {commandIndex}", sendChatMessageToPlayer);
        }

        /// <summary>
        /// Applies the same "don't mark the same spot twice" rule as WaypointUtil.AddWaypoint, but
        /// against the player's client-synced waypoints plus any waypoint we've requested and the
        /// server hasn't echoed back yet.
        /// </summary>
        private static bool IsCoveredByExistingWaypoint(Vec3d position, string title, string icon, AutoMapMarkerSetting settings)
        {
            int? settingColor = settings.MarkerColorInteger;

            foreach (Waypoint waypoint in ClientWaypointAccess.GetOwnWaypoints())
            {
                if (waypoint.Position == null)
                    continue;
                if (ClientWaypointAccess.IsPendingDeletion(waypoint))
                    continue;  // On its way out, so it shouldn't block a new marker here
                if (!WithinCoverage(waypoint.Position, position, settings.MarkerCoverageRadius))
                    continue;
                if (WaypointUtil.MatchingWaypointTitle(waypoint.Title, title)
                    && waypoint.Icon == icon
                    && (settingColor == null || waypoint.Color == settingColor))
                {
                    return true;
                }
            }

            foreach (var pending in ClientWaypointAccess.GetPending())
            {
                if (!WithinCoverage(pending.Position, position, settings.MarkerCoverageRadius))
                    continue;
                if (WaypointUtil.MatchingWaypointTitle(pending.Title, title)
                    && pending.Icon == icon
                    && (settingColor == null || pending.Color == settingColor))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool WithinCoverage(Vec3d existing, Vec3d candidate, int coverageRadius)
        {
            double xDiff = Math.Abs(existing.X - candidate.X);
            double zDiff = Math.Abs(existing.Z - candidate.Z);
            return Math.Max(xDiff, zDiff) < coverageRadius;
        }

        /// <summary>
        /// The title is the trailing argument of the command, so spaces are fine, but line breaks
        /// would truncate or split the command.
        /// </summary>
        private static string SanitizeTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
                return title;
            return title.Replace('\r', ' ').Replace('\n', ' ').Trim();
        }

        /// <summary>
        /// The game's color argument accepts either a hex value or a known .NET color name, which is
        /// exactly what AutoMapMarkerSetting.MarkerColor already stores. Falls back to hex derived
        /// from the parsed color if the stored value isn't a single token.
        /// </summary>
        private static string SanitizeColor(AutoMapMarkerSetting settings)
        {
            string color = settings.MarkerColor?.Trim();
            if (!string.IsNullOrEmpty(color) && !color.Any(char.IsWhiteSpace))
                return color;

            int? colorInt = settings.MarkerColorIntegerNoAlpha;
            if (colorInt == null)
                return null;

            return "#" + (colorInt.Value & 0xFFFFFF).ToString("X6", CultureInfo.InvariantCulture);
        }

        private static void Enqueue(string command, bool sendChatMessageToPlayer)
        {
            if (commandQueue.Count >= MaxQueuedCommands)
            {
                if (!queueOverflowLogged)
                {
                    queueOverflowLogged = true;
                    MessageUtil.LogError($"Map marker command queue is full ({MaxQueuedCommands}) - dropping requests.");
                }
                return;
            }
            queueOverflowLogged = false;

            commandQueue.Add(new QueuedCommand
            {
                Command = command,
                // The server answers every waypoint command with a chat confirmation. Suppress it
                // unless the player asked to be notified, in which case the vanilla message is the
                // notification.
                SuppressServerReply = !sendChatMessageToPlayer
            });
        }

        private static void DrainQueue(float dt)
        {
            if (commandQueue.Count == 0)
                return;

            ICoreClientAPI capi = MapMarkerMod.CoreClientAPI;
            if (capi == null)
            {
                commandQueue.Clear();
                return;
            }

            QueuedCommand queued = commandQueue[0];
            commandQueue.RemoveAt(0);

            // Armed here rather than at enqueue time so the suppression window tracks the actual
            // send, not however long the command sat in the queue.
            if (queued.SuppressServerReply)
                ChatSuppression.ArmSuppression();

            capi.SendChatMessage(queued.Command, GlobalConstants.GeneralChatGroup, null);
        }
    }
}
