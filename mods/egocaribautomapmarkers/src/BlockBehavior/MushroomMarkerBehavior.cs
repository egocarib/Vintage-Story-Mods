using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace egocarib_AutoMapMarkers
{
    public class MushroomMarkerBehavior : BlockBehavior
    {
        public bool IsBolete { get { return block.Code.Path.Contains("-bolete-"); } }
        public bool IsFieldMushroom { get { return block.Code.Path.Contains("-fieldmushroom-"); } }
        public bool IsFlyAgaric { get { return block.Code.Path.Contains("-flyagaric-"); } }

        public MushroomMarkerBehavior(Block block) : base(block)
        {
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            if (world.Side == EnumAppSide.Server)
            {
                if (byPlayer != null)
                {
                    EnumTool? tool = byPlayer.InventoryManager.ActiveTool;
                    BlockMushroom thisBlock = block as BlockMushroom;
                    if (thisBlock != null && thisBlock.IsGrown() && tool == EnumTool.Knife)
                    {
                        //set map marker
                        Vec3d mushroomBlockPosition = new Vec3d(pos.X, pos.Y, pos.Z);
                        WaypointUtil waypointUtil = new WaypointUtil(byPlayer as IServerPlayer);
                        waypointUtil.AddWaypoint(mushroomBlockPosition, GetMushroomSettings());
                    }
                }
            }
            base.OnBlockBroken(world, pos, byPlayer, ref handling);
        }

        private MapMarkerConfig.Settings.AutoMapMarkerSetting GetMushroomSettings()
        {
            if (IsBolete)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.OrganicMatter.MushroomBolete;
            }
            if (IsFieldMushroom)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.OrganicMatter.MushroomFieldMushroom;
            }
            if (IsFlyAgaric)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.OrganicMatter.MushroomFlyAgaric;
            }
            return null;
        }
    }
}