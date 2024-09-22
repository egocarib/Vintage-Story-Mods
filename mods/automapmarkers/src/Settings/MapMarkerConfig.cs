using Egocarib.AutoMapMarkers.Utilities;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Globalization;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Egocarib.AutoMapMarkers.Settings
{
    /// <summary>
    /// Settings and configuration methods for the Auto Map Markers mod.
    /// </summary>
    public static class MapMarkerConfig
    {
        /// <summary>The filename used when saving mod settings to the ModConfig folder.</summary>
        public const string ConfigFilename = "auto_map_markers_config.json";
        /// <summary>Mod settings are cached on client side to optimize performance.</summary>
        private static Settings _cachedClientSettings = null;

        [ProtoContract]
        public class Settings
        {
            private OrderedDictionary<string, OrderedDictionary<string, AutoMapMarkerSetting>> _MapMarkerSettingsCollection = null;

            [ProtoMember(1, IsRequired = true)]
            public bool ChatNotifyOnWaypointCreation = false;
            [ProtoMember(5, IsRequired = true)]
            public bool EnableWaypointDeletionHotkey = false;
            [ProtoMember(6, IsRequired = true)]
            public bool ChatNotifyOnWaypointDeletion = true;
            [ProtoMember(7, IsRequired = true)]
            public bool EnableCustomHotkeys = false;
            [ProtoMember(8, IsRequired = true)]
            public bool EnableMarkOnSneak = true;
            [ProtoMember(9, IsRequired = true)]
            public bool EnableMarkOnInteract = true;
            [ProtoMember(10, IsRequired = true)]
            public bool LabelCoordinates = false;
            [ProtoMember(2, IsRequired = true)]
            public bool DisableAllModFeatures = true;
            [ProtoMember(3)]
            public double ConfigVersion = 3.00;  // 3.00 adds MapMarkerSettings_Misc and MapMarkerSettings_DeepOre
            [ProtoMember(4)]
            public MapMarkerSettingsGrouper AutoMapMarkers = new MapMarkerSettingsGrouper();

            [ProtoContract]
            public class MapMarkerSettingsGrouper
            {
                [ProtoMember(1)]
                public MapMarkerSettings_OrganicMatter OrganicMatter = new MapMarkerSettings_OrganicMatter();
                [ProtoMember(2)]
                public MapMarkerSettings_Ore SurfaceOre = new MapMarkerSettings_Ore();
                [ProtoMember(6)]
                public MapMarkerSettings_DeepOre DeepOre = new MapMarkerSettings_DeepOre();
                [ProtoMember(5)]
                public MapMarkerSettings_Misc MiscBlocks = new MapMarkerSettings_Misc();
                [ProtoMember(3)]
                public MapMarkerSettings_Traders Traders = new MapMarkerSettings_Traders();
                [ProtoMember(4)]
                public MapMarkerSettings_Custom Custom = new MapMarkerSettings_Custom();
            }

            [ProtoContract]
            public class MapMarkerSettings_OrganicMatter
            {
                [ProtoMember(1)]
                public AutoMapMarkerSetting Resin = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("item-resin"),
                    markerColor: "darkorange",
                    markerIcon: "circle",
                    markerCoverageRadius: 1);

                [ProtoMember(2)]
                public AutoMapMarkerSetting Blueberry = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("item-fruit-blueberry"),
                    markerColor: "midnightblue",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

                [ProtoMember(3)]
                public AutoMapMarkerSetting Cranberry = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("item-fruit-cranberry"),
                    markerColor: "maroon",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

                [ProtoMember(4)]
                public AutoMapMarkerSetting BlackCurrant = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("item-fruit-blackcurrant"),
                    markerColor: "#291B1A",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

                [ProtoMember(5)]
                public AutoMapMarkerSetting RedCurrant = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("item-fruit-redcurrant"),
                    markerColor: "darkred",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

                [ProtoMember(6)]
                public AutoMapMarkerSetting WhiteCurrant = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("item-fruit-whitecurrant"),
                    markerColor: "ivory",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

                //[ProtoMember(7)]
                //public AutoMapMarkerSetting MushroomBolete = new AutoMapMarkerSetting(
                //    enabled: false,
                //    pinned: false,
                //    markerTitle: Lang.GetMatching("block-mushroom-bolete-normal-*"),
                //    markerColor: "#503922",
                //    markerIcon: "circle",
                //    markerCoverageRadius: 6);

                //[ProtoMember(8)]
                //public AutoMapMarkerSetting MushroomFieldMushroom = new AutoMapMarkerSetting(
                //    enabled: false,
                //    pinned: false,
                //    markerTitle: Lang.GetMatching("block-mushroom-fieldmushroom-normal-*"),
                //    markerColor: "ghostwhite",
                //    markerIcon: "circle",
                //    markerCoverageRadius: 6);

                //[ProtoMember(9)]
                //public AutoMapMarkerSetting MushroomFlyAgaric = new AutoMapMarkerSetting(
                //    enabled: false,
                //    pinned: false,
                //    markerTitle: Lang.GetMatching("block-mushroom-flyagaric-normal-*"),
                //    markerColor: "brown",
                //    markerIcon: "circle",
                //    markerCoverageRadius: 6);

                [ProtoMember(10)]
                public AutoMapMarkerSetting SafeMushroom = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:safe-mushrooms"),
                    markerColor: "forestgreen",
                    markerIcon: "mushroom",
                    markerCoverageRadius: 9);

                [ProtoMember(11)]
                public AutoMapMarkerSetting UnsafeMushroom = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:unsafe-mushrooms"),
                    markerColor: "crimson",
                    markerIcon: "mushroom",
                    markerCoverageRadius: 9);

                [ProtoMember(12)]
                public AutoMapMarkerSetting WildCrop = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:wild-crops"),
                    markerColor: "goldenrod",
                    markerIcon: "grain",
                    markerCoverageRadius: 6);

                [ProtoMember(13)]
                public AutoMapMarkerSetting Flower = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:flowers"),
                    markerColor: "fuchsia",
                    markerIcon: "grain",
                    markerCoverageRadius: 9);

                [ProtoMember(14)]
                public AutoMapMarkerSetting Reed = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:reeds"),
                    markerColor: "darkkhaki",
                    markerIcon: "grain",
                    markerCoverageRadius: 20);

                [ProtoMember(16)]
                public AutoMapMarkerSetting Tule = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-tallplant-tule-*"),
                    markerColor: "darkkhaki",
                    markerIcon: "grain",
                    markerCoverageRadius: 20);

                [ProtoMember(15)]
                public AutoMapMarkerSetting FruitTree = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:fruit-trees"),
                    markerColor: "mediumpurple",
                    markerIcon: "tree",
                    markerCoverageRadius: 6);
            }

            [ProtoContract]
            public class MapMarkerSettings_Ore
            {
                [ProtoMember(1)]
                public AutoMapMarkerSetting LooseOreAnthracite = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-looseores-anthracite-*"),
                    markerColor: "black",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(2)]
                public AutoMapMarkerSetting LooseOreBlackCoal = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-looseores-bituminouscoal-*"),
                    markerColor: "black",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(3)]
                public AutoMapMarkerSetting LooseOreBorax = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-looseores-borax-*"),
                    markerColor: "ghostwhite",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(4)]
                public AutoMapMarkerSetting LooseOreBrownCoal = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-looseores-lignite-*"),
                    markerColor: "black",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(5)]
                public AutoMapMarkerSetting LooseOreCinnabar = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-looseores-cinnabar-*"),
                    markerColor: "crimson",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(6)]
                public AutoMapMarkerSetting LooseOreCopper = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-looseores-nativecopper-*"),
                    markerColor: "darkorange",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                //public AutoMapMarkerSetting LooseOreFluorite = new AutoMapMarkerSetting(
                //        enabled: false,
                //        pinned: false,
                //        markerTitle: Lang.GetMatching("block-looseores-fluorite-*"),
                //        markerColor: "black",
                //        markerIcon: "pick",
                //        markerCoverageRadius: 9);

                [ProtoMember(7)]
                public AutoMapMarkerSetting LooseOreGold = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-looseores-quartz_nativegold-*"),
                    markerColor: "gold",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                //public AutoMapMarkerSetting LooseOreGraphite = new AutoMapMarkerSetting(
                //        enabled: false,
                //        pinned: false,
                //        markerTitle: Lang.GetMatching("block-looseores-graphite-*"),
                //        markerColor: "black",
                //        markerIcon: "pick",
                //        markerCoverageRadius: 9);

                //public AutoMapMarkerSetting LooseOreKernite = new AutoMapMarkerSetting(
                //        enabled: false,
                //        pinned: false,
                //        markerTitle: Lang.GetMatching("block-looseores-kernite-*"),
                //        markerColor: "black",
                //        markerIcon: "pick",
                //        markerCoverageRadius: 9);

                [ProtoMember(8)]
                public AutoMapMarkerSetting LooseOreLapisLazuli = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-looseores-lapislazuli-*"),
                    markerColor: "royalblue",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(9)]
                public AutoMapMarkerSetting LooseOreLead = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-looseores-galena-*"),
                    markerColor: "slategray",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(10)]
                public AutoMapMarkerSetting LooseOreOlivine = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-looseores-olivine-peridotite-*"),
                    markerColor: "olivedrab",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                //public AutoMapMarkerSetting LooseOrePhosporite = new AutoMapMarkerSetting(
                //        enabled: false,
                //        pinned: false,
                //        markerTitle: Lang.GetMatching("block-looseores-phosphorite-*"),
                //        markerColor: "black",
                //        markerIcon: "pick",
                //        markerCoverageRadius: 9);

                [ProtoMember(11)]
                public AutoMapMarkerSetting LooseOreQuartz = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-looseores-quartz-*"),
                    markerColor: "white",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(12)]
                public AutoMapMarkerSetting LooseOreSilver = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-looseores-quartz_nativesilver-*"),
                    markerColor: "silver",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(13)]
                public AutoMapMarkerSetting LooseOreSulfur = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-looseores-sulfur-*"),
                    markerColor: "khaki",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(14)]
                public AutoMapMarkerSetting LooseOreTin = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-looseores-cassiterite-*"),
                    markerColor: "#3C1E05",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);
            }

            [ProtoContract]
            public class MapMarkerSettings_DeepOre
            {
                [ProtoMember(1)]
                public AutoMapMarkerSetting DeepOreAnthracite = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-ore-anthracite-*"),
                    markerColor: "black",
                    markerIcon: "ladder",
                    markerCoverageRadius: 20);

                [ProtoMember(15)]
                public AutoMapMarkerSetting DeepOreBismuth = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-ore-*-bismuthinite-*"),
                    markerColor: "seagreen",
                    markerIcon: "ladder",
                    markerCoverageRadius: 12);

                [ProtoMember(2)]
                public AutoMapMarkerSetting DeepOreBlackCoal = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-ore-bituminouscoal-*"),
                    markerColor: "black",
                    markerIcon: "ladder",
                    markerCoverageRadius: 24);

                [ProtoMember(3)]
                public AutoMapMarkerSetting DeepOreBorax = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-ore-borax-*"),
                    markerColor: "ghostwhite",
                    markerIcon: "ladder",
                    markerCoverageRadius: 18);

                [ProtoMember(4)]
                public AutoMapMarkerSetting DeepOreBrownCoal = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-ore-lignite-*"),
                    markerColor: "black",
                    markerIcon: "ladder",
                    markerCoverageRadius: 24);

                [ProtoMember(5)]
                public AutoMapMarkerSetting DeepOreCinnabar = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-ore-cinnabar-*"),
                    markerColor: "crimson",
                    markerIcon: "ladder",
                    markerCoverageRadius: 18);

                [ProtoMember(6)]
                public AutoMapMarkerSetting DeepOreCopper = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:copper-ore"),
                    markerColor: "darkorange",
                    markerIcon: "ladder",
                    markerCoverageRadius: 16);

                [ProtoMember(7)]
                public AutoMapMarkerSetting DeepOreGold = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:gold-ore"),
                    markerColor: "gold",
                    markerIcon: "ladder",
                    markerCoverageRadius: 16);

                [ProtoMember(16)]
                public AutoMapMarkerSetting DeepOreIron = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-ore-*-limonite-*"),
                    markerColor: "maroon",
                    markerIcon: "ladder",
                    markerCoverageRadius: 32);

                [ProtoMember(8)]
                public AutoMapMarkerSetting DeepOreLapisLazuli = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-ore-lapislazuli-*"),
                    markerColor: "royalblue",
                    markerIcon: "ladder",
                    markerCoverageRadius: 8);

                [ProtoMember(9)]
                public AutoMapMarkerSetting DeepOreLead = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-ore-*-galena-*"),
                    markerColor: "slategray",
                    markerIcon: "ladder",
                    markerCoverageRadius: 12);

                [ProtoMember(10)]
                public AutoMapMarkerSetting DeepOreOlivine = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("ore-olivine"),
                    markerColor: "olivedrab",
                    markerIcon: "ladder",
                    markerCoverageRadius: 32);

                [ProtoMember(11)]
                public AutoMapMarkerSetting DeepOreQuartz = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("ore-quartz"),
                    markerColor: "white",
                    markerIcon: "ladder",
                    markerCoverageRadius: 32);

                [ProtoMember(12)]
                public AutoMapMarkerSetting DeepOreSilver = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:silver-ore"),
                    markerColor: "silver",
                    markerIcon: "ladder",
                    markerCoverageRadius: 16);

                [ProtoMember(13)]
                public AutoMapMarkerSetting DeepOreSulfur = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("ore-sulfur"),
                    markerColor: "khaki",
                    markerIcon: "ladder",
                    markerCoverageRadius: 18);

                [ProtoMember(14)]
                public AutoMapMarkerSetting DeepOreTin = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-ore-*-cassiterite-*"),
                    markerColor: "#3C1E05",
                    markerIcon: "ladder",
                    markerCoverageRadius: 12);

                [ProtoMember(17)]
                public AutoMapMarkerSetting DeepOreTitanium = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-ore-*-ilmenite-*"),
                    markerColor: "darkslategray",
                    markerIcon: "ladder",
                    markerCoverageRadius: 22);

                [ProtoMember(18)]
                public AutoMapMarkerSetting DeepOreZinc = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-ore-*-sphalerite-*"),
                    markerColor: "gray",
                    markerIcon: "ladder",
                    markerCoverageRadius: 10);

                [ProtoMember(19)]
                public AutoMapMarkerSetting DeepOreNickel = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-ore-*-pentlandite-*"),
                    markerColor: "darkgoldenrod",
                    markerIcon: "ladder",
                    markerCoverageRadius: 12);
            }

            [ProtoContract]
            public class MapMarkerSettings_Misc
            {
                [ProtoMember(9)]
                public AutoMapMarkerSetting BlockRedClay = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("item-clay-red"),
                    markerColor: "indianred",
                    markerIcon: "vessel",
                    markerCoverageRadius: 30);

                [ProtoMember(1)]
                public AutoMapMarkerSetting BlockBlueClay = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("item-clay-blue"),
                    markerColor: "cornflowerblue",
                    markerIcon: "vessel",
                    markerCoverageRadius: 30);

                [ProtoMember(2)]
                public AutoMapMarkerSetting BlockFireClay = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("item-clay-fire"),
                    markerColor: "firebrick",
                    markerIcon: "vessel",
                    markerCoverageRadius: 30);

                [ProtoMember(3)]
                public AutoMapMarkerSetting BlockPeat = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-peat-none"),
                    markerColor: "chocolate",
                    markerIcon: "rocks",
                    markerCoverageRadius: 30);

                [ProtoMember(4)]
                public AutoMapMarkerSetting BlockHighFertilitySoil = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-soil-compost-none"),
                    markerColor: "indigo",
                    markerIcon: "star2",
                    markerCoverageRadius: 30);

                [ProtoMember(5)]
                public AutoMapMarkerSetting BlockMeteoriticIron = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-meteorite-iron"),
                    markerColor: "darkorange",
                    markerIcon: "star2",
                    markerCoverageRadius: 8);

                [ProtoMember(6)]
                public AutoMapMarkerSetting BlockCoatingSaltpeter = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("block-saltpeter-d"),
                    markerColor: "beige",
                    markerIcon: "ladder",
                    markerCoverageRadius: 24);

                [ProtoMember(7)]
                public AutoMapMarkerSetting Beehive = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:beehive"),
                    markerColor: "gold",
                    markerIcon: "bee",
                    markerCoverageRadius: 1);

                [ProtoMember(8)]
                public AutoMapMarkerSetting Translocator = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("wpSuggestion-spiral"),
                    markerColor: "darkturquoise",
                    markerIcon: "spiral",
                    markerCoverageRadius: 2);
            }

            [ProtoContract]
            public class MapMarkerSettings_Traders
            {
                [ProtoMember(1)]
                public AutoMapMarkerSetting TraderArtisan = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-artisan"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(2)]
                public AutoMapMarkerSetting TraderBuildingMaterials = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-buildmaterials"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(3)]
                public AutoMapMarkerSetting TraderClothing = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-clothing"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(4)]
                public AutoMapMarkerSetting TraderCommodities = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-commodities"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(5)]
                public AutoMapMarkerSetting TraderFoods = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-foods"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(6)]
                public AutoMapMarkerSetting TraderFurniture = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-furniture"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(7)]
                public AutoMapMarkerSetting TraderLuxuries = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-luxuries"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(8)]
                public AutoMapMarkerSetting TraderSurvivalGoods = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-survivalgoods"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(9)]
                public AutoMapMarkerSetting TraderTreasureHunter = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-treasurehunter"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);
            }

            [ProtoContract]
            public class MapMarkerSettings_Custom
            {
                [ProtoMember(1)]
                public AutoMapMarkerSetting CustomMarker1 = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:custom-marker-1"),
                    markerColor: "black",
                    markerIcon: "circle",
                    markerCoverageRadius: 1);

                [ProtoMember(2)]
                public AutoMapMarkerSetting CustomMarker2 = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:custom-marker-2"),
                    markerColor: "black",
                    markerIcon: "circle",
                    markerCoverageRadius: 1);

                [ProtoMember(3)]
                public AutoMapMarkerSetting CustomMarker3 = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:custom-marker-3"),
                    markerColor: "black",
                    markerIcon: "circle",
                    markerCoverageRadius: 1);

                public AutoMapMarkerSetting SettingByIndex(int index)
                {
                    return index == 3 ? CustomMarker3 : (index == 2 ? CustomMarker2 : CustomMarker1);
                }
            }

            public OrderedDictionary<string, OrderedDictionary<string, AutoMapMarkerSetting>> GetMapMarkerSettingCollection()
            {
                if (_MapMarkerSettingsCollection == null)
                {
                    _MapMarkerSettingsCollection = new OrderedDictionary<string, OrderedDictionary<string, AutoMapMarkerSetting>>
                        {
                            { Lang.Get("egocarib-mapmarkers:organic-matter"),
                                new OrderedDictionary<string, AutoMapMarkerSetting>
                                {
                                    { Lang.Get("item-resin"), AutoMapMarkers.OrganicMatter.Resin },
                                    { Lang.Get("item-fruit-blueberry"), AutoMapMarkers.OrganicMatter.Blueberry },
                                    { Lang.Get("item-fruit-cranberry"), AutoMapMarkers.OrganicMatter.Cranberry },
                                    { Lang.Get("item-fruit-blackcurrant"), AutoMapMarkers.OrganicMatter.BlackCurrant },
                                    { Lang.Get("item-fruit-redcurrant"), AutoMapMarkers.OrganicMatter.RedCurrant },
                                    { Lang.Get("item-fruit-whitecurrant"), AutoMapMarkers.OrganicMatter.WhiteCurrant },
                                    { Lang.Get("egocarib-mapmarkers:safe-mushrooms"), AutoMapMarkers.OrganicMatter.SafeMushroom },
                                    { Lang.Get("egocarib-mapmarkers:unsafe-mushrooms"), AutoMapMarkers.OrganicMatter.UnsafeMushroom },
                                    { Lang.Get("egocarib-mapmarkers:flowers"), AutoMapMarkers.OrganicMatter.Flower },
                                    { Lang.Get("egocarib-mapmarkers:fruit-trees"), AutoMapMarkers.OrganicMatter.FruitTree },
                                    { Lang.Get("egocarib-mapmarkers:wild-crops"), AutoMapMarkers.OrganicMatter.WildCrop },
                                    { Lang.Get("egocarib-mapmarkers:reeds"), AutoMapMarkers.OrganicMatter.Reed },
                                    { Lang.GetMatching("block-tallplant-tule-*"), AutoMapMarkers.OrganicMatter.Tule }
                                }
                            },
                            { Lang.Get("egocarib-mapmarkers:surface-ore"),
                                new OrderedDictionary<string, AutoMapMarkerSetting>
                                {
                                    { Lang.GetMatching("block-looseores-anthracite-*"), AutoMapMarkers.SurfaceOre.LooseOreAnthracite },
                                    { Lang.GetMatching("block-looseores-bituminouscoal-*"), AutoMapMarkers.SurfaceOre.LooseOreBlackCoal },
                                    { Lang.GetMatching("block-looseores-borax-*"), AutoMapMarkers.SurfaceOre.LooseOreBorax },
                                    { Lang.GetMatching("block-looseores-lignite-*"), AutoMapMarkers.SurfaceOre.LooseOreBrownCoal },
                                    { Lang.GetMatching("block-looseores-cinnabar-*"), AutoMapMarkers.SurfaceOre.LooseOreCinnabar },
                                    { Lang.Get("egocarib-mapmarkers:copper-ore-bits"), AutoMapMarkers.SurfaceOre.LooseOreCopper },
                                    { Lang.Get("egocarib-mapmarkers:gold-ore-bits"), AutoMapMarkers.SurfaceOre.LooseOreGold },
                                    { Lang.GetMatching("block-looseores-lapislazuli-*"), AutoMapMarkers.SurfaceOre.LooseOreLapisLazuli },
                                    { Lang.GetMatching("block-looseores-galena-*"), AutoMapMarkers.SurfaceOre.LooseOreLead },
                                    { Lang.GetMatching("block-looseores-olivine-peridotite-*"), AutoMapMarkers.SurfaceOre.LooseOreOlivine },
                                    { Lang.GetMatching("block-looseores-quartz-*"), AutoMapMarkers.SurfaceOre.LooseOreQuartz },
                                    { Lang.Get("egocarib-mapmarkers:silver-ore-bits"), AutoMapMarkers.SurfaceOre.LooseOreSilver },
                                    { Lang.GetMatching("block-looseores-sulfur-*"), AutoMapMarkers.SurfaceOre.LooseOreSulfur },
                                    { Lang.GetMatching("block-looseores-cassiterite-*"), AutoMapMarkers.SurfaceOre.LooseOreTin }
                                }
                            },
                            { Lang.Get("egocarib-mapmarkers:deep-ore"),
                                new OrderedDictionary<string, AutoMapMarkerSetting>
                                {
                                    { Lang.GetMatching("block-ore-anthracite-*"), AutoMapMarkers.DeepOre.DeepOreAnthracite },
                                    { Lang.GetMatching("block-ore-*-bismuthinite-*"), AutoMapMarkers.DeepOre.DeepOreBismuth },
                                    { Lang.GetMatching("block-ore-bituminouscoal-*"), AutoMapMarkers.DeepOre.DeepOreBlackCoal },
                                    { Lang.GetMatching("block-ore-borax-*"), AutoMapMarkers.DeepOre.DeepOreBorax },
                                    { Lang.GetMatching("block-ore-lignite-*"), AutoMapMarkers.DeepOre.DeepOreBrownCoal },
                                    { Lang.GetMatching("block-ore-cinnabar-*"), AutoMapMarkers.DeepOre.DeepOreCinnabar },
                                    { Lang.Get("egocarib-mapmarkers:copper-ore"), AutoMapMarkers.DeepOre.DeepOreCopper },
                                    { Lang.Get("egocarib-mapmarkers:gold-ore"), AutoMapMarkers.DeepOre.DeepOreGold },
                                    { Lang.GetMatching("block-ore-*-limonite-*"), AutoMapMarkers.DeepOre.DeepOreIron },
                                    { Lang.GetMatching("block-ore-lapislazuli-*"), AutoMapMarkers.DeepOre.DeepOreLapisLazuli },
                                    { Lang.GetMatching("block-ore-*-galena-*"), AutoMapMarkers.DeepOre.DeepOreLead },
                                    { Lang.GetMatching("block-ore-*-pentlandite-*"), AutoMapMarkers.DeepOre.DeepOreNickel },
                                    { Lang.Get("ore-olivine"), AutoMapMarkers.DeepOre.DeepOreOlivine },
                                    { Lang.Get("ore-quartz"), AutoMapMarkers.DeepOre.DeepOreQuartz },
                                    { Lang.Get("egocarib-mapmarkers:silver-ore"), AutoMapMarkers.DeepOre.DeepOreSilver },
                                    { Lang.Get("ore-sulfur"), AutoMapMarkers.DeepOre.DeepOreSulfur },
                                    { Lang.GetMatching("block-ore-*-cassiterite-*"), AutoMapMarkers.DeepOre.DeepOreTin },
                                    { Lang.GetMatching("block-ore-*-ilmenite-*"), AutoMapMarkers.DeepOre.DeepOreTitanium },
                                    { Lang.GetMatching("block-ore-*-sphalerite-*"), AutoMapMarkers.DeepOre.DeepOreZinc }
                                }
                            },
                            { Lang.Get("egocarib-mapmarkers:misc"),
                                new OrderedDictionary<string, AutoMapMarkerSetting>
                                {
                                    { Lang.Get("egocarib-mapmarkers:beehive"), AutoMapMarkers.MiscBlocks.Beehive },
                                    { Lang.Get("wpSuggestion-spiral"), AutoMapMarkers.MiscBlocks.Translocator },
                                    { Lang.GetMatching("item-clay-red"), AutoMapMarkers.MiscBlocks.BlockRedClay },
                                    { Lang.GetMatching("item-clay-blue"), AutoMapMarkers.MiscBlocks.BlockBlueClay },
                                    { Lang.GetMatching("item-clay-fire"), AutoMapMarkers.MiscBlocks.BlockFireClay },
                                    { Lang.GetMatching("block-peat-none"), AutoMapMarkers.MiscBlocks.BlockPeat },
                                    { Lang.GetMatching("block-soil-compost-none"), AutoMapMarkers.MiscBlocks.BlockHighFertilitySoil },
                                    { Lang.GetMatching("block-meteorite-iron"), AutoMapMarkers.MiscBlocks.BlockMeteoriticIron },
                                    { Lang.Get("block-saltpeter-d"), AutoMapMarkers.MiscBlocks.BlockCoatingSaltpeter }
                                }
                            },
                            { Lang.Get("egocarib-mapmarkers:traders"),
                                new OrderedDictionary<string, AutoMapMarkerSetting>
                                {
                                    { Lang.Get("item-creature-humanoid-trader-artisan"), AutoMapMarkers.Traders.TraderArtisan },
                                    { Lang.Get("item-creature-humanoid-trader-buildmaterials"), AutoMapMarkers.Traders.TraderBuildingMaterials },
                                    { Lang.Get("item-creature-humanoid-trader-clothing"), AutoMapMarkers.Traders.TraderClothing },
                                    { Lang.Get("item-creature-humanoid-trader-commodities"), AutoMapMarkers.Traders.TraderCommodities },
                                    { Lang.Get("item-creature-humanoid-trader-foods"), AutoMapMarkers.Traders.TraderFoods },
                                    { Lang.Get("item-creature-humanoid-trader-furniture"), AutoMapMarkers.Traders.TraderFurniture },
                                    { Lang.Get("item-creature-humanoid-trader-luxuries"), AutoMapMarkers.Traders.TraderLuxuries },
                                    { Lang.Get("item-creature-humanoid-trader-survivalgoods"), AutoMapMarkers.Traders.TraderSurvivalGoods },
                                    { Lang.Get("item-creature-humanoid-trader-treasurehunter"), AutoMapMarkers.Traders.TraderTreasureHunter }
                                }
                            },
                            { Lang.Get("egocarib-mapmarkers:custom"),
                                new OrderedDictionary<string, AutoMapMarkerSetting>
                                {
                                    { Lang.Get("egocarib-mapmarkers:custom-marker-1"), AutoMapMarkers.Custom.CustomMarker1 },
                                    { Lang.Get("egocarib-mapmarkers:custom-marker-2"), AutoMapMarkers.Custom.CustomMarker2 },
                                    { Lang.Get("egocarib-mapmarkers:custom-marker-3"), AutoMapMarkers.Custom.CustomMarker3 }
                                }
                            }
                        };
                }
                return _MapMarkerSettingsCollection;
            }

            [ProtoContract]
            public class AutoMapMarkerSetting
            {
                // IsRequired is needed to prevent ProtoBuf trying to optimize the fields to "default" values
                // and ultimately ignoring what was actually sent across the network.
                [ProtoMember(1, IsRequired = true)]
                public bool Enabled;
                [ProtoMember(2, IsRequired = true)]
                public string MarkerTitle;
                [ProtoMember(3, IsRequired = true)]
                public string MarkerColor;
                [ProtoMember(4, IsRequired = true)]
                public string MarkerIcon;
                [ProtoMember(6, IsRequired = true)]
                public bool MarkerPinned;
                [ProtoMember(5, IsRequired = true)]
                public int MarkerCoverageRadius;

                public AutoMapMarkerSetting() { /* paramaterless constructor required by proto-buf */ }

                [JsonConstructor]
                public AutoMapMarkerSetting(bool enabled, bool pinned, string markerTitle, string markerColor, string markerIcon, int markerCoverageRadius)
                {
                    Enabled = enabled;
                    MarkerPinned = pinned;
                    MarkerTitle = markerTitle;
                    MarkerColor = markerColor;
                    MarkerIcon = markerIcon;
                    MarkerCoverageRadius = markerCoverageRadius;
                }

                [JsonIgnore]
                public int? MarkerColorInteger
                {
                    get
                    {
                        int? ret = MarkerColorIntegerNoAlpha;
                        if (ret.HasValue)
                            ret = ret.Value | (255 << 24);
                        return ret;
                    }
                }

                [JsonIgnore]
                public int? MarkerColorIntegerNoAlpha
                {
                    get
                    {
                        System.Drawing.Color parsedColor;
                        if (MarkerColor.StartsWith("#", StringComparison.Ordinal))
                        {
                            try
                            {
                                int argb = Int32.Parse(MarkerColor.Replace("#", ""), NumberStyles.HexNumber);
                                parsedColor = System.Drawing.Color.FromArgb(argb);
                            }
                            catch (FormatException)
                            {
                                return null;
                            }
                        }
                        else
                        {
                            parsedColor = System.Drawing.Color.FromName(MarkerColor);
                        }
                        return parsedColor.ToArgb();
                    }
                }

                /// <summary>
                /// Makes this AutoMapMarkerSetting's icon and color equal to another's.
                /// </summary>
                /// <param name="sourceSetting">AutoMapMarkerSetting from which to copy values.</param>
                public void CopyIconAndColorFrom(AutoMapMarkerSetting sourceSetting)
                {
                    MarkerColor = sourceSetting.MarkerColor;
                    MarkerIcon = sourceSetting.MarkerIcon;
                }
            }
        }

        public static Settings GetSettings(ICoreAPI api, bool allowServerThread = false, bool returnDefaults = true)
        {
            if (!allowServerThread && api.Side == EnumAppSide.Server)
            {
                MessageUtil.LogError("Unexpected attempt to retrieve settings from server side thread (should typically only occur on Client thread).");
                throw new InvalidOperationException();
            }
            Settings settings = api.Side == EnumAppSide.Client ? _cachedClientSettings : null;
            if (settings == null)
            {
                try
                {
                    settings = api.LoadModConfig<Settings>(ConfigFilename);
                    if (api.Side == EnumAppSide.Client)
                        _cachedClientSettings = settings;
                }
                catch
                {
                    MessageUtil.LogError("Unable to load your mod configuration file "
                        + "(" + ConfigFilename + "). There may have been a syntax error in the file."
                        + "A new default settings file will be generated.");
                }
            }
            if (settings == null && returnDefaults)
            {
                settings = new Settings();
                SaveSettings(api, settings, allowServerThread); //Create a default settings file if one didn't already exist.
            }
            return settings;
        }

        public static void SaveSettings(ICoreAPI api, Settings settings, bool allowServerThread = false)
        {
            if (!allowServerThread && api.Side == EnumAppSide.Server)
            {
                MessageUtil.LogError("Unexpected attempt to save settings on server side thread (should typically only occur on Client thread).");
                throw new InvalidOperationException();
            }
            if (api.Side == EnumAppSide.Client)
                _cachedClientSettings = settings;
            api.StoreModConfig<Settings>(settings, ConfigFilename);
        }

        /// <summary>
        /// Checks if a settings file already exists
        /// </summary>
        public static bool CheckIfSettingsExist(ICoreAPI api)
        {
            Settings settings = GetSettings(api, true, false);
            if (settings != null)
            {
                return true;
            }
            return false;
        }
    }
}
