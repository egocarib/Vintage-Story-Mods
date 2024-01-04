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
                        MapMarkerConfig.Settings config = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
                        if (config != null && config.EnableMarkOnInteract)
                        {
                            var settings = GetTraderSettings(config);
                            if (settings != null && entity.Pos != null)
                            {
                                bool shouldChat = config.ChatNotifyOnWaypointCreation;
                                Vec3d traderPos = entity.Pos.XYZ;
                                MapMarkerMod.Network.RequestWaypointFromServer(traderPos, settings, shouldChat);
                            }
                        }
                    }
                }
            }
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
        }

        /// <summary>
        /// Gets the map marker settings for the block this behavior is attached to.
        /// </summary>
        private MapMarkerConfig.Settings.AutoMapMarkerSetting GetTraderSettings(MapMarkerConfig.Settings config)
        {
            ThingIdentifier thing = new ThingIdentifier(entity);
            if (thing.IsArtisan)
                return config.AutoMapMarkers.Traders.TraderArtisan;
            if (thing.IsBuildingMaterials)
                return config.AutoMapMarkers.Traders.TraderBuildingMaterials;
            if (thing.IsClothing)
                return config.AutoMapMarkers.Traders.TraderClothing;
            if (thing.IsCommodities)
                return config.AutoMapMarkers.Traders.TraderCommodities;
            if (thing.IsFoods)
                return config.AutoMapMarkers.Traders.TraderFoods;
            if (thing.IsFurniture)
                return config.AutoMapMarkers.Traders.TraderFurniture;
            if (thing.IsLuxuries)
                return config.AutoMapMarkers.Traders.TraderLuxuries;
            if (thing.IsSurvivalGoods)
                return config.AutoMapMarkers.Traders.TraderSurvivalGoods;
            if (thing.IsTreasureHunter)
                return config.AutoMapMarkers.Traders.TraderTreasureHunter;

            return null;
        }
    }
}
