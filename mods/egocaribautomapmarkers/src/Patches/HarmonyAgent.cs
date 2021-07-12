using Egocarib.AutoMapMarkers.Utilities;
using HarmonyLib;
using System.Reflection;

namespace Egocarib.AutoMapMarkers.Patches
{
    public static class HarmonyAgent
    {
        public static Harmony harmonyInstance;
        public const string harmonyID = "Egocarib.AutoMapMarkers.Patches";

        /// <summary>
        /// Applies the mod's Harmony patches.
        /// </summary>
        public static void Harmonize()
        {
            if (harmonyInstance == null)
            {
                harmonyInstance = new Harmony(harmonyID);
                harmonyInstance.PatchAll(Assembly.GetAssembly(typeof(HarmonyAgent)));
                string patchedMethods = "";
                foreach (var method in harmonyInstance.GetPatchedMethods())
                {
                    patchedMethods += string.IsNullOrEmpty(patchedMethods) ? "" : ", ";
                    patchedMethods += method.Name;
                }
                MessageUtil.Log("Patched methods: " + patchedMethods);
            }
        }

        /// <summary>
        /// Removes the mod's Harmony patches and disposes of the Harmony instance.
        /// </summary>
        public static void Deharmonize()
        {
            harmonyInstance?.UnpatchAll(harmonyID);
            harmonyInstance = null;
        }
    }
}
