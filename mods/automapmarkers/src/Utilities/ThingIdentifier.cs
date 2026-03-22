using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Egocarib.AutoMapMarkers.Settings;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using Vintagestory.API.Config;

namespace Egocarib.AutoMapMarkers.Utilities
{
    /// <summary>
    /// Identifies Blocks or Entities and helps retrieve the associated mod settings and other
    /// details that are used to create map markers for them.
    /// </summary>
    public class ThingIdentifier
    {
        private RegistryObject BlockOrEntity { get; }
        private BlockPos BlockPosition { get; } = null;
        private bool Identified { get; set; } = false;
        private MapMarkerConfig.Settings.AutoMapMarkerSetting MarkerSettings { get; set; } = null;
        public string DynamicTitleComponent { get; set; } = null;
        private MapMarkerConfig.Settings _Config { get; set; } = null;
        private MapMarkerConfig.Settings ModConfig
        {
            get
            {
                _Config ??= MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI);
                return _Config;
            }
            set
            {
                _Config = value;
            }
        }

        private ThingIdentifier()
        {
            throw new NotImplementedException("can't initialize ThingIdentifier without parameter");
        }

        public ThingIdentifier(Block block, BlockPos blockPos)
        {
            BlockOrEntity = block;
            BlockPosition = blockPos;
            if (BlockOrEntity == null)
                throw new ArgumentNullException(nameof(block));
        }

        public ThingIdentifier(Entity entity)
        {
            BlockOrEntity = entity;
            if (BlockOrEntity == null)
                throw new ArgumentNullException(nameof(entity));
        }

        public bool Identify(MapMarkerConfig.Settings config)
        {
            ModConfig = config;

            var registry = MapMarkerConfig.GetRegistry();
            if (registry == null) return false;

            string assetPath = GetAssetPath();
            if (string.IsNullOrEmpty(assetPath)) return false;

            var matches = registry.FindMatches(assetPath);
            if (matches.Count == 0) return false;

            MarkerDetectionRegistry.MatchResult match;
            if (matches.Count > 1)
            {
                match = DisambiguateMatches(matches) ?? matches[0];
            }
            else
            {
                match = matches[0];
            }

            MarkerSettings = match.Setting;
            if (match.DynamicTitle)
                DynamicTitleComponent = CalculateDynamicMarkerTitleComponent();

            Identified = true;
            return true;
        }

        public bool Identify()
        {
            return Identify(ModConfig);
        }

        private static readonly HashSet<string> MushroomLabels = new HashSet<string>
        {
            "egocarib-mapmarkers:safe-mushrooms",
            "egocarib-mapmarkers:safe-mushrooms-nonpsychedelic",
            "egocarib-mapmarkers:safe-mushrooms-psychedelic",
            "egocarib-mapmarkers:unsafe-mushrooms",
            "egocarib-mapmarkers:unsafe-mushrooms-nonpsychedelic",
            "egocarib-mapmarkers:unsafe-mushrooms-psychedelic"
        };

        private const string FruitTreeLabelPrefix = "game:treegen-variant-fruittree-";
        private const string FruitTreeParentLabel = "egocarib-mapmarkers:fruit-trees";

        /// <summary>
        /// Disambiguates when multiple registry entries match the same asset path.
        /// This occurs for mushrooms (safe vs unsafe, psychedelic vs non-psychedelic) and
        /// fruit trees (tree type is in block entity data, not the block code).
        /// Returns null if no disambiguation is needed or possible.
        /// </summary>
        private MarkerDetectionRegistry.MatchResult DisambiguateMatches(List<MarkerDetectionRegistry.MatchResult> matches)
        {
            // Check for mushroom disambiguation
            int mushroomCount = 0;
            foreach (var m in matches)
                if (MushroomLabels.Contains(m.Label)) mushroomCount++;

            if (mushroomCount >= 2)
            {
                string targetLabel = ResolveMushroomLabel(matches);
                return matches.FirstOrDefault(m => m.Label == targetLabel);
            }

            // Check for fruit tree disambiguation
            int fruitTreeCount = 0;
            foreach (var m in matches)
                if (m.Label.StartsWith(FruitTreeLabelPrefix, StringComparison.Ordinal) || m.Label == FruitTreeParentLabel)
                    fruitTreeCount++;

            if (fruitTreeCount >= 2)
            {
                string targetLabel = ResolveFruitTreeLabel(matches);
                if (targetLabel != null)
                    return matches.FirstOrDefault(m => m.Label == targetLabel);
            }

            return null;
        }

