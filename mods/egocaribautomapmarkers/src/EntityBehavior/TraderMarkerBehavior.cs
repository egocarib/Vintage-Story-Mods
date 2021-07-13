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
                        MapMarkerConfig.Settings.AutoMapMarkerSetting settingData = GetTraderSettings(out bool shouldChat);
                        MapMarkerMod.Network.RequestWaypointFromServer(traderPos, settingData, shouldChat);
                    }
                }
            }
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
        }

        /// <summary>
        /// Gets the map marker settings for the block this behavior is attached to.
        /// </summary>
        private MapMarkerConfig.Settings.AutoMapMarkerSetting GetTraderSettings(out bool shouldChat)
        {
            MapMarkerConfig.Settings settings = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
            shouldChat = settings.ChatNotifyOnWaypointCreation;
            if (IsArtisan)
            {
                return settings.AutoMapMarkers.Traders.TraderArtisan;
            }
            if (IsBuildingMaterials)
            {
                return settings.AutoMapMarkers.Traders.TraderBuildingMaterials;
            }
            if (IsClothing)
            {
                return settings.AutoMapMarkers.Traders.TraderClothing;
            }
            if (IsCommodities)
            {
                return settings.AutoMapMarkers.Traders.TraderCommodities;
            }
            if (IsFoods)
            {
                return settings.AutoMapMarkers.Traders.TraderFoods;
            }
            if (IsFurniture)
            {
                return settings.AutoMapMarkers.Traders.TraderFurniture;
            }
            if (IsLuxuries)
            {
                return settings.AutoMapMarkers.Traders.TraderLuxuries;
            }
            if (IsSurvivalGoods)
            {
                return settings.AutoMapMarkers.Traders.TraderSurvivalGoods;
            }
            if (IsTreasureHunter)
            {
                return settings.AutoMapMarkers.Traders.TraderTreasureHunter;
            }
            return null;
        }
    }
}
