//using HarmonyLib;
//using System;
//using Vintagestory.API.Common;
//using Vintagestory.API.MathTools;

//namespace Egocarib.AutoMapMarkers.Patches
//{
//    [HarmonyPatch]
//    public class EntityTrader
//    {
//        /// <summary>
//        /// Enables surfacing of the OnInteract event to our TraderMarkerBehavior EntityBehavior that is attached to EntityTrader.
//        /// Required due to this issue: https://github.com/anegostudios/VintageStory-Issues/issues/1105
//        /// </summary>
//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(Vintagestory.GameContent.EntityTrader), "OnInteract")]
//        public static void Prefix(Vintagestory.GameContent.EntityTrader __instance, EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode)
//        {
//            if (mode != EnumInteractMode.Interact || !(byEntity is EntityPlayer))
//            {
//                return; //game's implementation already calls base.OnInteract in this scenario
//            }
//            else
//            {
//                Base_OnInteract(__instance, byEntity, slot, hitPosition, mode);
//            }
//        }

//        /// <summary>
//        /// Reverse patch stub, used to call the base implementation of OnInteract from our other patch above.
//        /// Method signature needs to match the original. The code herein will never actually be called.
//        /// </summary>
//        [HarmonyReversePatch]
//        [HarmonyPatch(typeof(Vintagestory.API.Common.EntityAgent), "OnInteract")]
//        public static void Base_OnInteract(object instance, EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
