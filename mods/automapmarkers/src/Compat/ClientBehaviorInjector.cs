using Egocarib.AutoMapMarkers.Utilities;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using HarvestBehavior = Egocarib.AutoMapMarkers.BlockBehavior.HarvestMarkerBehavior;
using LooseOreBehavior = Egocarib.AutoMapMarkers.BlockBehavior.LooseOresMarkerBehavior;
using TraderBehavior = Egocarib.AutoMapMarkers.EntityBehavior.TraderMarkerBehavior;
using VSBlockBehavior = Vintagestory.API.Common.BlockBehavior;

namespace Egocarib.AutoMapMarkers.Compat
{
    /// <summary>
    /// Attaches this mod's marker behaviors to blocks and entities from the client side.
    /// </summary>
    /// <remarks>
    /// The behaviors are normally attached by the JSON patches in assets/automapmarkers/patches,
    /// which are all "side": "server". That works because the server sends its block and entity
    /// types (behaviors included) down to the client on join - but it also means the behaviors are
    /// missing entirely when the mod isn't installed on the server.
    ///
    /// Client-side JSON patching doesn't help either, for the same reason: in multiplayer the
    /// client's block and entity types come from the server's packets, not from the client's own
    /// parsed assets. So the behaviors are added here instead, after the types have arrived.
    ///
    /// The HasBehavior guards make this a no-op when the server does have the mod, so this runs
    /// unconditionally and only fills the gap when there is one.
    /// </remarks>
    public static class ClientBehaviorInjector
    {
        /// <summary>
        /// Block code prefixes matching the JSON patches, and the behavior each one gets:
        ///   "fruitingbush-"  from patches/fruitingbush.json  (blocktypes/plant/fruitingbush)
        ///   "log-resin"      from patches/log-withresin.json (covers log-resin-* and
        ///                    log-resinharvested-*, matching that patch's "/behaviorsByType/*/-")
        ///   "looseores-"     from patches/looseores.json     (blocktypes/stone/looseores)
        /// </summary>
        private static readonly (string prefix, Type behaviorType)[] BlockTargets =
        {
            ("fruitingbush-", typeof(HarvestBehavior)),
            ("log-resin", typeof(HarvestBehavior)),
            ("looseores-", typeof(LooseOreBehavior)),
        };

        private const string TraderBehaviorName = "egocarib_TraderMarkerBehavior";
        private const string TraderCodePrefix = "trader-";

        private static ICoreClientAPI clientAPI;

        /// <summary>
        /// Registers the injector. Blocks are handled once the world is loaded; traders are handled
        /// per entity, since entity behaviors are built when each entity is created.
        /// </summary>
        public static void Register(ICoreClientAPI capi)
        {
            if (capi == null)
                return;

            clientAPI = capi;
            capi.Event.LevelFinalize += InjectBlockBehaviors;
            capi.Event.OnEntityLoaded += InjectTraderBehavior;
            capi.Event.OnEntitySpawn += InjectTraderBehavior;
        }

        public static void Unregister(ICoreClientAPI capi)
        {
            if (capi?.Event != null)
            {
                capi.Event.LevelFinalize -= InjectBlockBehaviors;
                capi.Event.OnEntityLoaded -= InjectTraderBehavior;
                capi.Event.OnEntitySpawn -= InjectTraderBehavior;
            }
            clientAPI = null;
        }

        private static void InjectBlockBehaviors()
        {
            if (clientAPI?.World?.Blocks == null)
                return;

            int injected = 0;
            foreach (Block block in clientAPI.World.Blocks)
            {
                string path = block?.Code?.Path;
                if (string.IsNullOrEmpty(path))
                    continue;

                foreach (var (prefix, behaviorType) in BlockTargets)
                {
                    if (!path.StartsWith(prefix, StringComparison.Ordinal))
                        continue;
                    if (block.HasBehavior(behaviorType, withInheritance: false))
                        break;  // Already attached - the server has the mod and patched it in

                    if (TryAddBlockBehavior(block, behaviorType))
                        injected++;
                    break;
                }
            }

            if (injected > 0)
                MessageUtil.Log($"Attached map marker behaviors to {injected} block types client-side.");
        }

        /// <summary>
        /// Appends a block behavior the way the game's own Block.CreateBehaviors does - block
        /// behaviors live in both BlockBehaviors and CollectibleBehaviors. Appending (rather than
        /// inserting) matches the "/behaviors/-" position used by the JSON patches, so behavior
        /// precedence is unchanged.
        /// </summary>
        private static bool TryAddBlockBehavior(Block block, Type behaviorType)
        {
            try
            {
                var behavior = (VSBlockBehavior)Activator.CreateInstance(behaviorType, block);
                behavior.Initialize(new Vintagestory.API.Datastructures.JsonObject(
                    new Newtonsoft.Json.Linq.JObject()));

                block.BlockBehaviors = Append(block.BlockBehaviors, behavior);
                block.CollectibleBehaviors = Append(block.CollectibleBehaviors, behavior);
                return true;
            }
            catch (Exception e)
            {
                MessageUtil.LogError($"Unable to attach {behaviorType.Name} to block {block.Code} - {e.Message}");
                return false;
            }
        }

        private static T[] Append<T>(T[] existing, T value)
        {
            var list = new List<T>(existing ?? Array.Empty<T>()) { value };
            return list.ToArray();
        }

        private static void InjectTraderBehavior(Entity entity)
        {
            string path = entity?.Code?.Path;
            if (path == null || !path.StartsWith(TraderCodePrefix, StringComparison.Ordinal))
                return;
            if (entity.HasBehavior(TraderBehaviorName))
                return;  // Already attached - the server has the mod and patched it in

            try
            {
                entity.AddBehavior(new TraderBehavior(entity));
            }
            catch (Exception e)
            {
                MessageUtil.LogError($"Unable to attach trader marker behavior to {entity.Code} - {e.Message}");
            }
        }
    }
}
