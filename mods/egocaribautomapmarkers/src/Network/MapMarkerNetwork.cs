﻿using Egocarib.AutoMapMarkers.Settings;
using Egocarib.AutoMapMarkers.Utilities;
using ProtoBuf;
using System;
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
    }

    [ProtoContract]
    public class ClientDefaultSettingsRequest
    {
        //Stub class used only for making an identifiable request to the server
    }

    /// <summary>
    /// Network handler for Auto Map Markers mod. Facilitates communication between the
    /// client, which may request that a waypoint be created, and the server, which creates 
    /// the waypoints and syncs them back to the client.
    /// </summary>
    public class MapMarkerNetwork
    {
        public EnumAppSide Side;
        public ICoreAPI CoreAPI;
        public IServerNetworkChannel ServerNetworkChannel;
        public IClientNetworkChannel ClientNetworkChannel;
        public const string ChannelID = "Egocarib.AutoMapMarkers.Network.MapMarkerChannel";
        private Timer ClientHandshakeTimer;
        private int ConnectionCheckAttempts = 0;

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
                .RegisterMessageType(typeof(MapMarkerConfig.Settings))
                .RegisterMessageType(typeof(ClientWaypointRequest))
                .SetMessageHandler<MapMarkerConfig.Settings>(OnReceiveDefaultSettingsFromServer);
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
                .RegisterMessageType(typeof(MapMarkerConfig.Settings))
                .RegisterMessageType(typeof(ClientWaypointRequest))
                .SetMessageHandler<ClientDefaultSettingsRequest>(OnClientDefaultSettingsRequest)
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
                MessageUtil.LogError("Couldn't create waypoint - unable to resolve ServerPlayer associated with request.");
                return;
            }
            WaypointUtil waypointUtil = new WaypointUtil(serverPlayer);
            waypointUtil.AddWaypoint(request.waypointPosition, request.waypointSettings, request.sendChatMessageToPlayer);
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
            ServerNetworkChannel.SendPacket(MapMarkerConfig.GetSettings(CoreAPI, true), new[] { fromPlayer as IServerPlayer });
        }

        /// <summary>
        /// Client-side handler - Accepts default Auto Map Marker settings from the server.
        /// </summary>
        /// <remarks>
        /// Side: client only
        /// </remarks>
        private void OnReceiveDefaultSettingsFromServer(MapMarkerConfig.Settings response)
        {
            MapMarkerConfig.SaveSettings(CoreAPI, response);
        }

        /// <summary>
        /// Method called by a client to request that the server create a waypoint on behalf of the client.
        /// </summary>
        /// <remarks>
        /// Side: client only
        /// </remarks>
        public void RequestWaypointFromServer(Vec3d position, AutoMapMarkerSetting settings, bool sendChatMessage)
        {
            if (Side != EnumAppSide.Client)
            {
                MessageUtil.LogError("New waypoint unexpectedly requested from server-side thread.");
                return;
            }
            if (MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).DisableAllModFeatures)
            {
                MessageUtil.Log("Suppressed automatic waypoint creation - mod features are currently disabled.");
                return;
            }
            if (!ClientNetworkChannel.Connected)
            {
                MessageUtil.LogError("Not connected to mod instance on server - unable to request waypoint creation.");
                return;
            }
            var waypointRequest = new ClientWaypointRequest
            {
                waypointPosition = position,
                waypointSettings = settings,
                sendChatMessageToPlayer = sendChatMessage
            };
            ClientNetworkChannel.SendPacket(waypointRequest);
        }

        /// <summary>
        /// Initiates a handshake with the server to confirm that the mod is installed on the server.
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
            ClientHandshakeTimer = new Timer(1000);
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
                if (!MapMarkerConfig.CheckIfSettingsExist())
                {
                    MessageUtil.Log("No existing config file detected for Auto Map Marker mod. Downloading the server's default settings for Auto Map Markers.");
                    ClientNetworkChannel.SendPacket(new ClientDefaultSettingsRequest());
                }
            }
            else if (++ConnectionCheckAttempts > 9)
            {
                MessageUtil.Chat(Lang.Get("egocarib-mapmarkers:server-warning"));
                MapMarkerConfig.GetSettings(CoreAPI); //Ensure that a config file is generated
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
