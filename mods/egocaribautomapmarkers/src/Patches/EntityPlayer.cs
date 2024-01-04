//using HarmonyLib;
//using System;
//using Vintagestory.API.Common;
//using Vintagestory.API.Common.Entities;
//using Vintagestory.API.MathTools;

//namespace Egocarib.AutoMapMarkers.Patches
//{
//    [HarmonyPatch]
//    public class EntityPlayer
//    {
//        /// <summary>
//        /// Enables surfacing of the OnGameTick event to players. During this tick, we check whether
//        /// The player is viewing something that needs to be marked on the map. This check occurs only
//        /// if the player is sneaking.
//        /// </summary>
//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(Vintagestory.API.Common.EntityPlayer), "OnGameTick")]
//        public static void Postfix(Vintagestory.API.Common.EntityPlayer __instance)
//        {
//
//        }
//    }
//}
