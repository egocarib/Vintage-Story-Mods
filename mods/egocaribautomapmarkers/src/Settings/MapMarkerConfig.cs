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
        public const string ConfigFilename = "auto_map_markers_config.json";

        [ProtoContract]
        public class Settings
        {
            private OrderedDictionary<string, OrderedDictionary<string, AutoMapMarkerSetting>> _MapMarkerSettingsCollection = null;
            public const string Icons = "circle,bee,cave,home,ladder,pick,rocks,ruins,spiral,star1,star2,trader,vessel";
            public const string IconsVTML = "<icon name=\"wpCircle\">,<icon name=\"wpBee\">,<icon name=\"wpCave\">,<icon name=\"wpHome\">,<icon name=\"wpLadder\">,<icon name=\"wpPick\">,<icon name=\"wpRocks\">,<icon name=\"wpRuins\">,<icon name=\"wpSpiral\">,<icon name=\"wpStar1\">,<icon name=\"wpStar2\">,<icon name=\"wpTrader\">,<icon name=\"wpVessel\">";

            [ProtoMember(1, IsRequired = true)]
            public bool ChatNotifyOnWaypointCreation = false;
            [ProtoMember(5, IsRequired = true)]
            public bool EnableWaypointDeletionHotkey = false;
            [ProtoMember(6, IsRequired = true)]
            public bool ChatNotifyOnWaypointDeletion = true;
            [ProtoMember(7, IsRequired = true)]
            public bool EnableCustomHotkeys = false;
            [ProtoMember(2, IsRequired = true)]
            public bool DisableAllModFeatures = true;
            [ProtoMember(3)]
            public double ConfigVersion = 2.01;
            [ProtoMember(4)]
            public MapMarkerSettingsGrouper AutoMapMarkers = new MapMarkerSettingsGrouper();

            [ProtoContract]
            public class MapMarkerSettingsGrouper
            {
                [ProtoMember(1)]
                public MapMarkerSettings_OrganicMatter OrganicMatter = new MapMarkerSettings_OrganicMatter();
                [ProtoMember(2)]
                public MapMarkerSettings_Ore SurfaceOre = new MapMarkerSettings_Ore();
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
                    markerTitle: Lang.Get("item-resin"),
                    markerColor: "darkorange",
                    markerIcon: "circle",
                    markerCoverageRadius: 1);

                [ProtoMember(2)]
                public AutoMapMarkerSetting Blueberry = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.Get("item-fruit-blueberry"),
                    markerColor: "midnightblue",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

                [ProtoMember(3)]
                public AutoMapMarkerSetting Cranberry = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.Get("item-fruit-cranberry"),
                    markerColor: "maroon",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

                [ProtoMember(4)]
                public AutoMapMarkerSetting BlackCurrant = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.Get("item-fruit-blackcurrant"),
                    markerColor: "#291B1A",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

                [ProtoMember(5)]
                public AutoMapMarkerSetting RedCurrant = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.Get("item-fruit-redcurrant"),
                    markerColor: "darkred",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

                [ProtoMember(6)]
                public AutoMapMarkerSetting WhiteCurrant = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.Get("item-fruit-whitecurrant"),
                    markerColor: "ivory",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

                [ProtoMember(7)]
                public AutoMapMarkerSetting MushroomBolete = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.GetMatching("block-mushroom-bolete-normal-*"),
                    markerColor: "#503922",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

                [ProtoMember(8)]
                public AutoMapMarkerSetting MushroomFieldMushroom = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.GetMatching("block-mushroom-fieldmushroom-normal-*"),
                    markerColor: "ghostwhite",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);

                [ProtoMember(9)]
                public AutoMapMarkerSetting MushroomFlyAgaric = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.GetMatching("block-mushroom-flyagaric-normal-*"),
                    markerColor: "brown",
                    markerIcon: "circle",
                    markerCoverageRadius: 6);
            }

            [ProtoContract]
            public class MapMarkerSettings_Ore
            {
                [ProtoMember(1)]
                public AutoMapMarkerSetting LooseOreAnthracite = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.GetMatching("block-looseores-anthracite-*"),
                    markerColor: "black",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(2)]
                public AutoMapMarkerSetting LooseOreBlackCoal = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.GetMatching("block-looseores-bituminouscoal-*"),
                    markerColor: "black",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(3)]
                public AutoMapMarkerSetting LooseOreBorax = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.GetMatching("block-looseores-borax-*"),
                    markerColor: "ghostwhite",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(4)]
                public AutoMapMarkerSetting LooseOreBrownCoal = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.GetMatching("block-looseores-lignite-*"),
                    markerColor: "black",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(5)]
                public AutoMapMarkerSetting LooseOreCinnabar = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.GetMatching("block-looseores-cinnabar-*"),
                    markerColor: "crimson",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(6)]
                public AutoMapMarkerSetting LooseOreCopper = new AutoMapMarkerSetting(
                    enabled: true,
                    markerTitle: Lang.GetMatching("block-looseores-nativecopper-*"),
                    markerColor: "darkorange",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                //public AutoMapMarkerSetting LooseOreFluorite = new AutoMapMarkerSetting(
                //        enabled: false,
                //        markerTitle: Lang.GetMatching("block-looseores-fluorite-*"),
                //        markerColor: "black",
                //        markerIcon: "pick",
                //        markerCoverageRadius: 9);

                    [ProtoMember(7)]
                public AutoMapMarkerSetting LooseOreGold = new AutoMapMarkerSetting(
                        enabled: true,
                        markerTitle: Lang.GetMatching("block-looseores-quartz_nativegold-*"),
                        markerColor: "gold",
                        markerIcon: "pick",
                        markerCoverageRadius: 9);

                //public AutoMapMarkerSetting LooseOreGraphite = new AutoMapMarkerSetting(
                //        enabled: false,
                //        markerTitle: Lang.GetMatching("block-looseores-graphite-*"),
                //        markerColor: "black",
                //        markerIcon: "pick",
                //        markerCoverageRadius: 9);

                //public AutoMapMarkerSetting LooseOreKernite = new AutoMapMarkerSetting(
                //        enabled: false,
                //        markerTitle: Lang.GetMatching("block-looseores-kernite-*"),
                //        markerColor: "black",
                //        markerIcon: "pick",
                //        markerCoverageRadius: 9);

                [ProtoMember(8)]
                public AutoMapMarkerSetting LooseOreLapisLazuli = new AutoMapMarkerSetting(
                        enabled: false,
                        markerTitle: Lang.GetMatching("block-looseores-lapislazuli-*"),
                        markerColor: "royalblue",
                        markerIcon: "pick",
                        markerCoverageRadius: 9);

                [ProtoMember(9)]
                public AutoMapMarkerSetting LooseOreLead = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.GetMatching("block-looseores-galena-*"),
                    markerColor: "slategray",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(10)]
                public AutoMapMarkerSetting LooseOreOlivine = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.GetMatching("block-looseores-olivine-peridotite-*"),
                    markerColor: "olivedrab",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                //public AutoMapMarkerSetting LooseOrePhosporite = new AutoMapMarkerSetting(
                //        enabled: false,
                //        markerTitle: Lang.GetMatching("block-looseores-phosphorite-*"),
                //        markerColor: "black",
                //        markerIcon: "pick",
                //        markerCoverageRadius: 9);

                    [ProtoMember(11)]
                public AutoMapMarkerSetting LooseOreQuartz = new AutoMapMarkerSetting(
                        enabled: false,
                        markerTitle: Lang.GetMatching("block-looseores-quartz-*"),
                        markerColor: "white",
                        markerIcon: "pick",
                        markerCoverageRadius: 9);

                [ProtoMember(12)]
                public AutoMapMarkerSetting LooseOreSilver = new AutoMapMarkerSetting(
                    enabled: true,
                    markerTitle: Lang.GetMatching("block-looseores-quartz_nativesilver-*"),
                    markerColor: "silver",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(13)]
                public AutoMapMarkerSetting LooseOreSulfur = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.GetMatching("block-looseores-sulfur-*"),
                    markerColor: "khaki",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);

                [ProtoMember(14)]
                public AutoMapMarkerSetting LooseOreTin = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.GetMatching("block-looseores-cassiterite-*"),
                    markerColor: "#3C1E05",
                    markerIcon: "pick",
                    markerCoverageRadius: 9);
            }

            [ProtoContract]
            public class MapMarkerSettings_Traders
            {
                [ProtoMember(1)]
                public AutoMapMarkerSetting TraderArtisan = new AutoMapMarkerSetting(
                    enabled: true,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-artisan"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(2)]
                public AutoMapMarkerSetting TraderBuildingMaterials = new AutoMapMarkerSetting(
                    enabled: true,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-buildmaterials"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(3)]
                public AutoMapMarkerSetting TraderClothing = new AutoMapMarkerSetting(
                    enabled: true,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-clothing"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(4)]
                public AutoMapMarkerSetting TraderCommodities = new AutoMapMarkerSetting(
                    enabled: true,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-commodities"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(5)]
                public AutoMapMarkerSetting TraderFoods = new AutoMapMarkerSetting(
                    enabled: true,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-foods"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(6)]
                public AutoMapMarkerSetting TraderFurniture = new AutoMapMarkerSetting(
                    enabled: true,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-furniture"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(7)]
                public AutoMapMarkerSetting TraderLuxuries = new AutoMapMarkerSetting(
                    enabled: true,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-luxuries"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(8)]
                public AutoMapMarkerSetting TraderSurvivalGoods = new AutoMapMarkerSetting(
                    enabled: true,
                    markerTitle: Lang.Get("item-creature-humanoid-trader-survivalgoods"),
                    markerColor: "yellow",
                    markerIcon: "trader",
                    markerCoverageRadius: 20);

                [ProtoMember(9)]
                public AutoMapMarkerSetting TraderTreasureHunter = new AutoMapMarkerSetting(
                    enabled: true,
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
                    markerTitle: Lang.Get("egocarib-mapmarkers:custom-marker-1"),
                    markerColor: "black",
                    markerIcon: "circle",
                    markerCoverageRadius: 1);

                [ProtoMember(2)]
                public AutoMapMarkerSetting CustomMarker2 = new AutoMapMarkerSetting(
                    enabled: false,
                    markerTitle: Lang.Get("egocarib-mapmarkers:custom-marker-2"),
                    markerColor: "black",
                    markerIcon: "circle",
                    markerCoverageRadius: 1);

                [ProtoMember(3)]
                public AutoMapMarkerSetting CustomMarker3 = new AutoMapMarkerSetting(
                    enabled: false,
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
                                    { Lang.GetMatching("block-mushroom-bolete-normal-*"), AutoMapMarkers.OrganicMatter.MushroomBolete },
                                    { Lang.GetMatching("block-mushroom-fieldmushroom-normal-*"), AutoMapMarkers.OrganicMatter.MushroomFieldMushroom },
                                    { Lang.GetMatching("block-mushroom-flyagaric-normal-*"), AutoMapMarkers.OrganicMatter.MushroomFlyAgaric }
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
                                    //{ Lang.GetMatching("block-looseores-fluorite-*"), AutoMapMarkers.SurfaceOre.LooseOreFluorite },
                                    { Lang.Get("egocarib-mapmarkers:gold-ore-bits"), AutoMapMarkers.SurfaceOre.LooseOreGold },
                                    //{ Lang.GetMatching("block-looseores-graphite-*"), AutoMapMarkers.SurfaceOre.LooseOreGraphite },
                                    //{ Lang.GetMatching("block-looseores-kernite-*"), AutoMapMarkers.SurfaceOre.LooseOreKernite },
                                    { Lang.GetMatching("block-looseores-lapislazuli-*"), AutoMapMarkers.SurfaceOre.LooseOreLapisLazuli },
                                    { Lang.GetMatching("block-looseores-galena-*"), AutoMapMarkers.SurfaceOre.LooseOreLead },
                                    { Lang.GetMatching("block-looseores-olivine-peridotite-*"), AutoMapMarkers.SurfaceOre.LooseOreOlivine },
                                    //{ Lang.GetMatching("block-looseores-phosphorite-*"), AutoMapMarkers.SurfaceOre.LooseOrePhosporite },
                                    { Lang.GetMatching("block-looseores-quartz-*"), AutoMapMarkers.SurfaceOre.LooseOreQuartz },
                                    { Lang.Get("egocarib-mapmarkers:silver-ore-bits"), AutoMapMarkers.SurfaceOre.LooseOreSilver },
                                    { Lang.GetMatching("block-looseores-sulfur-*"), AutoMapMarkers.SurfaceOre.LooseOreSulfur },
                                    { Lang.GetMatching("block-looseores-cassiterite-*"), AutoMapMarkers.SurfaceOre.LooseOreTin }
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
                [ProtoMember(5, IsRequired = true)]
                public int MarkerCoverageRadius;

                public AutoMapMarkerSetting() { /* paramaterless constructor required by proto-buf */ }

                [JsonConstructor]
                public AutoMapMarkerSetting(bool enabled, string markerTitle, string markerColor, string markerIcon, int markerCoverageRadius)
                {
                    Enabled = enabled;
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
                        System.Drawing.Color parsedColor;
                        if (MarkerColor.StartsWith("#"))
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
                        return parsedColor.ToArgb() | (255 << 24);
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
            Settings settings = null;
            try
            {
                settings = api.LoadModConfig<Settings>(ConfigFilename);
            }
            catch
            {
                MessageUtil.LogError("Unable to load your mod configuration file "
                    + "(" + ConfigFilename + "). There may have been a syntax error in the file."
                    + "A new default settings file will be generated.");
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
