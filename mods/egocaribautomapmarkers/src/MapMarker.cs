using Egocarib.AutoMapMarkers.BlockBehavior;
using Egocarib.AutoMapMarkers.EntityBehavior;
using Egocarib.AutoMapMarkers.Settings;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Egocarib.AutoMapMarkers
{
    public class MapMarkerMod : ModSystem
    {
        public static WorldMapManager MapManager;
        public static ICoreAPI CoreAPI;
        public static ICoreServerAPI CoreServerAPI;
        public static ICoreClientAPI CoreClientAPI;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            CoreAPI = api;
            api.RegisterEntityBehaviorClass("egocarib_TraderMarkerBehavior", typeof(TraderMarkerBehavior));
            api.RegisterBlockBehaviorClass("egocarib_HarvestMarkerBehavior", typeof(HarvestMarkerBehavior));
            api.RegisterBlockBehaviorClass("egocarib_MushroomMarkerBehavior", typeof(MushroomMarkerBehavior));
            api.RegisterBlockBehaviorClass("egocarib_LooseOreMarkerBehavior", typeof(LooseOresMarkerBehavior));
            MapManager = api.ModLoader.GetModSystem<WorldMapManager>();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            CoreServerAPI = api;
            MapMarkerConfig.GetSettings(api); //Ensure config file is generated at startup if one does not exist yet.
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            CoreClientAPI = api;
        }

        public static void Chat(IPlayer player, string message, EnumChatType chatTypeForServer = EnumChatType.Notification)
        {
            if (CoreServerAPI != null)
            {
                CoreServerAPI.SendMessage(player, GlobalConstants.GeneralChatGroup, message, chatTypeForServer);
            }
            else if (CoreClientAPI != null)
            {
                CoreClientAPI.SendChatMessage(message);
            }
        }

        public static void Log(string message)
        {
            MapMarkerMod.CoreAPI.Logger.Notification("MapMarkerMod: " + message);
        }
    }
}
