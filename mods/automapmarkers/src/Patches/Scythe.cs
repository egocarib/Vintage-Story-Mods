using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.GameContent;

namespace Egocarib.AutoMapMarkers.Patches
{
    [HarmonyPatch]
    public class Scythe
    {
        // ItemScythe extends ItemShears. When not in trim mode, ItemScythe.breakMultiBlock
        // calls base.breakMultiBlock (i.e. ItemShears.breakMultiBlock), so patching the base
        // method is sufficient to intercept all scythe harvest breaks.
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ItemShears), "breakMultiBlock");
        }

        [HarmonyPrefix]
        public static void Prefix(BlockPos pos, IPlayer plr)
        {
            if (MapMarkerMod.CoreClientAPI == null)
                return;

            MapMarkerConfig.Settings config = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
            if (config?.EnableMarkOnInteract != true)
                return;

            Vintagestory.API.Common.Block block = MapMarkerMod.CoreClientAPI.World.BlockAccessor.GetBlock(pos);
            if (block == null)
                return;

            if (!(block is BlockPlant
                || block is BlockCrop
                || block is BlockMushroom
                || block is BlockReeds
                || block is BlockFruitTreePart))
                return;

            ThingIdentifier thing = new ThingIdentifier(block, pos);
            if (!thing.Identify(config, ThingIdentifier.IdentifyAsType.DynamicFlora))
                return;

            if (config.SuppressMarkerOnFarmland && thing.IsOnFarmland())
                return;

            var settings = thing.GetMapMarkerSettings();
            string dynamicTitleComponent = thing.DynamicTitleComponent;

            MapMarkerMod.Network?.RequestWaypointFromServer(
                position: pos.ToVec3d(),
                settings: settings,
                sendChatMessage: config.ChatNotifyOnWaypointCreation,
                dynamicTitleComponent: dynamicTitleComponent
            );
        }
    }
}
