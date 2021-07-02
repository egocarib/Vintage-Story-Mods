using Newtonsoft.Json;
using System;
using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace egocarib_AutoMapMarkers
{
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
                    MarkerTitle = Lang.GetMatching("block-mushroom-bolete-*"),
                    MarkerColor = "#503922",
                    MarkerIcon = "circle",
                    MarkerCoverageRadius = 6
                };

                public AutoMapMarkerSetting MushroomFieldMushroom = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-mushroom-fieldmushroom-*"),
                    MarkerColor = "ghostwhite",
                    MarkerIcon = "circle",
                    MarkerCoverageRadius = 6
                };

                public AutoMapMarkerSetting MushroomFlyAgaric = new AutoMapMarkerSetting
                {
                    Enabled = false,
                    MarkerTitle = Lang.GetMatching("block-mushroom-flyagaric-*"),
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
                                { Lang.GetMatching("block-mushroom-bolete-*"), AutoMapMarkers.OrganicMatter.MushroomBolete },
                                { Lang.GetMatching("block-mushroom-fieldmushroom-*"), AutoMapMarkers.OrganicMatter.MushroomFieldMushroom },
                                { Lang.GetMatching("block-mushroom-flyagaric-*"), AutoMapMarkers.OrganicMatter.MushroomFlyAgaric }
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
                                { Lang.GetMatching("block-looseores-nativecopper-*"), AutoMapMarkers.SurfaceOre.LooseOreCopper },
                                //{ Lang.GetMatching("block-looseores-fluorite-*"), AutoMapMarkers.SurfaceOre.LooseOreFluorite },
                                //{ Lang.GetMatching("block-looseores-graphite-*"), AutoMapMarkers.SurfaceOre.LooseOreGraphite },
                                //{ Lang.GetMatching("block-looseores-kernite-*"), AutoMapMarkers.SurfaceOre.LooseOreKernite },
                                { Lang.GetMatching("block-looseores-lapislazuli-*"), AutoMapMarkers.SurfaceOre.LooseOreLapisLazuli },
                                { Lang.GetMatching("block-looseores-galena-*"), AutoMapMarkers.SurfaceOre.LooseOreLead },
                                { Lang.GetMatching("block-looseores-olivine-peridotite-*"), AutoMapMarkers.SurfaceOre.LooseOreOlivine },
                                //{ Lang.GetMatching("block-looseores-phosphorite-*"), AutoMapMarkers.SurfaceOre.LooseOrePhosporite },
                                { Lang.GetMatching("block-looseores-sulfur-*"), AutoMapMarkers.SurfaceOre.LooseOreSulfur },
                                { Lang.GetMatching("block-looseores-cassiterite-*"), AutoMapMarkers.SurfaceOre.LooseOreTin }
                            }
                        }
                    };
                }
                return _MapMarkerSettingsCollection;
            }

            public class AutoMapMarkerSetting
            {
                public bool Enabled;
                public string MarkerTitle;
                public string MarkerColor;
                public string MarkerIcon;
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
