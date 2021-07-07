using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

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

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (world.Side == EnumAppSide.Server) //Server only if harvest fully completed (aborted harvest action registers only on client thread)
            {
                if (blockSel != null && blockSel.Position != null && byPlayer != null)
                {
                    Vec3d resinBlockPosition = new Vec3d(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
                    WaypointUtil waypointUtil = new WaypointUtil(byPlayer as IServerPlayer);
                    waypointUtil.AddWaypoint(resinBlockPosition, GetHarvestableObjectSettings());
                }
            }
            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel, ref handling);
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