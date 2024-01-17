using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Egocarib.AutoMapMarkers.BlockBehavior
{
    using static Egocarib.AutoMapMarkers.Settings.MapMarkerConfig;
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
            if (world.Side == EnumAppSide.Client)
            {
                HandleInteraction(blockSel.Position, byPlayer);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            if (world.Side == EnumAppSide.Client)
            {
                HandleInteraction(pos, byPlayer);
            }
            base.OnBlockBroken(world, pos, byPlayer, ref handling);
        }

        public void HandleInteraction(BlockPos pos, IPlayer byPlayer)
        {
            if (pos != null && byPlayer != null)
            {
                MapMarkerConfig.Settings config = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
                if (config != null && config.EnableMarkOnInteract)
                {
                    var settings = GetLooseOreSettings(config, pos);
                    if (settings != null)
                    {
                        bool shouldChat = config.ChatNotifyOnWaypointCreation;
                        Vec3d vecPos = pos.ToVec3d();
                        MapMarkerMod.Network.RequestWaypointFromServer(vecPos, settings, shouldChat);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the map marker settings for the block this behavior is attached to.
        /// </summary>
        private MapMarkerConfig.Settings.AutoMapMarkerSetting GetLooseOreSettings(MapMarkerConfig.Settings config, BlockPos blockPos)
        {
            ThingIdentifier thing = new ThingIdentifier(block, blockPos);
            if (thing.IsAnthracite)
                return config.AutoMapMarkers.SurfaceOre.LooseOreAnthracite;
            if (thing.IsBlackCoal)
                return config.AutoMapMarkers.SurfaceOre.LooseOreBlackCoal;
            if (thing.IsBorax)
                return config.AutoMapMarkers.SurfaceOre.LooseOreBorax;
            if (thing.IsBrownCoal)
                return config.AutoMapMarkers.SurfaceOre.LooseOreBrownCoal;
            if (thing.IsCinnabar)
                return config.AutoMapMarkers.SurfaceOre.LooseOreCinnabar;
            if (thing.IsGold)
                return config.AutoMapMarkers.SurfaceOre.LooseOreGold;
            if (thing.IsLapisLazuli)
                return config.AutoMapMarkers.SurfaceOre.LooseOreLapisLazuli;
            if (thing.IsLead)
                return config.AutoMapMarkers.SurfaceOre.LooseOreLead;
            if (thing.IsMalachiteCopper)
                return config.AutoMapMarkers.SurfaceOre.LooseOreCopper;
            if (thing.IsNativeCopper)
                return config.AutoMapMarkers.SurfaceOre.LooseOreCopper;
            if (thing.IsOlivine)
                return config.AutoMapMarkers.SurfaceOre.LooseOreOlivine;
            if (thing.IsQuartz)
                return config.AutoMapMarkers.SurfaceOre.LooseOreQuartz;
            if (thing.IsSilver)
                return config.AutoMapMarkers.SurfaceOre.LooseOreSilver;
            if (thing.IsSulfur)
                return config.AutoMapMarkers.SurfaceOre.LooseOreSulfur;
            if (thing.IsTin)
                return config.AutoMapMarkers.SurfaceOre.LooseOreTin;
            //if (thing.IsFluorite)
            //    return config.AutoMapMarkers.SurfaceOre.LooseOreFluorite;
            //if (thing.IsGraphite)
            //    return config.AutoMapMarkers.SurfaceOre.LooseOreGraphite;
            //if (thing.IsKernite)
            //    return config.AutoMapMarkers.SurfaceOre.LooseOreKernite;
            //if (thing.IsPhosporite)
            //    return config.AutoMapMarkers.SurfaceOre.LooseOrePhosporite;

            return null;
        }
    }
}