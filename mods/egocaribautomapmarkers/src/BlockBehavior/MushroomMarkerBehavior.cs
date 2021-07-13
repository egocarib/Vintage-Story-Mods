using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Egocarib.AutoMapMarkers.BlockBehavior
{
    using BlockBehavior = Vintagestory.API.Common.BlockBehavior;

    /// <summary>
    /// Block behavior for harvestable mushrooms, such as Bolete and Field Mushrooms.
    /// Automatically creates map markers when the player harvests mushrooms with a knife.
    /// </summary>
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
            if (world.Side == EnumAppSide.Client)
            {
                if (byPlayer != null)
                {
                    EnumTool? tool = byPlayer.InventoryManager.ActiveTool;
                    BlockMushroom thisBlock = block as BlockMushroom;
                    if (thisBlock != null && thisBlock.IsGrown() && tool == EnumTool.Knife)
                    {
                        //set map marker
                        Vec3d mushroomBlockPosition = new Vec3d(pos.X, pos.Y, pos.Z);
                        MapMarkerConfig.Settings.AutoMapMarkerSetting settingData = GetMushroomSettings(out bool shouldChat);
                        MapMarkerMod.Network.RequestWaypointFromServer(mushroomBlockPosition, settingData, shouldChat);
                    }
                }
            }
            base.OnBlockBroken(world, pos, byPlayer, ref handling);
        }

        /// <summary>
        /// Gets the map marker settings for the block this behavior is attached to.
        /// </summary>
        private MapMarkerConfig.Settings.AutoMapMarkerSetting GetMushroomSettings(out bool shouldChat)
        {
            MapMarkerConfig.Settings settings = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
            shouldChat = settings.ChatNotifyOnWaypointCreation;
            if (IsBolete)
            {
                return settings.AutoMapMarkers.OrganicMatter.MushroomBolete;
            }
            if (IsFieldMushroom)
            {
                return settings.AutoMapMarkers.OrganicMatter.MushroomFieldMushroom;
            }
            if (IsFlyAgaric)
            {
                return settings.AutoMapMarkers.OrganicMatter.MushroomFlyAgaric;
            }
            return null;
        }
    }
}