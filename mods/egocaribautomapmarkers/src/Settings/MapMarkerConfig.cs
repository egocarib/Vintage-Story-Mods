using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Globalization;
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

        public class Settings
        {
            private OrderedDictionary<string, OrderedDictionary<string, AutoMapMarkerSetting>> _MapMarkerSettingsCollection = null;
            public const string Icons = "circle,bee,cave,home,ladder,pick,rocks,ruins,spiral,star1,star2,trader,vessel";
            public const string IconsVTML = "<icon name=\"wpCircle\">,<icon name=\"wpBee\">,<icon name=\"wpCave\">,<icon name=\"wpHome\">,<icon name=\"wpLadder\">,<icon name=\"wpPick\">,<icon name=\"wpRocks\">,<icon name=\"wpRuins\">,<icon name=\"wpSpiral\">,<icon name=\"wpStar1\">,<icon name=\"wpStar2\">,<icon name=\"wpTrader\">,<icon name=\"wpVessel\">";

            public bool ChatNotifyOnWaypointCreation = false;

            public double ConfigVersion = 1.0;

            public MapMarkerSettingsGrouper AutoMapMarkers = new MapMarkerSettingsGrouper();

            public class MapMarkerSettingsGrouper
            {
                public MapMarkerSettings_OrganicMatter OrganicMatter = new MapMarkerSettings_OrganicMatter();
                public MapMarkerSettings_Ore SurfaceOre = new MapMarkerSettings_Ore();
                public MapMarkerSettings_Traders Traders = new MapMarkerSettings_Traders();
            }

            public class MapMarkerSettings_OrganicMatter
            {
                public AutoMapMarkerSetting Resin = new AutoMapMarkerSetting
                {
                    Enabled = true,
                    MarkerTitle = Lang.Get("item-resin"),
                    MarkerColor = "darkorange",
                    MarkerIcon = "circle",
                    MarkerCoverageRadius = 1
                };

                public AutoMapMarkerSetting Blueberry = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.Get("item-fruit-blueberry"),
                    MarkerColor = "midnightblue",
                    MarkerIcon = "circle",
                    MarkerCoverageRadius = 6
                };

                public AutoMapMarkerSetting Cranberry = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.Get("item-fruit-cranberry"),
                    MarkerColor = "maroon",
                    MarkerIcon = "circle",
                    MarkerCoverageRadius = 6
                };

                public AutoMapMarkerSetting BlackCurrant = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.Get("item-fruit-blackcurrant"),
                    MarkerColor = "#291B1A",
                    MarkerIcon = "circle",
                    MarkerCoverageRadius = 6
                };

                public AutoMapMarkerSetting RedCurrant = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.Get("item-fruit-redcurrant"),
                    MarkerColor = "darkred",
                    MarkerIcon = "circle",
                    MarkerCoverageRadius = 6
                };

                public AutoMapMarkerSetting WhiteCurrant = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.Get("item-fruit-whitecurrant"),
                    MarkerColor = "ivory",
                    MarkerIcon = "circle",
                    MarkerCoverageRadius = 6
                };
                
                public AutoMapMarkerSetting MushroomBolete = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-mushroom-bolete-normal-*"),
                    MarkerColor = "#503922",
                    MarkerIcon = "circle",
                    MarkerCoverageRadius = 6
                };

                public AutoMapMarkerSetting MushroomFieldMushroom = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-mushroom-fieldmushroom-normal-*"),
                    MarkerColor = "ghostwhite",
                    MarkerIcon = "circle",
                    MarkerCoverageRadius = 6
                };

                public AutoMapMarkerSetting MushroomFlyAgaric = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-mushroom-flyagaric-normal-*"),
                    MarkerColor = "brown",
                    MarkerIcon = "circle",
                    MarkerCoverageRadius = 6
                };
            }

            public class MapMarkerSettings_Ore
            { 
                public AutoMapMarkerSetting LooseOreAnthracite = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-looseores-anthracite-*"),
                    MarkerColor = "black",
                    MarkerIcon = "pick",
                    MarkerCoverageRadius = 9
                };

                public AutoMapMarkerSetting LooseOreBlackCoal = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-looseores-bituminouscoal-*"),
                    MarkerColor = "black",
                    MarkerIcon = "pick",
                    MarkerCoverageRadius = 9
                };

                public AutoMapMarkerSetting LooseOreBorax = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-looseores-borax-*"),
                    MarkerColor = "ghostwhite",
                    MarkerIcon = "pick",
                    MarkerCoverageRadius = 9
                };

                public AutoMapMarkerSetting LooseOreBrownCoal = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-looseores-lignite-*"),
                    MarkerColor = "black",
                    MarkerIcon = "pick",
                    MarkerCoverageRadius = 9
                };

                public AutoMapMarkerSetting LooseOreCinnabar = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-looseores-cinnabar-*"),
                    MarkerColor = "crimson",
                    MarkerIcon = "pick",
                    MarkerCoverageRadius = 9
                };

                public AutoMapMarkerSetting LooseOreCopper = new AutoMapMarkerSetting
                {
                    Enabled = true,
                    MarkerTitle = Lang.GetMatching("block-looseores-nativecopper-*"),
                    MarkerColor = "darkorange",
                    MarkerIcon = "pick",
                    MarkerCoverageRadius = 9
                };

                //public AutoMapMarkerSetting LooseOreFluorite = new AutoMapMarkerSetting
                //{
                //    Enabled = false,
                //    MarkerTitle = Lang.GetMatching("block-looseores-fluorite-*"),
                //    MarkerColor = "black",
                //    MarkerIcon = "pick",
                //    MarkerCoverageRadius = 9
                //};

                public AutoMapMarkerSetting LooseOreGold = new AutoMapMarkerSetting
                {
                    Enabled = true,
                    MarkerTitle = Lang.GetMatching("block-looseores-quartz_nativegold-*"),
                    MarkerColor = "gold",
                    MarkerIcon = "pick",
                    MarkerCoverageRadius = 9
                };

                //public AutoMapMarkerSetting LooseOreGraphite = new AutoMapMarkerSetting
                //{
                //    Enabled = false,
                //    MarkerTitle = Lang.GetMatching("block-looseores-graphite-*"),
                //    MarkerColor = "black",
                //    MarkerIcon = "pick",
                //    MarkerCoverageRadius = 9
                //};

                //public AutoMapMarkerSetting LooseOreKernite = new AutoMapMarkerSetting
                //{
                //    Enabled = false,
                //    MarkerTitle = Lang.GetMatching("block-looseores-kernite-*"),
                //    MarkerColor = "black",
                //    MarkerIcon = "pick",
                //    MarkerCoverageRadius = 9
                //};

                public AutoMapMarkerSetting LooseOreLapisLazuli = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-looseores-lapislazuli-*"),
                    MarkerColor = "royalblue",
                    MarkerIcon = "pick",
                    MarkerCoverageRadius = 9
                };

                public AutoMapMarkerSetting LooseOreLead = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-looseores-galena-*"),
                    MarkerColor = "slategray",
                    MarkerIcon = "pick",
                    MarkerCoverageRadius = 9
                };

                public AutoMapMarkerSetting LooseOreOlivine = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-looseores-olivine-peridotite-*"),
                    MarkerColor = "olivedrab",
                    MarkerIcon = "pick",
                    MarkerCoverageRadius = 9
                };

                //public AutoMapMarkerSetting LooseOrePhosporite = new AutoMapMarkerSetting
                //{
                //    Enabled = false,
                //    MarkerTitle = Lang.GetMatching("block-looseores-phosphorite-*"),
                //    MarkerColor = "black",
                //    MarkerIcon = "pick",
                //    MarkerCoverageRadius = 9
                //};

                public AutoMapMarkerSetting LooseOreQuartz = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-looseores-quartz-*"),
                    MarkerColor = "white",
                    MarkerIcon = "pick",
                    MarkerCoverageRadius = 9
                };

                public AutoMapMarkerSetting LooseOreSilver = new AutoMapMarkerSetting
                {
                    Enabled = true,
                    MarkerTitle = Lang.GetMatching("block-looseores-quartz_nativesilver-*"),
                    MarkerColor = "silver",
                    MarkerIcon = "pick",
                    MarkerCoverageRadius = 9
                };

                public AutoMapMarkerSetting LooseOreSulfur = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-looseores-sulfur-*"),
                    MarkerColor = "khaki",
                    MarkerIcon = "pick",
                    MarkerCoverageRadius = 9
                };

                public AutoMapMarkerSetting LooseOreTin = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-looseores-cassiterite-*"),
                    MarkerColor = "#3C1E05",
                    MarkerIcon = "pick",
                    MarkerCoverageRadius = 9
                };
            }

            public class MapMarkerSettings_Traders
            {
                public AutoMapMarkerSetting TraderArtisan = new AutoMapMarkerSetting
                {
                    Enabled = true,
                    MarkerTitle = Lang.Get("item-creature-humanoid-trader-artisan"),
                    MarkerColor = "yellow",
                    MarkerIcon = "trader",
                    MarkerCoverageRadius = 20
                };

                public AutoMapMarkerSetting TraderBuildingMaterials = new AutoMapMarkerSetting
                {
                    Enabled = true,
                    MarkerTitle = Lang.Get("item-creature-humanoid-trader-buildmaterials"),
                    MarkerColor = "yellow",
                    MarkerIcon = "trader",
                    MarkerCoverageRadius = 20
                };

                public AutoMapMarkerSetting TraderClothing = new AutoMapMarkerSetting
                {
                    Enabled = true,
                    MarkerTitle = Lang.Get("item-creature-humanoid-trader-clothing"),
                    MarkerColor = "yellow",
                    MarkerIcon = "trader",
                    MarkerCoverageRadius = 20
                };

                public AutoMapMarkerSetting TraderCommodities = new AutoMapMarkerSetting
                {
                    Enabled = true,
                    MarkerTitle = Lang.Get("item-creature-humanoid-trader-commodities"),
                    MarkerColor = "yellow",
                    MarkerIcon = "trader",
                    MarkerCoverageRadius = 20
                };

                public AutoMapMarkerSetting TraderFoods = new AutoMapMarkerSetting
                {
                    Enabled = true,
                    MarkerTitle = Lang.Get("item-creature-humanoid-trader-foods"),
                    MarkerColor = "yellow",
                    MarkerIcon = "trader",
                    MarkerCoverageRadius = 20
                };

                public AutoMapMarkerSetting TraderFurniture = new AutoMapMarkerSetting
                {
                    Enabled = true,
                    MarkerTitle = Lang.Get("item-creature-humanoid-trader-furniture"),
                    MarkerColor = "yellow",
                    MarkerIcon = "trader",
                    MarkerCoverageRadius = 20
                };

                public AutoMapMarkerSetting TraderLuxuries = new AutoMapMarkerSetting
                {
                    Enabled = true,
                    MarkerTitle = Lang.Get("item-creature-humanoid-trader-luxuries"),
                    MarkerColor = "yellow",
                    MarkerIcon = "trader",
                    MarkerCoverageRadius = 20
                };

                public AutoMapMarkerSetting TraderSurvivalGoods = new AutoMapMarkerSetting
                {
                    Enabled = true,
                    MarkerTitle = Lang.Get("item-creature-humanoid-trader-survivalgoods"),
                    MarkerColor = "yellow",
                    MarkerIcon = "trader",
                    MarkerCoverageRadius = 20
                };

                public AutoMapMarkerSetting TraderTreasureHunter = new AutoMapMarkerSetting
                {
                    Enabled = true,
                    MarkerTitle = Lang.Get("item-creature-humanoid-trader-treasurehunter"),
                    MarkerColor = "yellow",
                    MarkerIcon = "trader",
                    MarkerCoverageRadius = 20
                };
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
                        }
                    };
                }
                return _MapMarkerSettingsCollection;
            }

            [ProtoContract]
            public class AutoMapMarkerSetting
            {
                [ProtoMember(1)]
                public bool Enabled;
                [ProtoMember(2)]
                public string MarkerTitle;
                [ProtoMember(3)]
                public string MarkerColor;
                [ProtoMember(4)]
                public string MarkerIcon;
                [ProtoMember(5)]
                public int MarkerCoverageRadius;

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

        public static Settings GetSettings(ICoreAPI api)
        {
            Settings settings = null;
            try
            {
                settings = api.LoadModConfig<Settings>(ConfigFilename);
            }
            catch
            {
                api.Logger.Warning("Auto Map Markers Mod: Error attempting to load your mod configuration file "
                    + "(" + ConfigFilename + "). There may have been a syntax error in the file.");
            }
            if (settings == null)
            {
                settings = new Settings();
                SaveSettings(api, settings); //Create a default settings file if one didn't already exist.
            }
            return settings;
        }

        public static void SaveSettings(ICoreAPI api, Settings settings)
        {
            api.StoreModConfig<Settings>(settings, ConfigFilename);
        }
    }
}