        /// <summary>
        /// Resolves which mushroom label to use based on the block's nutrition properties.
        /// Handles both collapsed (parent) and expanded (sub-entry) states.
        /// </summary>
        private string ResolveMushroomLabel(List<MarkerDetectionRegistry.MatchResult> matches)
        {
            bool isSafe = MushroomNotPoisonous();
            bool isPsychedelic = MushroomIsPsychedelic();

            // Try the most specific label first (expanded sub-entries)
            string specificLabel = isSafe
                ? (isPsychedelic ? "egocarib-mapmarkers:safe-mushrooms-psychedelic" : "egocarib-mapmarkers:safe-mushrooms-nonpsychedelic")
                : (isPsychedelic ? "egocarib-mapmarkers:unsafe-mushrooms-psychedelic" : "egocarib-mapmarkers:unsafe-mushrooms-nonpsychedelic");

            if (matches.Any(m => m.Label == specificLabel))
                return specificLabel;

            // Fall back to parent label (collapsed)
            return isSafe
                ? "egocarib-mapmarkers:safe-mushrooms"
                : "egocarib-mapmarkers:unsafe-mushrooms";
        }

        /// <summary>
        /// Resolves which fruit tree label to use based on the block entity's TreeType.
        /// The tree type (cherry, apple, etc.) is stored in the block entity, not the block code.
        /// </summary>
        private string ResolveFruitTreeLabel(List<MarkerDetectionRegistry.MatchResult> matches)
        {
            string treeType = GetFruitTreeType();
            if (treeType == null)
                return FruitTreeParentLabel;

            string specificLabel = FruitTreeLabelPrefix + treeType;
            if (matches.Any(m => m.Label == specificLabel))
                return specificLabel;

            // Fall back to parent label (collapsed, or unknown tree type)
            return FruitTreeParentLabel;
        }

        /// <summary>
        /// Reads the tree type from the fruit tree block entity.
        /// </summary>
        private string GetFruitTreeType()
        {
            if (BlockPosition == null) return null;
            var entity = MapMarkerMod.CoreAPI.World.BlockAccessor.GetBlockEntity(BlockPosition);
            if (entity is BlockEntityFruitTreeBranch branchEntity)
                return branchEntity.TreeType;
            if (entity is BlockEntityFruitTreeFoliage foliageEntity)
                return foliageEntity.TreeType;
            return null;
        }

        public bool IsOnFarmland()
        {
            if (BlockPosition == null)
                return false;
            Block blockBelow = MapMarkerMod.CoreClientAPI.World.BlockAccessor.GetBlock(BlockPosition.DownCopy());
            return blockBelow?.Code?.Path?.StartsWith("farmland-", StringComparison.Ordinal) == true;
        }

        public bool IsIdentified()
        {
            return Identified;
        }

        public MapMarkerConfig.Settings.AutoMapMarkerSetting GetMapMarkerSettings()
        {
            return MarkerSettings;
        }

        public string GetAssetPath()
        {
            return BlockOrEntity?.Code?.Path ?? string.Empty;
        }

        private string CalculateDynamicMarkerTitleComponent()
        {
            if (BlockOrEntity is Block && BlockPosition != null)
            {
                Block block = BlockOrEntity as Block;
                string displayName = block.GetPlacedBlockName(MapMarkerMod.CoreAPI.World, BlockPosition);
                if (BlockOrEntity is BlockFruitTreePart)
                    return CalculateFruitTreeName(displayName);
                return displayName;
            }
            else if (BlockOrEntity is Entity)
            {
                // For traders, extract the type from the asset path (e.g. trader-male-treasurehunter-cold)
                // and resolve via our lang keys, since entity.GetName() returns "Trader" or a personal name.
                string path = GetAssetPath();
                if (path.StartsWith("trader-", StringComparison.Ordinal))
                {
                    return CalculateTraderTypeName(path);
                }
                Entity entity = BlockOrEntity as Entity;
                return entity.GetName();
            }
            return null;
        }

