using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Egocarib.ImmersiveCorpseDrop
{
    public class ImmersiveCorpseDropMod : ModSystem
    {
        //public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

#if DEBUG
            Harmony.DEBUG = true;
#endif

            Harmony harmony = new Harmony("egocarib:ImmersiveCorpseDropMod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

#if DEBUG
            Harmony.DEBUG = true;
#endif

            Harmony harmony = new Harmony("egocarib:ImmersiveCorpseDropMod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void Dispose()
        {
            new Harmony("egocarib:ImmersiveCorpseDropMod").UnpatchAll();
        }
    }
}