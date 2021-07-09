using Egocarib.AutoMapMarkers.Utilities;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
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
    }

    /// <summary>
    /// Network handler for Auto Map Markers mod. Facilitates communication between the
    /// client, which may request that a waypoint be created, and the server, which creates 
    /// the waypoints and syncs them back to the client.
    /// </summary>
    public class MapMarkerNetwork
    {
        public EnumAppSide Side;
        public IServerNetworkChannel ServerNetworkChannel;
        public IClientNetworkChannel ClientNetworkChannel;
        public const string ChannelID = "Egocarib.AutoMapMarkers.Network.MapMarkerChannel";

        public MapMarkerNetwork(ICoreAPI api)
        {
            Side = api.Side;
            if (Side == EnumAppSide.Server)
            {
                ConfigureServer(api as ICoreServerAPI);
            }
            else
            {
                ConfigureClient(api as ICoreClientAPI);
            }
        }

        /// <summary>
        /// Configures the client side of the network channel.
        /// </summary>
        private void ConfigureClient(ICoreClientAPI clientAPI)
        {
            ClientNetworkChannel = clientAPI.Network
                .RegisterChannel(ChannelID)
                .RegisterMessageType(typeof(ClientWaypointRequest));
        }

        /// <summary>
        /// Configures the server side of the network channel.
        /// </summary>
        private void ConfigureServer(ICoreServerAPI serverAPI)
        {
            ServerNetworkChannel = serverAPI.Network
                .RegisterChannel(ChannelID)
                .RegisterMessageType(typeof(ClientWaypointRequest))
                .SetMessageHandler<ClientWaypointRequest>(OnClientMessage);
        }

        /// <summary>
        /// Server-side handler - receives a waypoint creation request arriving from a client.
        /// </summary>
        private void OnClientMessage(IPlayer fromPlayer, ClientWaypointRequest request)
        {
            IServerPlayer serverPlayer = fromPlayer as IServerPlayer;
            if (serverPlayer == null)
            {
                MessageUtil.LogError("Couldn't create waypoint - unable to resolve ServerPlayer associated with request.");
                return;
            }
            WaypointUtil waypointUtil = new WaypointUtil(serverPlayer);
            waypointUtil.AddWaypoint(request.waypointPosition, request.waypointSettings);
        }

        /// <summary>
        /// Method called by a client to request that the server create a waypoint on behalf of the client.
        /// </summary>
        public void RequestWaypointFromServer(Vec3d position, AutoMapMarkerSetting settings)
        {
            if (Side != EnumAppSide.Client)
            {
                MessageUtil.LogError("New waypoint unexpectedly requested from server-side thread.");
                return;
            }
            var waypointRequest = new ClientWaypointRequest
            {
                waypointPosition = position,
                waypointSettings = settings
            };
            ClientNetworkChannel.SendPacket(waypointRequest);
        }

    }
}
