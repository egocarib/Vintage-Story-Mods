using Egocarib.AutoMapMarkers.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Egocarib.AutoMapMarkers.Utilities
{
    public class WaypointUtil
    {
        private readonly WaypointMapLayer WaypointMapLayer;
        private readonly IServerPlayer ServerPlayer;
        private static readonly MethodInfo ResendWaypointsMethod =
            typeof(WaypointMapLayer).GetMethod("ResendWaypoints", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo RebuildMapComponentsMethod =
            typeof(WaypointMapLayer).GetMethod("RebuildMapComponents", BindingFlags.NonPublic | BindingFlags.Instance);

        private bool Valid => ServerPlayer != null && WaypointMapLayer != null 
            && ResendWaypointsMethod != null && RebuildMapComponentsMethod != null;

        private string PlayerInfo => ServerPlayer != null ? $"{ServerPlayer.PlayerName} [{ServerPlayer.PlayerUID}]" : "NULL";

        /// <summary>
        /// Waypoint generator. To be instantiated and used only on the Server thread.
        /// </summary>
        public WaypointUtil(IServerPlayer serverPlayer)
        {
            ServerPlayer = serverPlayer;
            WorldMapManager serverWorldMapManager = MapMarkerMod.CoreServerAPI.ModLoader.GetModSystem<WorldMapManager>();
            WaypointMapLayer = serverWorldMapManager.MapLayers.FirstOrDefault((MapLayer ml) => ml is WaypointMapLayer) as WaypointMapLayer;
        }

        /// <summary>
        /// Parses the provided map marker settings and determines whether a new waypoint should be added
        /// at the specified coordinates based on those settings. Then creates the waypoint and syncs it
        /// back to the client.
        /// </summary>
        public void AddWaypoint(Vec3d position, MapMarkerConfig.Settings.AutoMapMarkerSetting settings, bool sendChatMessageToPlayer, string dynamicTitleComponent, bool includeCoordinates)
        {
            if (!Valid)
            {
                MessageUtil.LogError($"Unable to fulfill waypoint create request from player {PlayerInfo} - ServerPlayer, WaypointMapLayer, or Reflected method is inaccessible.");
                return;
            }
            if (position == null || settings == null)
            {
                MessageUtil.LogError($"Unable to fulfill waypoint create request from player {PlayerInfo} - missing position or settings data.");
                return;
            }
            if (!settings.Enabled)
            {
                return;
            }

            // If there's a dynamic component to the marker title, figure that out before creating the waypoint
            string title = $"{settings.MarkerTitle}{(dynamicTitleComponent != null ? $" ({dynamicTitleComponent})" : "")}";

            if (includeCoordinates)
            {
                Vec3d playerFriendlyPos = position.Clone().Sub(MapMarkerMod.CoreServerAPI.World.DefaultSpawnPosition.AsBlockPos);
                title = $"{title}  [{playerFriendlyPos.XInt}, {position.YInt}, {playerFriendlyPos.ZInt}]";
            }
            

            foreach (Waypoint waypoint in WaypointMapLayer.Waypoints.Where(w => w.OwningPlayerUid == ServerPlayer.PlayerUID))
            {
                double xDiff = Math.Abs(waypoint.Position.X - position.X);
                double zDiff = Math.Abs(waypoint.Position.Z - position.Z);
                if (Math.Max(xDiff, zDiff) < settings.MarkerCoverageRadius)
                {
                    bool sameTitle = waypoint.Title == title;
                    bool sameIcon = waypoint.Icon == settings.MarkerIcon;
                    if (sameTitle && sameIcon)
                    {
                        int? settingColor = settings.MarkerColorInteger;
                        if (settingColor == null || waypoint.Color == settingColor)
                        {
                            return; // Don't create another waypoint because this spot is too close to an existing waypoint
                        }
                    }
                }
            }

            AddWaypointToMap(position, title, settings.MarkerIcon, settings.MarkerColorInteger, sendChatMessageToPlayer, settings.MarkerPinned);
        }

        /// <summary>
        /// Adds a waypoint marker to the map at the specified position, using the specified parameters, and syncs it back to the client.
        /// </summary>
        private void AddWaypointToMap(Vec3d pos, string title, string icon, int? color, bool sendChatMessageToPlayer, bool pinned = false)
        {
            if (!Valid)
            {
                return;
            }
            if (pos == null || string.IsNullOrEmpty(title) || string.IsNullOrEmpty(icon))
            {
                MessageUtil.LogError("Unable to create map marker - missing position, title, or icon.");
                return;
            }
            if (color == null)
            {
                MessageUtil.LogError("Unable to create map marker - invalid color.");
                return;
            }

            Waypoint waypoint = new Waypoint()
            {
                Color = (int)color,
                OwningPlayerUid = ServerPlayer.PlayerUID,
                Position = pos,
                Title = title,
                Icon = icon,
                Pinned = pinned
            };

            WaypointMapLayer.Waypoints.Add(waypoint);

            if (sendChatMessageToPlayer)
            {
                int waypointIndex = WaypointMapLayer.Waypoints.Count(p => p.OwningPlayerUid == ServerPlayer.PlayerUID) - 1;
                MessageUtil.Chat(Lang.Get("Ok, waypoint nr. {0} added", waypointIndex), ServerPlayer);
            }
            ResendWaypoints();
        }

        /// <summary>
        /// Finds the waypoint nearest to the player's current location. Then deletes the waypoint and syncs data back to the client.
        /// </summary>
        public void DeleteNearestWaypoint(bool sendChatMessageToPlayer)
        {
            if (!Valid)
            {
                MessageUtil.LogError($"Unable to fulfill map marker delete request from player {PlayerInfo} - ServerPlayer, WaypointMapLayer, or Reflected method is inaccessible.");
                return;
            }

            double closestWpDistance = double.MaxValue;
            Waypoint closestWp = null;

            foreach (Waypoint wp in WaypointMapLayer.Waypoints.Where(w => w.OwningPlayerUid == ServerPlayer.PlayerUID))
            {
                double thisDist = ServerPlayer.Entity.Pos.DistanceTo(wp.Position);
                if (thisDist < closestWpDistance)
                {
                    closestWpDistance = thisDist;
                    closestWp = wp;
                }
            }
            if (closestWp != null)
            {
                WaypointMapLayer.Waypoints.Remove(closestWp);
                RebuildMapComponents();
                ResendWaypoints();
                if (sendChatMessageToPlayer)
                {
                    MessageUtil.Chat(Lang.Get("Ok, deleted waypoint."), ServerPlayer);
                }
            }
            else
            {
                MessageUtil.Log($"Player {PlayerInfo} tried to delete their nearest waypoint, but no waypoints owned by that player were found.");
            }
        }

        /// <summary>
        /// Resends waypoints to the client(s).
        /// </summary>
        private void ResendWaypoints()
        {
            if (Valid)
            {
                ResendWaypointsMethod.Invoke(WaypointMapLayer, new object[] { (ServerPlayer as IServerPlayer) });
            }
        }

        /// <summary>
        /// Rebuilds waypoint map components.
        /// </summary>
        private void RebuildMapComponents()
        {
            if (Valid)
            {
                RebuildMapComponentsMethod.Invoke(WaypointMapLayer, null);
            }
        }

        /// <summary>
        /// Get the default waypoint icon color array. This is intended as a fallback and is only used if
        /// the mod can't get the colors from the game's WaypointMapLayer for some reason. Copied from the
        /// private hardcoded list in Vintagestory.GameContent.WaypointMapLayer
        /// </summary>
        public static int[] GetDefaultWaypointColorOptions()
        {
            string[] hexcolors = new string[36]
            {
                "#F9D0DC", "#F179AF", "#F15A4A", "#ED272A", "#A30A35", "#FFDE98", "#EFFD5F", "#F6EA5E", "#FDBB3A", "#C8772E",
                "#F47832", "C3D941", "#9FAB3A", "#94C948", "#47B749", "#366E4F", "#516D66", "93D7E3", "#7698CF", "#20909E",
                "#14A4DD", "#204EA2", "#28417A", "#C395C4", "#92479B", "#8E007E", "#5E3896", "D9D4CE", "#AFAAA8", "#706D64",
                "#4F4C2B", "#BF9C86", "#9885530", "#5D3D21", "#FFFFFF", "#080504"
            };
            List<int> colors = new List<int>();
            for (int i = 0; i < hexcolors.Length; i++)
            {
                colors.Add(ColorUtil.Hex2Int(hexcolors[i]));
            }
            return colors.ToArray();
        }

    }
}
