using Egocarib.AutoMapMarkers.Settings;
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
        public bool IsResin { get { return block.Code.Path.StartsWith("log-resin-"); } }
        public bool IsBlueberry { get { return block.Code.Path.Contains("-blueberry-ripe"); } }
        public bool IsCranberry { get { return block.Code.Path.Contains("-cranberry-ripe"); } }
        public bool IsCurrantBlack { get { return block.Code.Path.Contains("-blackcurrant-ripe"); } }
        public bool IsCurrantRed { get { return block.Code.Path.Contains("-redcurrant-ripe"); } }
        public bool IsCurrantWhite { get { return block.Code.Path.Contains("-whitecurrant-ripe"); } }

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
                if (blockSel != null && blockSel.Position != null && byPlayer != null)
                {
                    Vec3d harvestBlockPosition = new Vec3d(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
                    MapMarkerConfig.Settings.AutoMapMarkerSetting settingData = GetHarvestableObjectSettings(out bool shouldChat);
                    MapMarkerMod.Network.RequestWaypointFromServer(harvestBlockPosition, settingData, shouldChat);
                }
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
        }

        /// <summary>
        /// Gets the map marker settings for the block this behavior is attached to.
        /// </summary>
        private MapMarkerConfig.Settings.AutoMapMarkerSetting GetHarvestableObjectSettings(out bool shouldChat)
        {
            MapMarkerConfig.Settings settings = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
            shouldChat = settings.ChatNotifyOnWaypointCreation;
            if (IsResin)
            {
                return settings.AutoMapMarkers.OrganicMatter.Resin;
            }
            if (IsBlueberry)
            {
                return settings.AutoMapMarkers.OrganicMatter.Blueberry;
            }
            if (IsCranberry)
            {
                return settings.AutoMapMarkers.OrganicMatter.Cranberry;
            }
            if (IsCurrantBlack)
            {
                return settings.AutoMapMarkers.OrganicMatter.BlackCurrant;
            }
            if (IsCurrantRed)
            {
                return settings.AutoMapMarkers.OrganicMatter.RedCurrant;
            }
            if (IsCurrantWhite)
            {
                return settings.AutoMapMarkers.OrganicMatter.WhiteCurrant;
            }
            return null;
        }
    }
}