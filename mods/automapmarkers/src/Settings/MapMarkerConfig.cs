using Egocarib.AutoMapMarkers.Utilities;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using MarkerRegistry = Egocarib.AutoMapMarkers.Utilities.MarkerDetectionRegistry;

namespace Egocarib.AutoMapMarkers.Settings
{
    /// <summary>
    /// Settings and configuration methods for the Auto Map Markers mod.
    /// </summary>
    public static class MapMarkerConfig
    {
        /// <summary>The filename used when saving mod settings to the ModConfig folder (legacy format).</summary>
        public const string ConfigFilename = "auto_map_markers_config.json";
        /// <summary>Mod settings are cached on client side to optimize performance.</summary>
        private static Settings _cachedClientSettings = null;
        /// <summary>Cached marker definitions loaded from JSON files.</summary>
        private static List<MarkerCategoryDef> _cachedDefinitions = null;
        /// <summary>Cached detection registry built from definitions + settings.</summary>
        private static MarkerRegistry _cachedRegistry = null;

        /// <summary>Returns the cached detection registry, or null if not yet built.</summary>
        public static MarkerRegistry GetRegistry() => _cachedRegistry;

        /// <summary>Rebuilds the detection registry from current definitions and settings.</summary>
        public static void RebuildRegistry()
        {
            if (_cachedDefinitions != null && _cachedClientSettings != null)
                _cachedRegistry = MarkerRegistry.Build(_cachedDefinitions, _cachedClientSettings);
        }

        /// <summary>Gets the path to the ModConfig folder.</summary>
        public static string GetModConfigPath(ICoreAPI api)
        {
            return api.GetOrCreateDataPath("ModConfig");
        }

        [ProtoContract]
        public class Settings
        {
            private MarkerSettingLayout _markerSettingLayout = null;

            /// <summary>
            /// Stores settings for addon-defined markers that don't have hardcoded grouper fields.
            /// Not serialized via protobuf — addon settings are rebuilt from definitions + overrides on each load.
            /// </summary>
            internal Dictionary<string, AutoMapMarkerSetting> AddonMarkerSettings = new Dictionary<string, AutoMapMarkerSetting>();

            /// <summary>
            /// Tracks expand/collapse state for expandable entries. Key is the parent entry label.
            /// </summary>
            internal Dictionary<string, bool> ExpandStates = new Dictionary<string, bool>();

            public bool IsExpanded(MarkerEntryDef entry)
            {
                if (entry.ExpandableEntries == null || entry.ExpandableEntries.Count == 0)
                    return false;
                if (ExpandStates.TryGetValue(entry.Label, out bool state))
                    return state;
                return entry.DefaultExpanded;
            }

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
            [ProtoMember(11, IsRequired = true)]
            public bool SuppressMarkerOnFarmland = true;
            [ProtoMember(12, IsRequired = true)]
            public bool EnableDetectHotkey = false;
            [ProtoMember(13, IsRequired = true)]
            public bool ChatNotifyOnBoatMarker = false;
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

                [ProtoMember(17)]
                public AutoMapMarkerSetting Beautyberry = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("item-fruit-beautyberry"),
                    markerColor: "fuchsia",
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

                [ProtoMember(18)]
                public AutoMapMarkerSetting Strawberry = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("item-fruit-strawberry"),
                    markerColor: "orangered",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

                [ProtoMember(20)]
                public AutoMapMarkerSetting Raspberry = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("item-fruit-raspberry"),
                    markerColor: "crimson",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

                [ProtoMember(21)]
                public AutoMapMarkerSetting Blackberry = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("item-fruit-blackberry"),
                    markerColor: "#1a0a1a",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

                [ProtoMember(22)]
                public AutoMapMarkerSetting Cloudberry = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("item-fruit-cloudberry"),
                    markerColor: "goldenrod",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

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
                    markerTitle: Lang.Get("egocarib-mapmarkers:tule"),
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

                [ProtoMember(19)]
                public AutoMapMarkerSetting Berries = new AutoMapMarkerSetting(
                    enabled: false,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:berries"),
                    markerColor: "midnightblue",
                    markerIcon: "circle",
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

                [ProtoMember(7)]
                public AutoMapMarkerSetting LooseOreGold = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.GetMatching("block-looseores-quartz_nativegold-*"),
                    markerColor: "gold",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

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

                [ProtoMember(10)]
                public AutoMapMarkerSetting Raft = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: true,
                    markerTitle: Lang.Get("egocarib-mapmarkers:raft"),
                    markerColor: "gold",
                    markerIcon: "star1",
                    markerCoverageRadius: 48);

