using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using System;

namespace Egocarib.AutoMapMarkers.Patches
{
    [HarmonyPatch(typeof(Vintagestory.API.Common.EntityAgent))]
    public class Boat
    {
        [ThreadStatic]
        private static IMountableSeat CapturedSeat;

        /// <summary>
        /// Captures the player's current mount before TryUnmount clears it.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("TryUnmount")]
        public static void TryUnmountPrefix(EntityAgent __instance)
        {
            CapturedSeat = null;
            if (__instance is EntityPlayer player
                && player.PlayerUID == MapMarkerMod.CoreClientAPI?.World?.Player?.PlayerUID)
            {
                CapturedSeat = __instance.MountedOn;
            }
        }

        /// <summary>
        /// After dismounting from a boat, creates a map marker at the boat's position.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("TryUnmount")]
        public static void TryUnmountPostfix(EntityAgent __instance, bool __result)
        {
            IMountableSeat seat = CapturedSeat;
            CapturedSeat = null;

            if (!__result || seat == null)
                return;

            if (!(__instance is EntityPlayer player)
                || player.PlayerUID != MapMarkerMod.CoreClientAPI?.World?.Player?.PlayerUID)
                return;

            Entity boatEntity = seat.MountSupplier?.OnEntity;
            if (boatEntity == null)
                return;

            var setting = GetBoatSetting(boatEntity);
            if (setting == null || !setting.Enabled)
                return;

            MapMarkerConfig.Settings config = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
            if (config == null)
                return;

            Vec3d boatPos = boatEntity.Pos.XYZ;
            MapMarkerMod.Network?.RequestWaypointFromServer(
                position: boatPos,
                settings: setting,
                sendChatMessage: config.ChatNotifyOnBoatMarker);
        }

        /// <summary>
        /// After mounting a boat, deletes the nearest parking marker for that boat type.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("TryMount")]
        public static void TryMountPostfix(EntityAgent __instance, bool __result, IMountableSeat onmount)
        {
            if (!__result || onmount == null)
                return;

            if (!(__instance is EntityPlayer player)
                || player.PlayerUID != MapMarkerMod.CoreClientAPI?.World?.Player?.PlayerUID)
                return;

            Entity boatEntity = onmount.MountSupplier?.OnEntity;
            if (boatEntity == null)
                return;

            var setting = GetBoatSetting(boatEntity);
            if (setting == null || !setting.Enabled)
                return;

            MapMarkerConfig.Settings config = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
            if (config == null)
                return;

            Vec3d boatPos = boatEntity.Pos.XYZ;
            MapMarkerMod.Network?.RequestWaypointDeletionAtPositionFromServer(
                position: boatPos,
                sendChatMessage: config.ChatNotifyOnBoatMarker,
                titlePattern: setting.MarkerTitle,
                maxRadius: setting.MarkerCoverageRadius);
        }

        /// <summary>
        /// Returns the appropriate map marker setting for the given mounted entity, or null if no
        /// Mount-typed entry matches. Detection is JSON-driven via marker definition entries
        /// whose <c>TriggerType</c> is <c>"Mount"</c> — addons can add new mount entries or
        /// extend the built-in Raft / Sailboat entries via patches.
        /// </summary>
        private static MapMarkerConfig.Settings.AutoMapMarkerSetting GetBoatSetting(Entity entity)
        {
            string code = entity.Code?.Path;
            if (code == null)
                return null;

            MapMarkerConfig.Settings config = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
            if (config == null)
                return null;

            return MapMarkerConfig.BoatRegistry?.TryMatch(code)?.Setting;
        }
    }
}
