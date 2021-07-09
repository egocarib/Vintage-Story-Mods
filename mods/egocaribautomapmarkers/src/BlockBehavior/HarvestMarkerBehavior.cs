using Egocarib.AutoMapMarkers.Settings;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Egocarib.AutoMapMarkers.BlockBehavior
{
    using BlockBehavior = Vintagestory.API.Common.BlockBehavior;

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
        /// When the player begins to harvest this object.
        /// We can't use OnBlockInteractStop, because it is sent only to the Server (not client) when an item is successfully harvested. It does get sent to client if the player stops attempting to harvest, but that's not helpful.
        /// My guess is this is because the harvestable block is replaced by its "harvested" variety, short-circuiting blockbehavior callbacks on the original "unharvested" block variant?
        /// </summary>
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (world.Side == EnumAppSide.Client)
            {
                if (blockSel != null && blockSel.Position != null && byPlayer != null)
                {
                    Vec3d harvestBlockPosition = new Vec3d(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
                    MapMarkerMod.Network.RequestWaypointFromServer(harvestBlockPosition, GetHarvestableObjectSettings());
                }
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
        }

        private MapMarkerConfig.Settings.AutoMapMarkerSetting GetHarvestableObjectSettings()
        {
            if (IsResin)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.OrganicMatter.Resin;
            }
            if (IsBlueberry)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.OrganicMatter.Blueberry;
            }
            if (IsCranberry)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.OrganicMatter.Cranberry;
            }
            if (IsCurrantBlack)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.OrganicMatter.BlackCurrant;
            }
            if (IsCurrantRed)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.OrganicMatter.RedCurrant;
            }
            if (IsCurrantWhite)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.OrganicMatter.WhiteCurrant;
            }
            return null;
        }
    }
}