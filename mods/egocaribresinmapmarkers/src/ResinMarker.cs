using System.Linq;
using System.Reflection;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ResinMarkerMod
{
    public class ResinMarkerMod : ModSystem
    {
        public static WorldMapManager MapManager;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockBehaviorClass("egocarib_ResinMarker", typeof(ResinMarker));
            MapManager = api.ModLoader.GetModSystem<WorldMapManager>();
        }
    }

    class ResinMarker : BlockBehavior
    {
        public string MarkerColor;
        public string MarkerIcon;
        public string MarkerTitle;

        public ResinMarker(Block block) : base(block)
        {
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            MarkerColor = properties["markerColor"].AsString("orange");
            MarkerIcon = properties["markerIcon"].AsString("circle");
            MarkerTitle = properties["markerTitle"].AsString("Resin");
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            //add the waypoint
            //sometimes byPlayer (even when a valid object) cannot be successfully cast to IServerPlayer. This happens if the harvest was not fully completed.
            if (blockSel != null && blockSel.Position != null && byPlayer != null && (byPlayer as IServerPlayer) != null)
            {
                Vec3d resinBlockPosition = new Vec3d(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);
                AddWaypoint(resinBlockPosition, byPlayer as IServerPlayer);
            }
            handling = EnumHandling.PreventDefault;
            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel, ref handling);
        }

        public void AddWaypoint(Vec3d position, IServerPlayer serverPlayer)
        {
            WaypointMapLayer waypointLayer = ResinMarkerMod.MapManager.MapLayers.FirstOrDefault((MapLayer ml) => ml.GetType() == typeof(WaypointMapLayer)) as WaypointMapLayer;
            if (waypointLayer != null)
            {
                string curPos = position.ToString();
                foreach (Waypoint waypoint in waypointLayer.Waypoints)
                {
                    if (waypoint.Position.ToString() == curPos && waypoint.Title == MarkerTitle && waypoint.Icon == MarkerIcon)
                    {
                        return; //Don't create duplicate waypoints
                    }
                }


                MethodInfo addWaypointMethod = typeof(WaypointMapLayer).GetMethod("AddWp", BindingFlags.NonPublic | BindingFlags.Instance);
                //private void AddWp(Vec3d pos, CmdArgs args, IServerPlayer player, int groupId, string icon, bool pinned)
                if (addWaypointMethod != null)
                {
                    CmdArgs args = new CmdArgs(new string[] { MarkerColor, MarkerTitle });
                    //serverPlayer.SendMessage(GlobalConstants.ConsoleGroup, "addWaypointMethod " + (addWaypointMethod == null ? "is NULL" : "has value"), EnumChatType.CommandSuccess);
                    //serverPlayer.SendMessage(GlobalConstants.ConsoleGroup, "waypointLayer " + (waypointLayer == null ? "is NULL" : "has value"), EnumChatType.CommandSuccess);
                    //serverPlayer.SendMessage(GlobalConstants.ConsoleGroup, "position " + (position == null ? "is NULL" : "has value"), EnumChatType.CommandSuccess);
                    //serverPlayer.SendMessage(GlobalConstants.ConsoleGroup, "args " + (args == null ? "is NULL" : "has value"), EnumChatType.CommandSuccess);
                    //serverPlayer.SendMessage(GlobalConstants.ConsoleGroup, "serverPlayer " + (serverPlayer == null ? "is NULL" : "has value"), EnumChatType.CommandSuccess);
                    //serverPlayer.SendMessage(GlobalConstants.ConsoleGroup, "waypointLayer " + (waypointLayer == null ? "is NULL" : "has value"), EnumChatType.CommandSuccess);
                    try
                    {
                        addWaypointMethod.Invoke(waypointLayer, new object[] { position, args, serverPlayer, GlobalConstants.ConsoleGroup, MarkerIcon, false });
                    }
                    catch
                    {
                        serverPlayer.SendMessage(GlobalConstants.ConsoleGroup, "FAILED to set waypoint", EnumChatType.CommandSuccess);
                    }
                    //TODO: re-implement AddWp (https://github.com/anegostudios/vsessentialsmod/blob/da3b10aa8fa451b421b06489e4c9329ca78b2da0/Systems/WorldMap/WaypointLayer/WaypointMapLayer.cs#L266-L323)
                    //      so that it doesn't send a message to the chat (right now I'm directing it to the console, but it'd be better if nothing happened, or if it was a custom message I made up)
                    //      Re-implementing this would also make the mod less likely to break if AddWp's signature changes.
                }
            }
        }
    }
}