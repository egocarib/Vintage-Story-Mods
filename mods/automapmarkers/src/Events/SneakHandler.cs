using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;

namespace Egocarib.AutoMapMarkers.Events
{
    /// <summary>
    /// Methods for handling a sneak state change on the player.
    /// </summary>
    public class SneakHandler
    {
        /// <summary>
        /// Event callback that allows us to respond to the player's sneak action.
        /// This is registered to the OnEntityAction event, ICoreClientAPI.Input.InWorldAction
        /// </summary>
        public static void HandlePlayerSneak(EnumEntityAction action, bool on, ref EnumHandling handled)
        {
            if (action != EnumEntityAction.Sneak || on != true)
                return;  // React only to sneak action

            MapMarkerConfig.Settings config = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
            if (config == null || config.EnableMarkOnSneak != true)
                return;

            BlockSelection blockSel = MapMarkerMod.CoreClientAPI?.World?.Player?.CurrentBlockSelection;
            EntitySelection entitySel = MapMarkerMod.CoreClientAPI?.World?.Player?.CurrentEntitySelection;
            if (blockSel == null && entitySel == null)
                return;  // Not currently looking at a block or entity

            Vec3d vecPos;
            ThingIdentifier thing;
            if (blockSel != null)
            {
                BlockPos pos = blockSel.Position;
                Block block = blockSel.Block;
                if (pos == null || block == null)
                    return;
                vecPos = new Vec3d(pos.X, pos.Y, pos.Z);
                thing = new ThingIdentifier(block, pos);
            }
            else  // entitySel != null
            {
                Entity entity = entitySel.Entity;
                vecPos = entitySel.Position;
                if (vecPos == null || entity == null)
                    return;
                thing = new ThingIdentifier(entity);
            }

            ////DEBUG
            //MessageUtil.Log($"DEBUG: viewing '{thing.GetAssetPath()}' [class: {(blockSel != null ? blockSel.Block.GetType() : entitySel.Entity.GetType())}]");
            //if (thing.GetAssetPath().StartsWith("tallgrass-", StringComparison.Ordinal))
            //{
            //    Block blockBelow = MapMarkerMod.CoreClientAPI.World.BlockAccessor.GetBlock(blockSel.Position.Copy().Down());
            //    MessageUtil.Log($"      block below: '{blockBelow.Code.Path}' [class: {(blockBelow.GetType())}]");
            //}

            // If looking at grass, check the block below it instead
            if (thing.GetAssetPath().StartsWith("tallgrass-", StringComparison.Ordinal))
            {
                try
                {
                    BlockPos downOnePos = blockSel.Position.DownCopy();
                    Block blockBelow = MapMarkerMod.CoreClientAPI.World.BlockAccessor.GetBlock(downOnePos);
                    thing = new ThingIdentifier(blockBelow, downOnePos);
                }
                catch
                {
                    return;
                }
            }

            if (!thing.Identify(config))
                return;  // Unrecognized block or entity; we don't know how to make map markers for this

            var settings = thing.GetMapMarkerSettings();
            if (settings == null)
                return;

            MapMarkerMod.Network.RequestWaypointFromServer(vecPos, settings, config.ChatNotifyOnWaypointCreation, thing.DynamicTitleComponent);
        }
    }
}
