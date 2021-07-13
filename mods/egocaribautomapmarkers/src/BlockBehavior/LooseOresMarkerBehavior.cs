using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Egocarib.AutoMapMarkers.BlockBehavior
{
    using BlockBehavior = Vintagestory.API.Common.BlockBehavior;

    /// <summary>
    /// Block behavior for loose surface ore blocks that indicate the location of underground ores.
    /// Automatically creates map markers when the player collects loose ores.
    /// </summary>
    public class LooseOresMarkerBehavior : BlockBehavior
    {
        public bool IsAnthracite { get { return block.Code.Path.Contains("-anthracite-"); } }
        public bool IsBlackCoal { get { return block.Code.Path.Contains("-bituminouscoal-"); } }
        public bool IsBorax { get { return block.Code.Path.Contains("-borax-"); } }
        public bool IsBrownCoal { get { return block.Code.Path.Contains("-lignite-"); } }
        public bool IsCinnabar { get { return block.Code.Path.Contains("-cinnabar-"); } }
        //public bool IsFluorite { get { return block.Code.Path.Contains("-fluorite-"); } }
        public bool IsGold { get { return block.Code.Path.Contains("_nativegold-"); } }
        //public bool IsGraphite { get { return block.Code.Path.Contains("-graphite-"); } }
        //public bool IsKernite { get { return block.Code.Path.Contains("-kernite-"); } }
        public bool IsLapisLazuli { get { return block.Code.Path.Contains("-lapislazuli-"); } }
        public bool IsLead { get { return block.Code.Path.Contains("-galena-"); } }
        public bool IsMalachiteCopper { get { return block.Code.Path.Contains("-malachite-"); } }
        public bool IsNativeCopper { get { return block.Code.Path.Contains("-nativecopper-"); } }
        public bool IsOlivine { get { return block.Code.Path.Contains("-olivine-"); } }
        //public bool IsPhosporite { get { return block.Code.Path.Contains("-phosphorite-"); } }
        public bool IsQuartz { get { return block.Code.Path.Contains("-quartz-"); } }
        public bool IsSilver { get { return block.Code.Path.Contains("_nativesilver-");  } }
        public bool IsSulfur { get { return block.Code.Path.Contains("-sulfur-"); } }
        public bool IsTin { get { return block.Code.Path.Contains("-cassiterite-"); } }

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
                var looseOreSettings = GetLooseOreSettings(out bool shouldChat);
                if (looseOreSettings != null)
                {
                    Vec3d looseOreBlockPosition = new Vec3d(pos.X, pos.Y, pos.Z);
                    MapMarkerMod.Network.RequestWaypointFromServer(looseOreBlockPosition, looseOreSettings, shouldChat);
                }
            }
        }

        /// <summary>
        /// Gets the map marker settings for the block this behavior is attached to.
        /// </summary>
        private MapMarkerConfig.Settings.AutoMapMarkerSetting GetLooseOreSettings(out bool shouldChat)
        {
            MapMarkerConfig.Settings settings = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
            shouldChat = settings.ChatNotifyOnWaypointCreation;
            if (IsAnthracite)
            {
                return settings.AutoMapMarkers.SurfaceOre.LooseOreAnthracite;
            }
            if (IsBlackCoal)
            {
                return settings.AutoMapMarkers.SurfaceOre.LooseOreBlackCoal;
            }
            if (IsBorax)
            {
                return settings.AutoMapMarkers.SurfaceOre.LooseOreBorax;
            }
            if (IsBrownCoal)
            {
                return settings.AutoMapMarkers.SurfaceOre.LooseOreBrownCoal;
            }
            if (IsCinnabar)
            {
                return settings.AutoMapMarkers.SurfaceOre.LooseOreCinnabar;
            }
            //if (IsFluorite)
            //{
            //    return settings.AutoMapMarkers.SurfaceOre.LooseOreFluorite;
            //}
            if (IsGold)
            {
                return settings.AutoMapMarkers.SurfaceOre.LooseOreGold;
            }
            //if (IsGraphite)
            //{
            //    return settings.AutoMapMarkers.SurfaceOre.LooseOreGraphite;
            //}
            //if (IsKernite)
            //{
            //    return settings.AutoMapMarkers.SurfaceOre.LooseOreKernite;
            //}
            if (IsLapisLazuli)
            {
                return settings.AutoMapMarkers.SurfaceOre.LooseOreLapisLazuli;
            }
            if (IsLead)
            {
                return settings.AutoMapMarkers.SurfaceOre.LooseOreLead;
            }
            if (IsMalachiteCopper)
            {
                //Shares settings with native copper
                return settings.AutoMapMarkers.SurfaceOre.LooseOreCopper;
            }
            if (IsNativeCopper)
            {
                return settings.AutoMapMarkers.SurfaceOre.LooseOreCopper;
            }
            if (IsOlivine)
            {
                return settings.AutoMapMarkers.SurfaceOre.LooseOreOlivine;
            }
            //if (IsPhosporite)
            //{
            //    return settings.AutoMapMarkers.LooseOrePhosporite;
            //}
            if (IsQuartz)
            {
                return settings.AutoMapMarkers.SurfaceOre.LooseOreQuartz;
            }
            if (IsSilver)
            {
                return settings.AutoMapMarkers.SurfaceOre.LooseOreSilver;
            }
            if (IsSulfur)
            {
                return settings.AutoMapMarkers.SurfaceOre.LooseOreSulfur;
            }
            if (IsTin)
            {
                return settings.AutoMapMarkers.SurfaceOre.LooseOreTin;
            }
            return null;
        }
    }
}