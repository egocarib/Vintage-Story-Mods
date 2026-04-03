using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Datastructures;

namespace Icebreaker
{
    public class IcebreakerModSystem : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterEntityBehaviorClass("icebreaker", typeof(EntityBehaviorIcebreaker));
        }
    }

    public class EntityBehaviorIcebreaker : EntityBehavior
    {
        ICoreServerAPI sapi;
        long tickListenerId;

        // Position delta tracking - track actual world movement to find the real forward direction
        double prevX = double.NaN;
        double prevZ = double.NaN;
        double forwardX = 0;
        double forwardZ = 0;
        bool hasForward = false;

        public EntityBehaviorIcebreaker(Entity entity) : base(entity)
        {
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            if (entity.Api is ICoreServerAPI serverApi)
            {
                this.sapi = serverApi;
                tickListenerId = sapi.Event.RegisterGameTickListener(OnTick, 100);
            }
        }

        public override void OnEntityDespawn(EntityDespawnData reason)
        {
            if (sapi != null)
            {
                sapi.Event.UnregisterGameTickListener(tickListenerId);
            }
            base.OnEntityDespawn(reason);
        }

        private bool IsIceBlock(Block block)
        {
            if (block == null || block.Code == null) return false;
            string path = block.Code.Path;
            if (block.BlockMaterial == EnumBlockMaterial.Ice) return true;
            if (path.Contains("pumice")) return false;
            if (path.Contains("ice") || path.Contains("glacier") || path.Contains("frozen")) return true;
            return false;
        }

        private bool HasIceAtPos(BlockPos bpos)
        {
            for (int layer = 0; layer <= 2; layer++)
            {
                Block block = sapi.World.BlockAccessor.GetBlock(bpos, layer);
                if (IsIceBlock(block)) return true;
            }
            return false;
        }

        private void BreakIceInRadius(BlockPos center, int radius)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        BlockPos bpos = center.AddCopy(x, y, z);
                        if (HasIceAtPos(bpos))
                        {
                            sapi.World.BlockAccessor.BreakBlock(bpos, null);
                        }
                    }
                }
            }
        }

        private void OnTick(float dt)
        {
            if (entity.Alive && entity.Pos != null)
            {
                try
                {
                    double curX = entity.Pos.X;
                    double curZ = entity.Pos.Z;

                    // Track actual position deltas to determine real travel direction
                    if (!double.IsNaN(prevX))
                    {
                        double dx = curX - prevX;
                        double dz = curZ - prevZ;
                        double dist = System.Math.Sqrt(dx * dx + dz * dz);

                        if (dist > 0.01)
                        {
                            forwardX = dx / dist;
                            forwardZ = dz / dist;
                            hasForward = true;
                        }
                    }

                    prevX = curX;
                    prevZ = curZ;

                    // Always break ice around the boat center (small radius for hull protection)
                    BlockPos boatCenter = entity.Pos.AsBlockPos;
                    BreakIceInRadius(boatCenter, 2);

                    // If we know the travel direction, also break ice at the tip
                    if (hasForward)
                    {
                        double tipX = curX + forwardX * 5.0;
                        double tipZ = curZ + forwardZ * 5.0;
                        BlockPos tipPos = new BlockPos((int)tipX, (int)entity.Pos.Y, (int)tipZ);
                        BreakIceInRadius(tipPos, 2);
                    }
                }
                catch (System.Exception)
                {
                    // Suppress exceptions from unloaded chunks etc.
                }
            }
        }

        public override string PropertyName()
        {
            return "icebreaker";
        }
    }
}
