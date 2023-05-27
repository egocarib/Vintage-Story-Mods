using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Egocarib.ImmersiveCorpseDrop
{
    [HarmonyPatch(typeof(EntityBehaviorHarvestable))]
    public static class ImmersiveCorpseDropPatch
    {
        [HarmonyPatch("OnInteract")]
        [HarmonyPrefix]
        static bool Prefix(EntityBehaviorHarvestable __instance, EntityAgent byEntity, ref InventoryGeneric ___inv)
        {
            bool inRange = (byEntity.World.Side == EnumAppSide.Client && byEntity.Pos.SquareDistanceTo(__instance.entity.Pos) <= 5) || (byEntity.World.Side == EnumAppSide.Server && byEntity.Pos.SquareDistanceTo(__instance.entity.Pos) <= 14);
            if (!__instance.IsHarvested || !inRange)
            {
                return false;
            }
            ___inv?.DropAll(__instance.entity.Pos.XYZ);
            if (__instance.entity.GetBehavior<EntityBehaviorDeadDecay>() != null)
            {
                __instance.entity.GetBehavior<EntityBehaviorDeadDecay>().DecayNow();
            }
            return false;
        }
    }
}