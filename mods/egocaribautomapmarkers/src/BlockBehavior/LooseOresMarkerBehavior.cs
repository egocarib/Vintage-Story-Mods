using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Egocarib.AutoMapMarkers.BlockBehavior
{
    using BlockBehavior = Vintagestory.API.Common.BlockBehavior;

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
                var looseOreSettings = GetLooseOreSettings();
                if (looseOreSettings != null)
                {
                    Vec3d looseOreBlockPosition = new Vec3d(pos.X, pos.Y, pos.Z);
                    MapMarkerMod.Network.RequestWaypointFromServer(looseOreBlockPosition, looseOreSettings);
                }
            }
        }

        private MapMarkerConfig.Settings.AutoMapMarkerSetting GetLooseOreSettings()
        {
            if (IsAnthracite)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreAnthracite;
            }
            if (IsBlackCoal)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreBlackCoal;
            }
            if (IsBorax)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreBorax;
            }
            if (IsBrownCoal)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreBrownCoal;
            }
            if (IsCinnabar)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreCinnabar;
            }
            //if (IsFluorite)
            //{
            //    return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreFluorite;
            //}
            if (IsGold)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreGold;
            }
            //if (IsGraphite)
            //{
            //    return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreGraphite;
            //}
            //if (IsKernite)
            //{
            //    return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreKernite;
            //}
            if (IsLapisLazuli)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreLapisLazuli;
            }
            if (IsLead)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreLead;
            }
            if (IsMalachiteCopper)
            {
                //Shares settings with native copper
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreCopper;
            }
            if (IsNativeCopper)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreCopper;
            }
            if (IsOlivine)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreOlivine;
            }
            //if (IsPhosporite)
            //{
            //    return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.LooseOrePhosporite;
            //}
            if (IsQuartz)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreQuartz;
            }
            if (IsSilver)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreSilver;
            }
            if (IsSulfur)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreSulfur;
            }
            if (IsTin)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.SurfaceOre.LooseOreTin;
            }
            return null;
        }
    }
}