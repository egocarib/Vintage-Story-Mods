using Egocarib.AutoMapMarkers.Utilities;
using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;

namespace Egocarib.AutoMapMarkers.Patches
{
    public static class HarmonyAgent
    {
        public static Harmony harmonyInstance;
        public const string harmonyID = "Egocarib.AutoMapMarkers.Patches";

        /// <summary>
        /// Applies the mod's Harmony patches.
        /// </summary>
        public static void Harmonize(EnumAppSide side)
        {
            if (harmonyInstance == null)
            {
                harmonyInstance = new Harmony(harmonyID);
                harmonyInstance.PatchAll(Assembly.GetAssembly(typeof(HarmonyAgent)));
            }

            // Chat suppression targets a method resolved at runtime, so it can't go through PatchAll,
            // and it only makes sense on the client. This sits outside the guard above because in
            // single player both sides share this static Harmony instance and the server side gets
            // here first, which would otherwise skip the client patch entirely.
            if (side == EnumAppSide.Client)
            {
                ChatSuppression.ApplyPatch(harmonyInstance);
            }

            string patchedMethods = "";
            foreach (var method in harmonyInstance.GetPatchedMethods())
            {
                patchedMethods += string.IsNullOrEmpty(patchedMethods) ? "" : ", ";
                patchedMethods += method.Name;
            }
            MessageUtil.Log("Patched methods: " + patchedMethods);
        }

        /// <summary>
        /// Removes the mod's Harmony patches and disposes of the Harmony instance.
        /// </summary>
        public static void Deharmonize()
        {
            harmonyInstance?.UnpatchAll(harmonyID);
            harmonyInstance = null;
            ChatSuppression.Reset();
        }
    }
}
