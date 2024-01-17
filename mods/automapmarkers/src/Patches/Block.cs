using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using Vintagestory.API.Util;
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
            if (blockSel.Position.Equals(LastPositionChecked))
                return;
            LastPositionChecked = blockSel.Position.Copy();

            bool shouldRequestWaypoint = false;
            Vintagestory.API.Common.Block block = blockSel.Block;
            BlockPos blockPos = blockSel.Position;
            MapMarkerConfig.Settings.AutoMapMarkerSetting settings = null;
            string dynamicTitleComponent = null;

            // If the player is looking at grass, check the block beneath the grass instead
            if (block is BlockTallGrass)
            {
                blockPos = blockSel.Position.DownCopy();
                block = MapMarkerMod.CoreClientAPI.World.BlockAccessor.GetBlock(blockPos);
            }

            // BlockSoil (High Fert. Soil) and BlockSoilDeposit (Peat, Blue Clay, Fire Clay)
            if (block is BlockSoil)
            {
                ThingIdentifier thing = new ThingIdentifier(block, blockPos);
                if (thing.IsBlueClay)
                    settings = config.AutoMapMarkers.MiscBlocks.BlockBlueClay;
                else if (thing.IsFireClay)
                    settings = config.AutoMapMarkers.MiscBlocks.BlockFireClay;
                else if (thing.IsPeat)
                    settings = config.AutoMapMarkers.MiscBlocks.BlockPeat;
                else if (thing.IsHighFertSoil)
                    settings = config.AutoMapMarkers.MiscBlocks.BlockHighFertilitySoil;
                else
                    return;

                shouldRequestWaypoint = true;
            }

            // BlockMeteorite (Meteoritic Iron)
            else if (block is BlockMeteorite)
            {
                ThingIdentifier thing = new ThingIdentifier(block, blockPos);
                if (thing.IsMeteoriticIron)
                    settings = config.AutoMapMarkers.MiscBlocks.BlockMeteoriticIron;
                else
                    return;

                shouldRequestWaypoint = true;
            }

            // BlockBeehive, BlockStaticTranslocator
            else if (block is BlockBeehive || block is BlockStaticTranslocator)
            {
                ThingIdentifier thing = new ThingIdentifier(block, blockPos);
                if (thing.IsBeehive)
                    settings = config.AutoMapMarkers.MiscBlocks.Beehive;
                else if (thing.IsTranslocator)
                    settings = config.AutoMapMarkers.MiscBlocks.Translocator;
                else
                    return;

                shouldRequestWaypoint = true;
            }

            // BlockOre (all underground ore deposits)
            else if (block is BlockOre)
            {
                ThingIdentifier thing = new ThingIdentifier(block, blockPos);
                if (!thing.Identify(config, ThingIdentifier.IdentifyAsType.DeepOre))
                    return;  // Unrecognized BlockOre variant
                
                settings = thing.GetMapMarkerSettings();
                shouldRequestWaypoint = true;
            }

            // BlockFullCoating (saltpeter)
            else if (block is BlockFullCoating)
            {
                ThingIdentifier thing = new ThingIdentifier(block, blockPos);
                if (thing.IsSaltpeter)
                    settings = config.AutoMapMarkers.MiscBlocks.BlockCoatingSaltpeter;
                else
                    return;

                shouldRequestWaypoint = true;
            }

            // BlockMushroom (all mushrooms), BlockPlant (all flowers), BlockCrop (all crops), BlockReed (all reeds), BlockFruitTreePart (all fruit trees)
            else if (block is BlockPlant
                || block is BlockCrop
                || block is BlockMushroom
                || block is BlockReeds
                || block is BlockFruitTreePart)
            {
                ThingIdentifier thing = new ThingIdentifier(block, blockPos);
                if (!thing.Identify(config, ThingIdentifier.IdentifyAsType.DynamicFlora))
                    return;  // Unrecognized mushroom, flower, crop, reed, or fruit tree

                settings = thing.GetMapMarkerSettings();
                dynamicTitleComponent = thing.DynamicTitleComponent;
                shouldRequestWaypoint = true;
            }

            // Attempt to create a waypoint if we matched something above
            if (shouldRequestWaypoint)
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
