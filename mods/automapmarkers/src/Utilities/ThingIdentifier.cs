using System;
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

        public enum IdentifyAsType
        {
            Any,
            Flora,
            DynamicFlora,
            SurfaceOre,
            DeepOre,
            Misc,
            Trader
        }

        public bool Identify(MapMarkerConfig.Settings config, IdentifyAsType type = IdentifyAsType.Any)
        {
            ModConfig = config;
            return Identify(type);
        }

        public bool Identify(IdentifyAsType type = IdentifyAsType.Any)
        {
            if (type == IdentifyAsType.Flora)
                return IdentifyAsFlora();
            else if (type == IdentifyAsType.DynamicFlora)
                return IdentifyAsDynamicFlora();
            else if (type == IdentifyAsType.SurfaceOre)
                return IdentifyAsSurfaceOre();
            else if (type == IdentifyAsType.DeepOre)
                return IdentifyAsDeepOre();
            else if (type == IdentifyAsType.Misc)
                return IdentifyAsMisc();
            else if (type == IdentifyAsType.Trader)
                return IdentifyAsTrader();
            else if (type == IdentifyAsType.Any)
                return IdentifyAsFlora()
                    || IdentifyAsDynamicFlora()
                    || IdentifyAsSurfaceOre()
                    || IdentifyAsDeepOre()
                    || IdentifyAsMisc()
                    || IdentifyAsTrader();
            return false;
        }

        private bool IdentifyAsFlora()
        {
            if (IsResin)
                MarkerSettings = ModConfig?.AutoMapMarkers.OrganicMatter.Resin;
            else if (IsBlueberry)
                MarkerSettings = ModConfig?.AutoMapMarkers.OrganicMatter.Blueberry;
            else if (IsCranberry)
                MarkerSettings = ModConfig?.AutoMapMarkers.OrganicMatter.Cranberry;
            else if (IsCurrantBlack)
                MarkerSettings = ModConfig?.AutoMapMarkers.OrganicMatter.BlackCurrant;
            else if (IsCurrantRed)
                MarkerSettings = ModConfig?.AutoMapMarkers.OrganicMatter.RedCurrant;
            else if (IsCurrantWhite)
                MarkerSettings = ModConfig?.AutoMapMarkers.OrganicMatter.WhiteCurrant;
            else
                return false;

            Identified = true;
            return true;
        }

        private bool IdentifyAsDynamicFlora()
        {
            if (IsMushroomAny)
                MarkerSettings = MushroomNotPoisonous() ?
                    ModConfig?.AutoMapMarkers.OrganicMatter.SafeMushroom :
                    ModConfig?.AutoMapMarkers.OrganicMatter.UnsafeMushroom;
            else if (IsReedAny)
                MarkerSettings = ModConfig?.AutoMapMarkers.OrganicMatter.Reed;
            else if (IsWildCropAny)
                MarkerSettings = ModConfig?.AutoMapMarkers.OrganicMatter.WildCrop;
            else if (IsFlowerAny)
                MarkerSettings = ModConfig?.AutoMapMarkers.OrganicMatter.Flower;
            else if (IsFruitTreeAny)
                MarkerSettings = ModConfig?.AutoMapMarkers.OrganicMatter.FruitTree;
            else
                return false;

            DynamicTitleComponent = CalculateDynamicMarkerTitleComponent();

            Identified = true;
            return true;
        }

        private bool IdentifyAsSurfaceOre()
        {
            if (IsAnthracite)
                MarkerSettings = ModConfig?.AutoMapMarkers.SurfaceOre.LooseOreAnthracite;
            else if (IsBlackCoal)
                MarkerSettings = ModConfig?.AutoMapMarkers.SurfaceOre.LooseOreBlackCoal;
            else if (IsBorax)
                MarkerSettings = ModConfig?.AutoMapMarkers.SurfaceOre.LooseOreBorax;
            else if (IsBrownCoal)
                MarkerSettings = ModConfig?.AutoMapMarkers.SurfaceOre.LooseOreBrownCoal;
            else if (IsCinnabar)
                MarkerSettings = ModConfig?.AutoMapMarkers.SurfaceOre.LooseOreCinnabar;
            else if (IsGold)
                MarkerSettings = ModConfig?.AutoMapMarkers.SurfaceOre.LooseOreGold;
            else if (IsLapisLazuli)
                MarkerSettings = ModConfig?.AutoMapMarkers.SurfaceOre.LooseOreLapisLazuli;
            else if (IsLead)
                MarkerSettings = ModConfig?.AutoMapMarkers.SurfaceOre.LooseOreLead;
            else if (IsMalachiteCopper)
                MarkerSettings = ModConfig?.AutoMapMarkers.SurfaceOre.LooseOreCopper;
            else if (IsNativeCopper)
                MarkerSettings = ModConfig?.AutoMapMarkers.SurfaceOre.LooseOreCopper;
            else if (IsOlivine)
                MarkerSettings = ModConfig?.AutoMapMarkers.SurfaceOre.LooseOreOlivine;
            else if (IsQuartz)
                MarkerSettings = ModConfig?.AutoMapMarkers.SurfaceOre.LooseOreQuartz;
            else if (IsSilver)
                MarkerSettings = ModConfig?.AutoMapMarkers.SurfaceOre.LooseOreSilver;
            else if (IsSulfur)
                MarkerSettings = ModConfig?.AutoMapMarkers.SurfaceOre.LooseOreSulfur;
            else if (IsTin)
                MarkerSettings = ModConfig?.AutoMapMarkers.SurfaceOre.LooseOreTin;
            else
                return false;

            Identified = true;
            return true;
        }

        private bool IdentifyAsDeepOre()
        {
            if (IsAnthraciteBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreAnthracite;
            else if (IsBismuthBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreBismuth;
            else if (IsBlackCoalBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreBlackCoal;
            else if (IsBoraxBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreBorax;
            else if (IsBrownCoalBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreBrownCoal;
            else if (IsCinnabarBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreCinnabar;
            else if (IsGoldBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreGold;
            else if (IsHematiteIronBlock || IsLimoniteIronBlock || IsMagnetiteIronBlock)
            {
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreIron;
                DynamicTitleComponent = CalculateDynamicMarkerTitleComponent();
            }
            else if (IsLapisLazuliBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreLapisLazuli;
            else if (IsLeadBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreLead;
            else if (IsMalachiteCopperBlock || IsNativeCopperBlock)
            {
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreCopper;
                DynamicTitleComponent = CalculateDynamicMarkerTitleComponent();
            }
            else if (IsOlivineBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreOlivine;
            else if (IsQuartzBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreQuartz;
            else if (IsSilverBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreSilver;
            else if (IsSulfurBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreSulfur;
            else if (IsTinBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreTin;
            else if (IsTitaniumBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreTitanium;
            else if (IsZincBlock)
                MarkerSettings = ModConfig?.AutoMapMarkers.DeepOre.DeepOreZinc;
            else
                return false;

            Identified = true;
            return true;
        }

        private bool IdentifyAsMisc()
        {
            if (IsBlueClay)
                MarkerSettings = ModConfig?.AutoMapMarkers.MiscBlocks.BlockBlueClay;
            else if (IsFireClay)
                MarkerSettings = ModConfig?.AutoMapMarkers.MiscBlocks.BlockFireClay;
            else if (IsPeat)
                MarkerSettings = ModConfig?.AutoMapMarkers.MiscBlocks.BlockPeat;
            else if (IsHighFertSoil)
                MarkerSettings = ModConfig?.AutoMapMarkers.MiscBlocks.BlockHighFertilitySoil;
            else if (IsMeteoriticIron)
                MarkerSettings = ModConfig?.AutoMapMarkers.MiscBlocks.BlockMeteoriticIron;
            else if (IsSaltpeter)
                MarkerSettings = ModConfig?.AutoMapMarkers.MiscBlocks.BlockCoatingSaltpeter;
            else if (IsBeehive)
                MarkerSettings = ModConfig?.AutoMapMarkers.MiscBlocks.Beehive;
            else if (IsTranslocator)
                MarkerSettings = ModConfig?.AutoMapMarkers.MiscBlocks.Translocator;
            else
                return false;

            Identified = true;
            return true;
        }

        private bool IdentifyAsTrader()
        {
            if (IsArtisan)
                MarkerSettings = ModConfig?.AutoMapMarkers.Traders.TraderArtisan;
            else if (IsBuildingMaterials)
                MarkerSettings = ModConfig?.AutoMapMarkers.Traders.TraderBuildingMaterials;
            else if (IsClothing)
                MarkerSettings = ModConfig?.AutoMapMarkers.Traders.TraderClothing;
            else if (IsCommodities)
                MarkerSettings = ModConfig?.AutoMapMarkers.Traders.TraderCommodities;
            else if (IsFoods)
                MarkerSettings = ModConfig?.AutoMapMarkers.Traders.TraderFoods;
            else if (IsFurniture)
                MarkerSettings = ModConfig?.AutoMapMarkers.Traders.TraderFurniture;
            else if (IsLuxuries)
                MarkerSettings = ModConfig?.AutoMapMarkers.Traders.TraderLuxuries;
            else if (IsSurvivalGoods)
                MarkerSettings = ModConfig?.AutoMapMarkers.Traders.TraderSurvivalGoods;
            else if (IsTreasureHunter)
                MarkerSettings = ModConfig?.AutoMapMarkers.Traders.TraderTreasureHunter;
            else
                return false;

            Identified = true;
            return true;
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
            return BlockOrEntity.Code.Path;
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
                Entity entity = BlockOrEntity as Entity;
                return entity.GetName();
            }
            return null;
        }

        private string CalculateFruitTreeName(string displayName)
        {
            string treeType = null;
            var entity = MapMarkerMod.CoreAPI.World.BlockAccessor.GetBlockEntity(BlockPosition);
            if (entity is BlockEntityFruitTreeBranch branchEntity)
                treeType = branchEntity.TreeType;
            else if (entity is BlockEntityFruitTreeFoliage foliageEntity)
                treeType = foliageEntity.TreeType;

            if (treeType != null)
            {
                if (treeType == "redapple")
                    return Lang.Get("item-fruit-redapple");
                if (treeType == "pinkapple")
                    return Lang.Get("item-fruit-pinkapple");
                if (treeType == "yellowapple")
                    return Lang.Get("item-fruit-yellowapple");
                if (treeType == "cherry")
                    return Lang.Get("item-fruit-cherry");
                if (treeType == "olive")
                    return Lang.Get("item-vegetable-olive");
                if (treeType == "peach")
                    return Lang.Get("item-fruit-peach");
                if (treeType == "pear")
                    return Lang.Get("item-fruit-pear");
                if (treeType == "mango")
                    return Lang.Get("item-fruit-mango");
                if (treeType == "orange")
                    return Lang.Get("item-fruit-orange");
                if (treeType == "breadfruit")
                    return Lang.Get("item-fruit-breadfruit");
                if (treeType == "lychee")
                    return Lang.Get("item-fruit-lychee");
                if (treeType == "pomegranate")
                    return Lang.Get("item-fruit-pomegranate");
            }
            return null;
        }

        public bool MushroomNotPoisonous()
        {
            BlockMushroom mushroomBlock = BlockOrEntity as BlockMushroom;
            if (mushroomBlock == null)
                return false;
            IClientWorldAccessor world = MapMarkerMod.CoreClientAPI.World;
            ItemStack[] drops = mushroomBlock.GetDrops(world, BlockPosition, world.Player);
            if (drops != null && drops.Length > 0 && drops[0] != null)
            {
                FoodNutritionProperties mushProps = drops[0].Collectible?.GetNutritionProperties(world, drops[0], world.Player.Entity);
                return mushProps == null || mushProps.Health >= 0f;  //null represents an inedible mushroom
            }
            return false;
        }

        // Misc blocks (soil, etc)
        public bool IsBeehive { get { return BlockOrEntity.Code.Path.StartsWith("wildbeehive-", StringComparison.Ordinal); } }
        public bool IsTranslocator { get { return BlockOrEntity.Code.Path.StartsWith("statictranslocator-", StringComparison.Ordinal); } }
        public bool IsBlueClay { get { return BlockOrEntity.Code.Path.StartsWith("rawclay-blue-", StringComparison.Ordinal); } }
        public bool IsFireClay { get { return BlockOrEntity.Code.Path.StartsWith("rawclay-fire-", StringComparison.Ordinal); } }
        public bool IsPeat { get { return BlockOrEntity.Code.Path.StartsWith("peat-", StringComparison.Ordinal); } }
        public bool IsHighFertSoil { get { return BlockOrEntity.Code.Path.StartsWith("soil-compost-", StringComparison.Ordinal); } }
        public bool IsMeteoriticIron { get { return BlockOrEntity.Code.Path.StartsWith("meteorite-iron", StringComparison.Ordinal); } }
        public bool IsSaltpeter { get { return BlockOrEntity.Code.Path.StartsWith("saltpeter-", StringComparison.Ordinal); } }

        // Flora
        public bool IsResin { get { return BlockOrEntity.Code.Path.StartsWith("log-resin-", StringComparison.Ordinal); } }
        public bool IsBlueberry { get { return BlockOrEntity.Code.Path.StartsWith("smallberrybush-blueberry-", StringComparison.Ordinal); } }
        public bool IsCranberry { get { return BlockOrEntity.Code.Path.StartsWith("smallberrybush-cranberry-", StringComparison.Ordinal); } }
        public bool IsCurrantBlack { get { return BlockOrEntity.Code.Path.StartsWith("bigberrybush-blackcurrant-", StringComparison.Ordinal); } }
        public bool IsCurrantRed { get { return BlockOrEntity.Code.Path.StartsWith("bigberrybush-redcurrant-", StringComparison.Ordinal); } }
        public bool IsCurrantWhite { get { return BlockOrEntity.Code.Path.StartsWith("bigberrybush-whitecurrant-", StringComparison.Ordinal); } }
        public bool IsMushroomAny { get { return BlockOrEntity.Code.Path.StartsWith("mushroom-", StringComparison.Ordinal); } }
        public bool IsWildCropAny { get { return BlockOrEntity.Code.Path.StartsWith("crop-", StringComparison.Ordinal); } }
        public bool IsFlowerAny { get { return BlockOrEntity.Code.Path.StartsWith("flower-", StringComparison.Ordinal) || BlockOrEntity.Code.Path.StartsWith("herb-", StringComparison.Ordinal); } }
        public bool IsReedAny { get { return BlockOrEntity.Code.Path.StartsWith("tallplant-", StringComparison.Ordinal); } }
        public bool IsFruitTreeAny { get { return BlockOrEntity.Code.Path.StartsWith("fruittree-", StringComparison.Ordinal) && !BlockOrEntity.Code.Path.Equals("fruittree-cutting", StringComparison.Ordinal); } }  // Ignore cuttings


        // Ore bits on surface
        public bool IsAnthracite { get { return BlockOrEntity.Code.Path.StartsWith("looseores-anthracite-", StringComparison.Ordinal); } }
        public bool IsBlackCoal { get { return BlockOrEntity.Code.Path.StartsWith("looseores-bituminouscoal-", StringComparison.Ordinal); } }
        public bool IsBorax { get { return BlockOrEntity.Code.Path.StartsWith("looseores-borax-", StringComparison.Ordinal); } }
        public bool IsBrownCoal { get { return BlockOrEntity.Code.Path.StartsWith("looseores-lignite-", StringComparison.Ordinal); } }
        public bool IsCinnabar { get { return BlockOrEntity.Code.Path.StartsWith("looseores-cinnabar-", StringComparison.Ordinal); } }
        public bool IsGold { get { return BlockOrEntity.Code.Path.StartsWith("looseores-quartz_nativegold-", StringComparison.Ordinal); } }
        public bool IsLapisLazuli { get { return BlockOrEntity.Code.Path.StartsWith("looseores-lapislazuli-", StringComparison.Ordinal); } }
        public bool IsLead { get { return BlockOrEntity.Code.Path.StartsWith("looseores-galena-", StringComparison.Ordinal); } }
        public bool IsMalachiteCopper { get { return BlockOrEntity.Code.Path.StartsWith("looseores-malachite-", StringComparison.Ordinal); } }
        public bool IsNativeCopper { get { return BlockOrEntity.Code.Path.StartsWith("looseores-nativecopper-", StringComparison.Ordinal); } }
        public bool IsOlivine { get { return BlockOrEntity.Code.Path.StartsWith("looseores-olivine-", StringComparison.Ordinal); } }
        public bool IsQuartz { get { return BlockOrEntity.Code.Path.StartsWith("looseores-quartz-", StringComparison.Ordinal); } }
        public bool IsSilver { get { return BlockOrEntity.Code.Path.StartsWith("looseores-", StringComparison.Ordinal) && BlockOrEntity.Code.Path.Contains("_nativesilver-"); } } // Has both quartz & galena variants
        public bool IsSulfur { get { return BlockOrEntity.Code.Path.StartsWith("looseores-sulfur-", StringComparison.Ordinal); } }
        public bool IsTin { get { return BlockOrEntity.Code.Path.StartsWith("looseores-cassiterite-", StringComparison.Ordinal); } }

        // Full mineable ore blocks
        public bool IsAnthraciteBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-anthracite-", StringComparison.Ordinal); } }
        public bool IsBismuthBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-", StringComparison.Ordinal) && BlockOrEntity.Code.Path.Contains("-bismuthinite-"); } }
        public bool IsBlackCoalBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-bituminouscoal-", StringComparison.Ordinal); } }
        public bool IsBoraxBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-borax-", StringComparison.Ordinal); } }
        public bool IsBrownCoalBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-lignite-", StringComparison.Ordinal); } }
        public bool IsCinnabarBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-cinnabar-", StringComparison.Ordinal); } }
        public bool IsGoldBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-", StringComparison.Ordinal) && BlockOrEntity.Code.Path.Contains("-quartz_nativegold-"); } }
        public bool IsHematiteIronBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-", StringComparison.Ordinal) && BlockOrEntity.Code.Path.Contains("-hematite-"); } }
        public bool IsLimoniteIronBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-", StringComparison.Ordinal) && BlockOrEntity.Code.Path.Contains("-limonite-"); } }
        public bool IsMagnetiteIronBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-", StringComparison.Ordinal) && BlockOrEntity.Code.Path.Contains("-magnetite-"); } }
        public bool IsLapisLazuliBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-lapislazuli-", StringComparison.Ordinal); } }
        public bool IsLeadBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-", StringComparison.Ordinal) && BlockOrEntity.Code.Path.Contains("-galena-"); } }
        public bool IsMalachiteCopperBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-", StringComparison.Ordinal) && BlockOrEntity.Code.Path.Contains("-malachite-"); } }
        public bool IsNativeCopperBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-", StringComparison.Ordinal) && BlockOrEntity.Code.Path.Contains("-nativecopper-"); } }
        public bool IsOlivineBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-olivine-", StringComparison.Ordinal); } }
        public bool IsQuartzBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-quartz-", StringComparison.Ordinal); } }
        public bool IsSilverBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-", StringComparison.Ordinal) && BlockOrEntity.Code.Path.Contains("_nativesilver-"); } } // Has both quartz & galena variants
        public bool IsSulfurBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-sulfur-", StringComparison.Ordinal); } }
        public bool IsTinBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-", StringComparison.Ordinal) && BlockOrEntity.Code.Path.Contains("-cassiterite-"); } }
        public bool IsTitaniumBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-", StringComparison.Ordinal) && BlockOrEntity.Code.Path.Contains("-ilmenite-"); } }
        public bool IsZincBlock { get { return BlockOrEntity.Code.Path.StartsWith("ore-", StringComparison.Ordinal) && BlockOrEntity.Code.Path.Contains("-sphalerite-"); } }

        // Trader entities
        public bool IsArtisan { get { return BlockOrEntity.Code.Path.Contains("-trader-artisan"); } }
        public bool IsBuildingMaterials { get { return BlockOrEntity.Code.Path.Contains("-trader-buildmaterials"); } }
        public bool IsClothing { get { return BlockOrEntity.Code.Path.Contains("-trader-clothing"); } }
        public bool IsCommodities { get { return BlockOrEntity.Code.Path.Contains("-trader-commodities"); } }
        public bool IsFoods { get { return BlockOrEntity.Code.Path.Contains("-trader-foods"); } }
        public bool IsFurniture { get { return BlockOrEntity.Code.Path.Contains("-trader-furniture"); } }
        public bool IsLuxuries { get { return BlockOrEntity.Code.Path.Contains("-trader-luxuries"); } }
        public bool IsSurvivalGoods { get { return BlockOrEntity.Code.Path.Contains("-trader-survivalgoods"); } }
        public bool IsTreasureHunter { get { return BlockOrEntity.Code.Path.Contains("-trader-treasurehunter"); } }
    }
}
