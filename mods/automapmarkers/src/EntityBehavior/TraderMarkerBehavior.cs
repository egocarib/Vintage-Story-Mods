using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

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
                    EntityPlayer entityPlayer = byEntity as EntityPlayer;
                    if (entityPlayer == null) return;
                    IPlayer byPlayer = byEntity.World.PlayerByUid(entityPlayer.PlayerUID);
                    if (byPlayer != null)
                    {
                        MapMarkerConfig.Settings config = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
                        if (config != null && config.EnableMarkOnInteract)
                        {
                            ThingIdentifier thing = new ThingIdentifier(entity);
                            if (thing.Identify(config) && entity.Pos != null)
                            {
                                var settings = thing.GetMapMarkerSettings();
                                if (settings != null)
                                {
                                    bool shouldChat = config.ChatNotifyOnWaypointCreation;
                                    Vec3d traderPos = entity.Pos.XYZ;
                                    MapMarkerMod.Network?.RequestWaypointFromServer(traderPos, settings, shouldChat, thing.DynamicTitleComponent);
                                }
                            }
                        }
                    }
                }
            }
            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
        }
    }
}
