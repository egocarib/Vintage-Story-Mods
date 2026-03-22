using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Egocarib.AutoMapMarkers.BlockBehavior
{
    using BlockBehavior = Vintagestory.API.Common.BlockBehavior;

    /// <summary>
    /// Block behavior for loose surface ore blocks that indicate the location of underground ores.
    /// Automatically creates map markers when the player collects loose ores.
    /// </summary>
    public class LooseOresMarkerBehavior : BlockBehavior
    {
        public LooseOresMarkerBehavior(Block block) : base(block)
        {
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (world.Side == EnumAppSide.Client && blockSel != null)
            {
                HandleInteraction(blockSel.Position, byPlayer);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier, ref EnumHandling handling)
        {
            if (world.Side == EnumAppSide.Client)
            {
                HandleInteraction(pos, byPlayer);
            }
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier, ref handling);
        }

        public void HandleInteraction(BlockPos pos, IPlayer byPlayer)
        {
            if (pos != null && byPlayer != null)
            {
                MapMarkerConfig.Settings config = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
                if (config != null && config.EnableMarkOnInteract)
                {
                    ThingIdentifier thing = new ThingIdentifier(block, pos);
                    if (thing.Identify(config))
                    {
                        var settings = thing.GetMapMarkerSettings();
                        if (settings != null)
                        {
                            bool shouldChat = config.ChatNotifyOnWaypointCreation;
                            Vec3d vecPos = pos.ToVec3d();
                            MapMarkerMod.Network?.RequestWaypointFromServer(vecPos, settings, shouldChat, thing.DynamicTitleComponent);
                        }
                    }
                }
            }
        }
    }
}
