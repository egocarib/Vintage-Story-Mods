using Egocarib.AutoMapMarkers.BlockBehavior;
using Egocarib.AutoMapMarkers.EntityBehavior;
using Egocarib.AutoMapMarkers.Events;
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

        /// <summary>
        /// Server/client shared initialization
        /// </summary>
        public override void Start(ICoreAPI api)
        {
            CoreAPI = api;
            CoreAPI.RegisterEntityBehaviorClass("egocarib_TraderMarkerBehavior", typeof(TraderMarkerBehavior));
            CoreAPI.RegisterBlockBehaviorClass("egocarib_HarvestMarkerBehavior", typeof(HarvestMarkerBehavior));
            CoreAPI.RegisterBlockBehaviorClass("egocarib_LooseOreMarkerBehavior", typeof(LooseOresMarkerBehavior));
            HarmonyAgent.Harmonize();
        }

        /// <summary>
        /// Server-specific intialization
        /// </summary>
        public override void StartServerSide(ICoreServerAPI api)
        {
            CoreServerAPI = api;
            MapMarkerConfig.EnsureServerSettingsExist(api);
            Network = new MapMarkerNetwork(CoreServerAPI);
        }

        /// <summary>
        /// Client-specific initialization
        /// </summary>
        public override void StartClientSide(ICoreClientAPI api)
        {
            CoreClientAPI = api;
            MapMarkerConfig.InitializeDefinitions(api);
            Network = new MapMarkerNetwork(CoreClientAPI);
            CoreClientAPI.Input.InWorldAction += DetectionHandler.HandlePlayerSneak;
        }

        /// <summary>
        /// Unapplies Harmony patches and disposes of all static variables in the ModSystem.
        /// </summary>
        public override void Dispose()
        {
            HarmonyAgent.Deharmonize();
            if (CoreClientAPI != null)
            {
                if (CoreClientAPI.Input != null)
                    CoreClientAPI.Input.InWorldAction -= DetectionHandler.HandlePlayerSneak;
                CoreClientAPI = null;
            }
            CoreAPI = null;
            CoreServerAPI = null;
            Network?.Dispose();
            Network = null;
        }
    }
}
