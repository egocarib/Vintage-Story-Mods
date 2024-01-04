using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Egocarib.AutoMapMarkers.BlockBehavior
{
    using BlockBehavior = Vintagestory.API.Common.BlockBehavior;

    /// <summary>
    /// Block behavior for harvestable objects, such as berries and mushrooms. Automatically
    /// creates map markers when the player interacts with certain harvestable objects.
    /// </summary>
    public class HarvestMarkerBehavior : BlockBehavior
    {
        public HarvestMarkerBehavior(Block block) : base(block)
        {
        }

        /// <summary>
        /// Called when the player begins to harvest this object. Attempts to create a new waypoint.
        /// </summary>
        /// <remarks>
        /// OnBlockInteractStop cannot be used client-side, because if a player successfully harvests the
        /// object, an OnBlockInteractStop event is not sent to the client. My guess is this is because the
        /// harvestable block is replaced by its "harvested" variety (in BehaviorHarvestable.OnBlockInteractStop),
        /// short-circuiting BlockBehavior callback on the original "unharvested" variant of the block?
        /// I think this could technically be resolved by creative JSON patching to insert our behavior before
        /// the harvest behavior, but alas - we'll leave that for another day.
        /// </remarks>
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (world.Side == EnumAppSide.Client)
            {
                if (blockSel != null && byPlayer != null)
                {
                    MapMarkerConfig.Settings config = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
                    if (config != null && config.EnableMarkOnInteract)
                    {
                        var settings = GetHarvestableObjectSettings(config, blockSel.Position);
                        if (settings != null && blockSel.Position != null)
                        {
                            bool shouldChat = config.ChatNotifyOnWaypointCreation;
                            Vec3d pos = blockSel.Position.ToVec3d();
                            MapMarkerMod.Network.RequestWaypointFromServer(pos, settings, shouldChat);
                        }
                    }
                }
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
        }

        /// <summary>
        /// Gets the map marker settings for the block this behavior is attached to.
        /// </summary>
        private MapMarkerConfig.Settings.AutoMapMarkerSetting GetHarvestableObjectSettings(MapMarkerConfig.Settings config, BlockPos blockPos)
        {
            ThingIdentifier thing = new ThingIdentifier(block, blockPos);
            if (thing.IsResin)
                return config.AutoMapMarkers.OrganicMatter.Resin;
            if (thing.IsBlueberry)
                return config.AutoMapMarkers.OrganicMatter.Blueberry;
            if (thing.IsCranberry)
                return config.AutoMapMarkers.OrganicMatter.Cranberry;
            if (thing.IsCurrantBlack)
                return config.AutoMapMarkers.OrganicMatter.BlackCurrant;
            if (thing.IsCurrantRed)
                return config.AutoMapMarkers.OrganicMatter.RedCurrant;
            if (thing.IsCurrantWhite)
                return config.AutoMapMarkers.OrganicMatter.WhiteCurrant;

            return null;
        }
    }
}