        /// <summary>
        /// Extracts the trader type from an asset path like "trader-male-treasurehunter-cold"
        /// and resolves it via our lang keys (e.g. "egocarib-mapmarkers:trader-treasurehunter").
        /// </summary>
        private string CalculateTraderTypeName(string assetPath)
        {
            // Pattern: trader-{gender}-{type}-{climate}
            string[] parts = assetPath.Split('-');
            if (parts.Length >= 3)
            {
                // Type is the third segment (index 2). For multi-word types this is still one segment
                // (e.g., "buildmaterials", "survivalgoods", "treasurehunter")
                string traderType = parts[2];
                string langKey = "egocarib-mapmarkers:trader-" + traderType;
                string resolved = Lang.Get(langKey);
                // If the lang key resolved (didn't return the key itself), use it
                if (resolved != langKey)
                    return resolved;
            }
            // Fallback to generic entity name
            return (BlockOrEntity as Entity)?.GetName();
        }

        private static readonly Dictionary<string, string> FruitTreeLangKeys = new()
        {
            ["redapple"] = "treegen-variant-fruittree-redapple",
            ["pinkapple"] = "treegen-variant-fruittree-pinkapple",
            ["yellowapple"] = "treegen-variant-fruittree-yellowapple",
            ["cherry"] = "treegen-variant-fruittree-cherry",
            ["olive"] = "treegen-variant-fruittree-olive",
            ["peach"] = "treegen-variant-fruittree-peach",
            ["pear"] = "treegen-variant-fruittree-pear",
            ["mango"] = "treegen-variant-fruittree-mango",
            ["orange"] = "treegen-variant-fruittree-orange",
            ["breadfruit"] = "treegen-variant-fruittree-breadfruit",
            ["lychee"] = "treegen-variant-fruittree-lychee",
            ["pomegranate"] = "treegen-variant-fruittree-pomegranate"
        };

        private string CalculateFruitTreeName(string displayName)
        {
            string treeType = GetFruitTreeType();
            if (treeType != null && FruitTreeLangKeys.TryGetValue(treeType, out string langKey))
                return Lang.Get(langKey);
            return null;
        }

        public bool MushroomNotPoisonous()
        {
            // Returns true by default if error or unexpected case occurs (such as modded mushroom with no nutrition properties)
            FoodNutritionProperties mushProps = GetMushroomNutritionProps();
            if (mushProps != null && mushProps.Health < 0f)
                return false;
            return true;
        }

        private static readonly string[] PsychedelicMushroomVarieties =
            { "flyagaric", "goldcap", "libertycap", "wavycap", "bluemeanie", "laughingjim" };

        public bool MushroomIsPsychedelic()
        {
            // Ideally we'd check NutritionProps.Psychedelic here, but that property is always 0 on the
            // client side due to a game bug: Packet_NutritionProperties and CollectibleNet don't include
            // the Psychedelic (or Intoxication) field when syncing block/item definitions from server to
            // client, so the value is lost during deserialization.
            // See: https://github.com/anegostudios/VintageStory-Issues/issues/8769
            //
            // As a workaround, we check the block's code path against known psychedelic mushroom varieties.
            FoodNutritionProperties mushProps = GetMushroomNutritionProps();
            if (mushProps != null && mushProps.Psychedelic > 0f)
                return true;

            string path = GetAssetPath();
            if (!string.IsNullOrEmpty(path))
            {
                foreach (string variety in PsychedelicMushroomVarieties)
                {
                    if (path.Contains("-" + variety + "-"))
                        return true;
                }
            }

            return false;
        }

        private FoodNutritionProperties GetMushroomNutritionProps()
        {
            BlockMushroom mushroomBlock = BlockOrEntity as BlockMushroom;
            if (mushroomBlock == null)
                return null;
            IClientWorldAccessor world = MapMarkerMod.CoreClientAPI.World;
            ItemStack[] drops = mushroomBlock.GetDrops(world, BlockPosition, world.Player);
            if (drops == null || drops.Length == 0 || drops[0] == null)
                return null;
            return drops[0].Collectible?.GetNutritionProperties(world, drops[0], world.Player.Entity);
        }

    }
}
