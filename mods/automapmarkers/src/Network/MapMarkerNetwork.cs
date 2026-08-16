using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using Egocarib.AutoMapMarkers.Waypoints;
using ProtoBuf;
using System;
using System.IO;
using System.Timers;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using static Egocarib.AutoMapMarkers.Settings.MapMarkerConfig.Settings;

namespace Egocarib.AutoMapMarkers.Network
{
    /// <summary>
    /// Data required for handling client waypoint creation requests.
    /// </summary>
    [ProtoContract]
    public class ClientWaypointRequest
    {
        [ProtoMember(1)]
        public Vec3d waypointPosition;
        [ProtoMember(2)]
        public AutoMapMarkerSetting waypointSettings;
        [ProtoMember(3)]
        public bool sendChatMessageToPlayer;
        [ProtoMember(4)]
        public string dynamicTitleComponent;
        [ProtoMember(5)]
        public bool includeCoordinates;
    }

    [ProtoContract]
    public class ClientDefaultSettingsRequest
    {
        //Stub class used only for making an identifiable request to the server
    }

    [ProtoContract]
    public class ServerSettingsResponse
    {
        [ProtoMember(1)]
        public string SettingsJson { get; set; }
    }

    [ProtoContract]
    public class ClientWaypointDeleteRequest
    {
        [ProtoMember(1)]
        public bool sendChatMessageToPlayer;
        [ProtoMember(2)]
        public Vec3d targetPosition;
        [ProtoMember(3)]
        public string titlePattern;
        [ProtoMember(4)]
        public double maxRadius;
    }

    /// <summary>
    /// Waypoint request handler for Auto Map Markers mod.
    /// </summary>
    /// <remarks>
    /// When the mod is installed on the server, this facilitates communication between the client,
    /// which may request that a waypoint be created, and the server, which creates the waypoints and
    /// syncs them back to the client.
    ///
    /// When the mod is not installed on the server, requests are instead fulfilled entirely
    /// client-side by <see cref="ClientWaypointCommands"/>, which drives the vanilla waypoint chat
    /// commands. The transport is chosen per request; detection code doesn't need to care which one
    /// is in use.
    /// </remarks>
    public class MapMarkerNetwork
    {
        /// <summary>
        /// Set the AUTOMAPMARKERS_FORCE_CLIENT environment variable to exercise the client-side
        /// waypoint path in single player, where the integrated server always has the mod.
        /// </summary>
        private static readonly bool ForceClientSideWaypoints =
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AUTOMAPMARKERS_FORCE_CLIENT"));

        public EnumAppSide Side;
        public ICoreAPI CoreAPI;
        public IServerNetworkChannel ServerNetworkChannel;
        public IClientNetworkChannel ClientNetworkChannel;
        public const string ChannelID = "Egocarib.AutoMapMarkers.Network.MapMarkerChannel";
        private const int HandshakeIntervalMs = 1000;
        private const int MaxConnectionAttempts = 9;
        private Timer ClientHandshakeTimer;
        private int ConnectionCheckAttempts = 0;
        private bool FallbackModeLogged = false;

        /// <summary>
        /// True when the mod is present on the server and its channel is available, meaning waypoint
        /// requests can be handled directly by <see cref="WaypointUtil"/> on the server.
        /// </summary>
        private bool UseServerChannel =>
            !ForceClientSideWaypoints && ClientNetworkChannel?.Connected == true;

        public MapMarkerNetwork(ICoreAPI api)
        {
            Side = api.Side;
            CoreAPI = api;
            if (Side == EnumAppSide.Server)
            {
                ConfigureServer(api as ICoreServerAPI);
            }
            else
            {
                ConfigureClient(api as ICoreClientAPI);
                InitiateHandshakeWithServer();
            }
        }

        /// <summary>
        /// Configures the client side of the network channel.
        /// </summary>
        /// <remarks>
        /// Side: client only
        /// </remarks>
        private void ConfigureClient(ICoreClientAPI clientAPI)
        {
            ClientNetworkChannel = clientAPI.Network
                .RegisterChannel(ChannelID)
                .RegisterMessageType(typeof(ClientDefaultSettingsRequest))
                .RegisterMessageType(typeof(ServerSettingsResponse))
                .RegisterMessageType(typeof(ClientWaypointRequest))
                .RegisterMessageType(typeof(ClientWaypointDeleteRequest))
                .SetMessageHandler<ServerSettingsResponse>(OnReceiveDefaultSettingsFromServer);
        }

