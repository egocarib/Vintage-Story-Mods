﻿using Egocarib.AutoMapMarkers.Utilities;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using static Egocarib.AutoMapMarkers.Settings.MapMarkerConfig.Settings;

namespace Egocarib.AutoMapMarkers.Network
{

    //[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    //public class MessageFromServer
    //{
    //    //Not implemented
    //}

    [ProtoContract]
    public class ClientWaypointRequest
    {
        [ProtoMember(1)]
        public Vec3d waypointPosition;
        [ProtoMember(2)]
        public AutoMapMarkerSetting waypointSettings;
    }

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

        private void ConfigureClient(ICoreClientAPI clientAPI)
        {
            ClientNetworkChannel = clientAPI.Network
                .RegisterChannel(ChannelID)
                //.RegisterMessageType(typeof(MessageFromServer))
                .RegisterMessageType(typeof(ClientWaypointRequest));
                //.SetMessageHandler<MessageFromServer>(OnServerMessage);
        }

        private void ConfigureServer(ICoreServerAPI serverAPI)
        {
            ServerNetworkChannel = serverAPI.Network
                .RegisterChannel(ChannelID)
                //.RegisterMessageType(typeof(MessageFromServer))
                .RegisterMessageType(typeof(ClientWaypointRequest))
                .SetMessageHandler<ClientWaypointRequest>(OnClientMessage);
        }

        //private void OnServerMessage(MessageFromServer serverMessage)
        //{

        //}

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