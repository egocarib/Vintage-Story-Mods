using Egocarib.AutoMapMarkers.BlockBehavior;
using Egocarib.AutoMapMarkers.EntityBehavior;
using Egocarib.AutoMapMarkers.Network;
using Egocarib.AutoMapMarkers.Patches;
using Egocarib.AutoMapMarkers.Settings;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Egocarib.AutoMapMarkers
{
    public class MapMarkerMod : ModSystem
    {
        public static ICoreAPI CoreAPI;
        public static ICoreServerAPI CoreServerAPI;
        public static ICoreClientAPI CoreClientAPI;
        public static MapMarkerNetwork Network;

        public override void Start(ICoreAPI api)
        {
            CoreAPI = api;
            CoreAPI.RegisterEntityBehaviorClass("egocarib_TraderMarkerBehavior", typeof(TraderMarkerBehavior));
            CoreAPI.RegisterBlockBehaviorClass("egocarib_HarvestMarkerBehavior", typeof(HarvestMarkerBehavior));
            CoreAPI.RegisterBlockBehaviorClass("egocarib_MushroomMarkerBehavior", typeof(MushroomMarkerBehavior));
            CoreAPI.RegisterBlockBehaviorClass("egocarib_LooseOreMarkerBehavior", typeof(LooseOresMarkerBehavior));
            HarmonyAgent.Harmonize();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            CoreServerAPI = api;
            MapMarkerConfig.GetSettings(api); //Ensure config file is generated at startup if one does not exist yet.
            Network = new MapMarkerNetwork(CoreServerAPI);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            CoreClientAPI = api;
            MapMarkerConfig.GetSettings(api); //Ensure config file is generated
            //TODO: make config loading more consistent (Client-side only?)
            Network = new MapMarkerNetwork(CoreClientAPI);
        }

        public override void Dispose()
        {
            HarmonyAgent.Deharmonize();
            CoreAPI = null;
            CoreServerAPI = null;
            CoreClientAPI = null;
            Network = null;
        }
    }
}