        /// <summary>
        /// Configures the server side of the network channel.
        /// </summary>
        /// <remarks>
        /// Side: server only
        /// </remarks>
        private void ConfigureServer(ICoreServerAPI serverAPI)
        {
            ServerNetworkChannel = serverAPI.Network
                .RegisterChannel(ChannelID)
                .RegisterMessageType(typeof(ClientDefaultSettingsRequest))
                .RegisterMessageType(typeof(ServerSettingsResponse))
                .RegisterMessageType(typeof(ClientWaypointRequest))
                .RegisterMessageType(typeof(ClientWaypointDeleteRequest))
                .SetMessageHandler<ClientDefaultSettingsRequest>(OnClientDefaultSettingsRequest)
                .SetMessageHandler<ClientWaypointDeleteRequest>(OnClientWaypointDeleteRequest)
                .SetMessageHandler<ClientWaypointRequest>(OnClientWaypointRequest);
        }

        /// <summary>
        /// Server-side handler - receives a waypoint creation request arriving from a client.
        /// </summary>
        /// <remarks>
        /// Side: server only
        /// </remarks>
        private void OnClientWaypointRequest(IPlayer fromPlayer, ClientWaypointRequest request)
        {
            IServerPlayer serverPlayer = fromPlayer as IServerPlayer;
            if (serverPlayer == null)
            {
                MessageUtil.LogError("Couldn't fulfill map marker creation request - unable to resolve ServerPlayer associated with request.");
                return;
            }
            WaypointUtil waypointUtil = new WaypointUtil(serverPlayer);
            waypointUtil.AddWaypoint(request.waypointPosition, request.waypointSettings, request.sendChatMessageToPlayer, request.dynamicTitleComponent, request.includeCoordinates);
        }

        /// <summary>
        /// Server-side handler - receives a waypoint deletion request arriving from a client.
        /// </summary>
        /// <remarks>
        /// Side: server only
        /// </remarks>
        private void OnClientWaypointDeleteRequest(IPlayer fromPlayer, ClientWaypointDeleteRequest request)
        {
            IServerPlayer serverPlayer = fromPlayer as IServerPlayer;
            if (serverPlayer == null)
            {
                MessageUtil.LogError("Couldn't fulfill map marker deletion request - unable to resolve ServerPlayer associated with request.");
                return;
            }
            WaypointUtil waypointUtil = new WaypointUtil(serverPlayer);
            waypointUtil.DeleteNearestWaypoint(request.sendChatMessageToPlayer,
                request.targetPosition, request.titlePattern,
                request.maxRadius > 0 ? request.maxRadius : double.MaxValue);
        }

        /// <summary>
        /// Server-side handler - If the client does not already have Auto Map Marker settings saved, they will
        /// make this request to the server to download the server's default settings for Auto Map Marker mod.
        /// </summary>
        /// <remarks>
        /// Side: server only
        /// </remarks>
        private void OnClientDefaultSettingsRequest(IPlayer fromPlayer, ClientDefaultSettingsRequest request)
        {
            string settingsPath = Path.Combine(
                MapMarkerConfig.GetModConfigPath(CoreAPI),
                MarkerSettingsPersistence.SettingsFolder,
                MarkerSettingsPersistence.SettingsFileName);

            string json = File.Exists(settingsPath) ? File.ReadAllText(settingsPath) : null;
            ServerNetworkChannel.SendPacket(
                new ServerSettingsResponse { SettingsJson = json },
                new[] { fromPlayer as IServerPlayer });
        }

        /// <summary>
        /// Client-side handler - Accepts default Auto Map Marker settings from the server.
        /// </summary>
        /// <remarks>
        /// Side: client only
        /// </remarks>
        private void OnReceiveDefaultSettingsFromServer(ServerSettingsResponse response)
        {
            if (!string.IsNullOrEmpty(response.SettingsJson))
            {
                string settingsDir = Path.Combine(
                    MapMarkerConfig.GetModConfigPath(CoreAPI),
                    MarkerSettingsPersistence.SettingsFolder);
                if (!Directory.Exists(settingsDir))
                    Directory.CreateDirectory(settingsDir);

                string settingsPath = Path.Combine(settingsDir, MarkerSettingsPersistence.SettingsFileName);
                File.WriteAllText(settingsPath, response.SettingsJson);
                MessageUtil.Log("Received and saved default settings from server.");
            }
            MapMarkerConfig.ClearCachedClientSettings();
        }

        /// <summary>
        /// Validates that a client request can proceed: correct side, mod enabled.
        /// Returns the current mod settings if valid, or null if the request should be aborted.
        /// </summary>
        /// <remarks>
        /// Connectivity is deliberately not checked here - a missing server-side mod is not a failure,
        /// it just selects the client-side transport instead.
        /// </remarks>
        private MapMarkerConfig.Settings ValidateClientRequest(string operationName)
        {
            if (Side != EnumAppSide.Client)
            {
                MessageUtil.LogError($"{operationName} unexpectedly requested from server-side thread.");
                return null;
            }
            var modSettings = MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
            if (modSettings == null || modSettings.DisableAllModFeatures)
            {
                MessageUtil.Log($"Suppressed {operationName} - mod features are currently disabled.");
                return null;
            }
            return modSettings;
        }