                [ProtoMember(11)]
                public AutoMapMarkerSetting Sailboat = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: true,
                    markerTitle: Lang.Get("egocarib-mapmarkers:sailboat"),
                    markerColor: "gold",
                    markerIcon: "star1",
                    markerCoverageRadius: 48);
            }

            [ProtoContract]
            public class MapMarkerSettings_Traders
            {
                [ProtoMember(10)]
                public AutoMapMarkerSetting TraderAgriculture = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:trader-agriculture"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(1)]
                public AutoMapMarkerSetting TraderArtisan = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:trader-artisan"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(2)]
                public AutoMapMarkerSetting TraderBuildingMaterials = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:trader-buildmaterials"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(3)]
                public AutoMapMarkerSetting TraderClothing = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:trader-clothing"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(4)]
                public AutoMapMarkerSetting TraderCommodities = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:trader-commodities"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(6)]
                public AutoMapMarkerSetting TraderFurniture = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:trader-furniture"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(7)]
                public AutoMapMarkerSetting TraderLuxuries = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:trader-luxuries"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(8)]
                public AutoMapMarkerSetting TraderSurvivalGoods = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:trader-survivalgoods"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(9)]
                public AutoMapMarkerSetting TraderTreasureHunter = new AutoMapMarkerSetting(
                    enabled: true,
                    pinned: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:trader-treasurehunter"),
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

            public MarkerSettingLayout GetMapMarkerSettingLayout()
            {
                if (_markerSettingLayout == null && _cachedDefinitions != null)
                {
                    _markerSettingLayout = MarkerSettingsPersistence.BuildSettingLayout(this, _cachedDefinitions);
                }
                return _markerSettingLayout;
            }

            internal void InvalidateLayout()
            {
                _markerSettingLayout = null;
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

        /// <summary>
        /// Initializes the definition loader. Should be called once on mod startup.
        /// Extracts _core.json from embedded resources if needed.
        /// </summary>
        public static void InitializeDefinitions(ICoreAPI api)
        {
            string modConfigPath = GetModConfigPath(api);
            MarkerDefinitionLoader.EnsureCoreDefinitionsExist(modConfigPath);
            _cachedDefinitions = MarkerDefinitionLoader.LoadDefinitions(modConfigPath, api);
            if (api.Side == EnumAppSide.Client)
                _cachedClientSettings = null;
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
                string modConfigPath = GetModConfigPath(api);

                // Ensure definitions are loaded
                if (_cachedDefinitions == null)
                    InitializeDefinitions(api);

                // Check for migration: if settings.json doesn't exist but old config does, migrate
                if (!MarkerSettingsPersistence.SettingsFileExists(modConfigPath))
                {
                    MigrateLegacyConfig(api, modConfigPath);
                }

                // Load from new pipeline
                try
                {
                    settings = MarkerSettingsPersistence.LoadSettings(modConfigPath, _cachedDefinitions);
                    if (api.Side == EnumAppSide.Client)
                    {
                        _cachedClientSettings = settings;
                        _cachedRegistry = MarkerRegistry.Build(_cachedDefinitions, settings);
                    }
                }
                catch (Exception ex)
                {
                    MessageUtil.LogError("Failed to load settings via new pipeline: " + ex.Message);
                }
            }
            if (settings == null && returnDefaults)
            {
                settings = new Settings();
                SaveSettings(api, settings, allowServerThread);
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
            {
                _cachedClientSettings = settings;
                if (_cachedDefinitions != null)
                    _cachedRegistry = MarkerRegistry.Build(_cachedDefinitions, settings);
            }

            string modConfigPath = GetModConfigPath(api);
            if (_cachedDefinitions == null)
                InitializeDefinitions(api);

            MarkerSettingsPersistence.SaveSettings(modConfigPath, settings, _cachedDefinitions);
        }

        /// <summary>
        /// Checks if a settings file already exists (either new or legacy format)
        /// </summary>
        public static bool CheckIfSettingsExist(ICoreAPI api)
        {
            string modConfigPath = GetModConfigPath(api);
            if (MarkerSettingsPersistence.SettingsFileExists(modConfigPath))
                return true;

            Settings settings = null;
            try
            {
                settings = api.LoadModConfig<Settings>(ConfigFilename);
            }
            catch (Exception ex)
            {
                MessageUtil.LogError($"Failed to check legacy config: {ex.Message}");
            }
            return settings != null;
        }

        /// <summary>
        /// Clears the cached client settings so the next GetSettings() call reloads from disk.
        /// </summary>
        public static void ClearCachedClientSettings()
        {
            _cachedClientSettings = null;
            _cachedRegistry = null;
        }

        /// <summary>
        /// Ensures a default settings.json exists on the server side.
        /// Creates one with default boolean toggles if none exists, without loading definitions.
        /// </summary>
        public static void EnsureServerSettingsExist(ICoreAPI api)
        {
            string modConfigPath = GetModConfigPath(api);
            if (!MarkerSettingsPersistence.SettingsFileExists(modConfigPath))
            {
                if (!MigrateLegacyConfig(api, modConfigPath))
                {
                    // No existing settings at all — create a minimal default settings.json
                    MarkerSettingsPersistence.CreateDefaultSettingsFile(modConfigPath);
                    MessageUtil.Log("Server: created default settings.json.");
                }
            }
        }

        /// <summary>
        /// Attempts to migrate a legacy config file to the new settings.json format.
        /// Returns true if a legacy config was found and migrated, false otherwise.
        /// </summary>
        private static bool MigrateLegacyConfig(ICoreAPI api, string modConfigPath)
        {
            Settings legacySettings = null;
            try
            {
                legacySettings = api.LoadModConfig<Settings>(ConfigFilename);
            }
            catch
            {
                MessageUtil.LogError("Unable to load legacy config file (" + ConfigFilename + "). A new default settings file will be generated.");
            }

            if (legacySettings == null)
                return false;

            MessageUtil.Log("Migrating settings from legacy format to new settings.json...");
            if (_cachedDefinitions == null)
                InitializeDefinitions(api);

            // Smart berry migration: if any individual berry was enabled, enable the
            // Berries parent and start it expanded so the user sees their prior settings.
            var organic = legacySettings.AutoMapMarkers.OrganicMatter;
            var allBerrySettings = new[]
            {
                organic.Blueberry, organic.Beautyberry, organic.Cranberry,
                organic.BlackCurrant, organic.RedCurrant, organic.WhiteCurrant, organic.Strawberry
            };
            bool anyBerryEnabled = allBerrySettings.Any(b => b.Enabled);
            if (anyBerryEnabled)
            {
                organic.Berries.Enabled = true;
                legacySettings.ExpandStates["egocarib-mapmarkers:berries"] = true;
            }

            // Smart trader migration: check if all traders have the same icon+color.
            // If uniform, start collapsed (user had default/uniform settings).
            // If customized per-trader, start expanded (preserve individual settings).
            var traders = legacySettings.AutoMapMarkers.Traders;
            var allTraderSettings = new[]
            {
                traders.TraderAgriculture, traders.TraderArtisan, traders.TraderBuildingMaterials,
                traders.TraderClothing, traders.TraderCommodities, traders.TraderFurniture,
                traders.TraderLuxuries, traders.TraderSurvivalGoods, traders.TraderTreasureHunter
            };
            bool allSameIconColor = allTraderSettings.All(t =>
                string.Equals(t.MarkerIcon, allTraderSettings[0].MarkerIcon, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(t.MarkerColor, allTraderSettings[0].MarkerColor, StringComparison.OrdinalIgnoreCase));
            if (allSameIconColor)
            {
                legacySettings.ExpandStates["egocarib-mapmarkers:traders"] = false;
            }

            MarkerSettingsPersistence.SaveSettings(modConfigPath, legacySettings, _cachedDefinitions);

            try
            {
                string oldFilePath = Path.Combine(modConfigPath, ConfigFilename);
                string bakFilePath = Path.Combine(modConfigPath, ConfigFilename + ".bak");
                if (File.Exists(oldFilePath))
                {
                    if (File.Exists(bakFilePath))
                        File.Delete(bakFilePath);
                    File.Move(oldFilePath, bakFilePath);
                    MessageUtil.Log("Legacy config file renamed to " + ConfigFilename + ".bak");
                }
            }
            catch (Exception ex)
            {
                MessageUtil.LogError("Failed to rename legacy config file: " + ex.Message);
            }

            return true;
        }
    }
}
