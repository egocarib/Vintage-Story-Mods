using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Egocarib.AutoMapMarkers.EntityBehavior
{
    using EntityBehavior = Vintagestory.API.Common.Entities.EntityBehavior;

    /// <summary>
    /// Block behavior for traders. Automatically creates map markers when the player
    /// interacts with a trader.
    /// </summary>
    public class TraderMarkerBehavior : EntityBehavior
    {
        public bool IsArtisan => entity.Code.Path.EndsWith("-trader-artisan");
        public bool IsBuildingMaterials => entity.Code.Path.EndsWith("-trader-buildmaterials");
        public bool IsClothing => entity.Code.Path.EndsWith("-trader-clothing");
        public bool IsCommodities => entity.Code.Path.EndsWith("-trader-commodities");
        public bool IsFoods => entity.Code.Path.EndsWith("-trader-foods");
        public bool IsFurniture => entity.Code.Path.EndsWith("-trader-furniture");
        public bool IsLuxuries => entity.Code.Path.EndsWith("-trader-luxuries");
        public bool IsSurvivalGoods => entity.Code.Path.EndsWith("-trader-survivalgoods");
        public bool IsTreasureHunter => entity.Code.Path.EndsWith("-trader-treasurehunter");

        public TraderMarkerBehavior(Entity entity) : base(entity)
        {
        }

        public override string PropertyName()
        {
            return "egocarib_TraderMarkerBehavior";
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                if (mode == EnumInteractMode.Interact)
                {
                    IPlayer byPlayer = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID);
                    if (byPlayer != null)
                    {
                        Vec3d traderPos = new Vec3d(entity.Pos.X, entity.Pos.Y, entity.Pos.Z);
                        //WaypointUtil waypointUtil = new WaypointUtil(byPlayer as IServerPlayer);
                        //waypointUtil.AddWaypoint(traderPos, GetTraderSettings());
                        MapMarkerMod.Network.RequestWaypointFromServer(traderPos, GetTraderSettings());
                    }
                }
            }
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
        }

        /// <summary>
        /// Gets the map marker settings for the block this behavior is attached to.
        /// </summary>
        private MapMarkerConfig.Settings.AutoMapMarkerSetting GetTraderSettings()
        {
            if (IsArtisan)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.Traders.TraderArtisan;
            }
            if (IsBuildingMaterials)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.Traders.TraderBuildingMaterials;
            }
            if (IsClothing)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.Traders.TraderClothing;
            }
            if (IsCommodities)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.Traders.TraderCommodities;
            }
            if (IsFoods)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.Traders.TraderFoods;
            }
            if (IsFurniture)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.Traders.TraderFurniture;
            }
            if (IsLuxuries)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.Traders.TraderLuxuries;
            }
            if (IsSurvivalGoods)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.Traders.TraderSurvivalGoods;
            }
            if (IsTreasureHunter)
            {
                return MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).AutoMapMarkers.Traders.TraderTreasureHunter;
            }
            return null;
        }
    }
}
