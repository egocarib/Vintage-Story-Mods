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
    public class Block
    {
        private static BlockPos LastPositionChecked = null;

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Vintagestory.API.Common.Block), "OnGettingBroken");
            // Need to patch BlockReeds separately because it overridees this method
            yield return AccessTools.Method(typeof(Vintagestory.GameContent.BlockReeds), "OnGettingBroken");
        }

        /// <summary>
        /// Enables surfacing of the OnGettingBroken event to Blocks that we want to mark on the map.
        /// This event is called every 40 milliseconds, and is ONLY called on the client side
        /// Unfortunately, Harmony patch seems like the best option to be able to access OnGettingBroken event.
        /// (https://discord.com/channels/302152934249070593/351624415039193098/1185776235457040466)
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(BlockSelection blockSel)
        {
            // Make sure this mod feature is enabled before proceeding
            MapMarkerConfig.Settings config = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
            if (config?.EnableMarkOnInteract != true)
                return;

            // Avoid expensive recalculations by monitoring position
            if (blockSel?.Position == null || blockSel.Position.Equals(LastPositionChecked))
                return;
            LastPositionChecked = blockSel.Position.Copy();

            BlockPos blockPos = blockSel.Position;
            Vintagestory.API.Common.Block block = blockSel.Block;
            block ??= MapMarkerMod.CoreClientAPI.World.BlockAccessor.GetBlock(blockPos);

            // If the player is looking at grass, check the block beneath the grass instead
            if (block is BlockPlant)
            {
                bool? isGrass = block.Code?.Path?.StartsWith("tallgrass-", System.StringComparison.Ordinal);
                if (isGrass == true)
                {
                    blockPos = blockSel.Position.DownCopy();
                    block = MapMarkerMod.CoreClientAPI.World.BlockAccessor.GetBlock(blockPos);
                }
            }

            if (block == null)
                return;

            ThingIdentifier thing = new ThingIdentifier(block, blockPos);
            if (!thing.Identify(config))
                return;

            if (config.SuppressMarkerOnFarmland && thing.IsOnFarmland())
                return;

            var settings = thing.GetMapMarkerSettings();
            string dynamicTitleComponent = thing.DynamicTitleComponent;

            // Attempt to create a waypoint
            if (MapMarkerMod.Network != null)
            {
                MapMarkerMod.Network.RequestWaypointFromServer
                (
                    position: blockPos.ToVec3d(),
                    settings: settings,
                    sendChatMessage: config.ChatNotifyOnWaypointCreation,
                    dynamicTitleComponent: dynamicTitleComponent
                );
            }
        }
    }
}