        /// <summary>
        /// Method called by a client to request the creation of a waypoint. Handled by the server if
        /// the mod is installed there, otherwise by the client itself.
        /// </summary>
        /// <remarks>
        /// Side: client only
        /// </remarks>
        public void RequestWaypointFromServer(Vec3d position, AutoMapMarkerSetting settings, bool sendChatMessage, string dynamicTitleComponent = null)
        {
            var modSettings = ValidateClientRequest("map marker creation");
            if (modSettings == null) return;

            if (!UseServerChannel)
            {
                ClientWaypointCommands.AddWaypoint(position, settings, sendChatMessage,
                    dynamicTitleComponent, modSettings.LabelCoordinates);
                return;
            }

            var waypointRequest = new ClientWaypointRequest
            {
                waypointPosition = position,
                waypointSettings = settings,
                sendChatMessageToPlayer = sendChatMessage,
                dynamicTitleComponent = dynamicTitleComponent,
                includeCoordinates = modSettings.LabelCoordinates
            };
            ClientNetworkChannel.SendPacket(waypointRequest);
        }

        /// <summary>
        /// Method called by a client to request the deletion of the waypoint nearest to the player.
        /// </summary>
        /// <remarks>
        /// Side: client only
        /// </remarks>
        public void RequestNearestWaypointDeletionFromServer(bool sendChatMessage)
        {
            if (ValidateClientRequest("map marker deletion") == null) return;

            if (!UseServerChannel)
            {
                ClientWaypointCommands.DeleteNearestWaypoint(sendChatMessage);
                return;
            }

            var waypointRequest = new ClientWaypointDeleteRequest
            {
                sendChatMessageToPlayer = sendChatMessage
            };
            ClientNetworkChannel.SendPacket(waypointRequest);
        }

        /// <summary>
        /// Method called by a client to request the deletion of a waypoint at a specific position
        /// matching a title pattern, within a given radius.
        /// </summary>
        /// <remarks>
        /// Side: client only
        /// </remarks>
        public void RequestWaypointDeletionAtPositionFromServer(Vec3d position, bool sendChatMessage, string titlePattern, double maxRadius)
        {
            if (ValidateClientRequest("map marker deletion") == null) return;

            if (!UseServerChannel)
            {
                ClientWaypointCommands.DeleteNearestWaypoint(sendChatMessage, position, titlePattern,
                    maxRadius > 0 ? maxRadius : double.MaxValue);
                return;
            }

            var waypointRequest = new ClientWaypointDeleteRequest
            {
                sendChatMessageToPlayer = sendChatMessage,
                targetPosition = position,
                titlePattern = titlePattern,
                maxRadius = maxRadius
            };
            ClientNetworkChannel.SendPacket(waypointRequest);
        }

        /// <summary>
        /// Initiates a handshake with the server to determine whether the mod is installed there.
        /// The result decides whether the mod's own network channel or the client-side waypoint
        /// commands are used, and whether default settings can be downloaded from the server.
        /// </summary>
        /// <remarks>
        /// Side: client only
        /// </remarks>
        public void InitiateHandshakeWithServer()
        {
            if (Side != EnumAppSide.Client)
            {
                MessageUtil.LogError("Tried to initiate handshake from server-side thread.");
                return;
            }
            ClientHandshakeTimer = new Timer(HandshakeIntervalMs);
            ClientHandshakeTimer.Elapsed += ClientConnectivityCheck;
            ClientHandshakeTimer.Start();
        }

        /// <summary>
        /// Runs after the server acknowledges the handshake from the client, or after 10 seconds elapse.
        /// </summary>
        /// <remarks>
        /// Side: client only
        /// </remarks>
        private void ClientConnectivityCheck(Object source = null, ElapsedEventArgs e = null)
        {
            if (ClientNetworkChannel.Connected)
            {
                MessageUtil.Log("Successfully established connection with the server.");
                if (!MapMarkerConfig.CheckIfSettingsExist(CoreAPI))
                {
                    MessageUtil.Log("No existing config file detected for Auto Map Marker mod. Downloading the server's default settings for Auto Map Markers.");
                    ClientNetworkChannel.SendPacket(new ClientDefaultSettingsRequest());
                }
            }
            else if (++ConnectionCheckAttempts > MaxConnectionAttempts)
            {
                if (!FallbackModeLogged)
                {
                    FallbackModeLogged = true;
                    MessageUtil.Log("Mod is not installed on the server - map markers will be created"
                        + " client-side using the game's waypoint commands.");
                    MapMarkerConfig.GetSettings(CoreAPI); //Ensure that a config file is generated
                }
            }
            else
            {
                return;
            }
            ClientHandshakeTimer?.Stop();
            ClientHandshakeTimer?.Dispose();
        }

        /// <summary>
        /// Clean up static variables.
        /// </summary>
        /// <remarks>
        /// Side: client and server
        /// </remarks>
        public void Dispose()
        {
            ClientHandshakeTimer?.Stop();
            ClientHandshakeTimer?.Dispose();
        }
    }
}
