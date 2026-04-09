using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Egocarib.AutoMapMarkers.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Config;
using static Egocarib.AutoMapMarkers.Settings.MapMarkerConfig.Settings;

namespace Egocarib.AutoMapMarkers.Settings
{
    public class MarkerSettingEntry
    {
        public string DisplayName { get; set; }
        public AutoMapMarkerSetting Setting { get; set; }
        public bool IsCore { get; set; }
        public string EntryLabel { get; set; }
        public bool IsExpandable { get; set; } = false;
        public bool IsExpanded { get; set; } = false;
        public List<MarkerSettingEntry> SubEntries { get; set; }
    }

    public class MarkerSettingSubgroup
    {
        public string SubgroupName { get; set; }
        public List<MarkerSettingEntry> Entries { get; set; } = new List<MarkerSettingEntry>();
    }

    public class MarkerSettingTab
    {
        public string TabKey { get; set; }
        public string LangKey { get; set; }
        public List<MarkerSettingSubgroup> Subgroups { get; set; } = new List<MarkerSettingSubgroup>();
        public bool HasSubheadings => Subgroups.Count > 1;
    }

    public class MarkerSettingLayout
    {
        public List<MarkerSettingTab> Tabs { get; set; } = new List<MarkerSettingTab>();
        public Vintagestory.API.Datastructures.OrderedDictionary<string, AutoMapMarkerSetting> CustomEntries { get; set; }
    }

    /// <summary>
    /// Handles loading and saving the new settings.json format.
    /// Bridges between external JSON definitions and the existing AutoMapMarkerSetting/grouper system.
    /// </summary>
    public static class MarkerSettingsPersistence
    {
        public const string SettingsFolder = "AutoMapMarkers";
        public const string SettingsFileName = "settings.json";

        /// <summary>
        /// Represents the settings.json file structure.
        /// </summary>
        private class SettingsFile
        {
            public double ConfigVersion { get; set; } = 5.0;
            public bool ChatNotifyOnWaypointCreation { get; set; } = false;
            public bool EnableWaypointDeletionHotkey { get; set; } = false;
            public bool ChatNotifyOnWaypointDeletion { get; set; } = true;
            public bool EnableCustomHotkeys { get; set; } = false;
            public bool EnableMarkOnSneak { get; set; } = true;
            public bool EnableMarkOnInteract { get; set; } = true;
            public bool LabelCoordinates { get; set; } = false;
            public bool SuppressMarkerOnFarmland { get; set; } = true;
            public bool EnableDetectHotkey { get; set; } = false;
            public bool ChatNotifyOnBoatMarker { get; set; } = false;
            public bool DisableAllModFeatures { get; set; } = true;
            public Dictionary<string, MarkerOverride> MarkerOverrides { get; set; } = new Dictionary<string, MarkerOverride>();
            public Dictionary<string, bool> ExpandStates { get; set; } = new Dictionary<string, bool>();
        }

        /// <summary>
        /// A user override for a single marker — only contains fields that differ from defaults.
        /// </summary>
        public class MarkerOverride
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool? Enabled { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Title { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Icon { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Color { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool? Pinned { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? CoverageRadius { get; set; }
        }

        /// <summary>
        /// Loads settings from the new pipeline: definitions + settings.json overrides.
        /// Returns a fully populated Settings object with grouper fields set.
        /// </summary>
        public static MapMarkerConfig.Settings LoadSettings(string modConfigPath, List<MarkerCategoryDef> definitions)
        {
            var settings = new MapMarkerConfig.Settings();
            string settingsPath = Path.Combine(modConfigPath, SettingsFolder, SettingsFileName);

            SettingsFile settingsFile = null;
            if (File.Exists(settingsPath))
            {
                try
                {
                    string json = File.ReadAllText(settingsPath);
                    settingsFile = JsonConvert.DeserializeObject<SettingsFile>(json);
                }
                catch (Exception ex)
                {
                    MessageUtil.LogError($"Failed to load {SettingsFileName}: {ex.Message}. Using defaults.");
                }
            }

            // Apply boolean toggles from settings file (or use defaults)
            if (settingsFile != null)
            {
                settings.ChatNotifyOnWaypointCreation = settingsFile.ChatNotifyOnWaypointCreation;
                settings.EnableWaypointDeletionHotkey = settingsFile.EnableWaypointDeletionHotkey;
                settings.ChatNotifyOnWaypointDeletion = settingsFile.ChatNotifyOnWaypointDeletion;
                settings.EnableCustomHotkeys = settingsFile.EnableCustomHotkeys;
                settings.EnableMarkOnSneak = settingsFile.EnableMarkOnSneak;
                settings.EnableMarkOnInteract = settingsFile.EnableMarkOnInteract;
                settings.LabelCoordinates = settingsFile.LabelCoordinates;
                settings.SuppressMarkerOnFarmland = settingsFile.SuppressMarkerOnFarmland;
                settings.EnableDetectHotkey = settingsFile.EnableDetectHotkey;
                settings.ChatNotifyOnBoatMarker = settingsFile.ChatNotifyOnBoatMarker;
                settings.DisableAllModFeatures = settingsFile.DisableAllModFeatures;
                settings.ConfigVersion = settingsFile.ConfigVersion;
            }

            var overrides = settingsFile?.MarkerOverrides ?? new Dictionary<string, MarkerOverride>();

            // For each definition entry, create an AutoMapMarkerSetting from defaults + overrides,
            // then populate the corresponding grouper field.
            foreach (var category in definitions)
            {
                foreach (var entry in category.Entries)
                {
                    var markerSetting = CreateMarkerSetting(entry.Defaults, overrides.GetValueOrDefault(entry.Label));
                    PopulateGrouperField(settings, entry.Label, markerSetting);

                    if (entry.ExpandableEntries != null)
                    {
                        foreach (var subEntry in entry.ExpandableEntries)
                        {
                            var subSetting = CreateMarkerSetting(subEntry.Defaults, overrides.GetValueOrDefault(subEntry.Label));
                            PopulateGrouperField(settings, subEntry.Label, subSetting);
                        }
                    }
                }
            }

            if (settingsFile?.ExpandStates != null)
                settings.ExpandStates = settingsFile.ExpandStates;

            return settings;
        }

        /// <summary>
        /// Saves settings to settings.json, computing diffs against definition defaults.
        /// </summary>
        public static void SaveSettings(string modConfigPath, MapMarkerConfig.Settings settings, List<MarkerCategoryDef> definitions)
        {
            var settingsFile = new SettingsFile
            {
                ConfigVersion = 5.0,
                ChatNotifyOnWaypointCreation = settings.ChatNotifyOnWaypointCreation,
                EnableWaypointDeletionHotkey = settings.EnableWaypointDeletionHotkey,
                ChatNotifyOnWaypointDeletion = settings.ChatNotifyOnWaypointDeletion,
                EnableCustomHotkeys = settings.EnableCustomHotkeys,
                EnableMarkOnSneak = settings.EnableMarkOnSneak,
                EnableMarkOnInteract = settings.EnableMarkOnInteract,
                LabelCoordinates = settings.LabelCoordinates,
                SuppressMarkerOnFarmland = settings.SuppressMarkerOnFarmland,
                EnableDetectHotkey = settings.EnableDetectHotkey,
                ChatNotifyOnBoatMarker = settings.ChatNotifyOnBoatMarker,
                DisableAllModFeatures = settings.DisableAllModFeatures
            };

            // Compute diffs: for each definition entry, check if the current setting differs from default
            foreach (var category in definitions)
            {
                foreach (var entry in category.Entries)
                {
                    var currentSetting = GetGrouperField(settings, entry.Label);
                    if (currentSetting == null) continue;

                    var over = ComputeOverride(entry.Defaults, currentSetting);
                    if (over != null)
                        settingsFile.MarkerOverrides[entry.Label] = over;

                    if (entry.ExpandableEntries != null)
                    {
                        foreach (var subEntry in entry.ExpandableEntries)
                        {
                            var subSetting = GetGrouperField(settings, subEntry.Label);
                            if (subSetting == null) continue;
                            var subOver = ComputeOverride(subEntry.Defaults, subSetting);
                            if (subOver != null)
                                settingsFile.MarkerOverrides[subEntry.Label] = subOver;
                        }
                    }
                }
            }

            settingsFile.ExpandStates = settings.ExpandStates;

            string settingsDir = Path.Combine(modConfigPath, SettingsFolder);
            if (!Directory.Exists(settingsDir))
                Directory.CreateDirectory(settingsDir);

            string settingsPath = Path.Combine(settingsDir, SettingsFileName);
            string json = JsonConvert.SerializeObject(settingsFile, Formatting.Indented);
            File.WriteAllText(settingsPath, json);
        }

        /// <summary>
        /// Checks whether settings.json exists.
        /// </summary>
        public static bool SettingsFileExists(string modConfigPath)
        {
            return File.Exists(Path.Combine(modConfigPath, SettingsFolder, SettingsFileName));
        }

        /// <summary>
        /// Creates a default settings.json with only boolean toggles (no marker overrides).
        /// Used on the server side when no settings exist and no legacy config needs migration.
        /// </summary>
        public static void CreateDefaultSettingsFile(string modConfigPath)
        {
            var settingsFile = new SettingsFile();

            string settingsDir = Path.Combine(modConfigPath, SettingsFolder);
            if (!Directory.Exists(settingsDir))
                Directory.CreateDirectory(settingsDir);

            string settingsPath = Path.Combine(settingsDir, SettingsFileName);
            string json = JsonConvert.SerializeObject(settingsFile, Formatting.Indented);
            File.WriteAllText(settingsPath, json);
        }

        /// <summary>
        /// Resolves a marker category to a fixed tab key and optional subgroup key.
        /// </summary>
        internal static (string Tab, string Subgroup) ResolveTabMapping(MarkerCategory category)
        {
            return category switch
            {
                MarkerCategory.Flora      => ("Flora", null),
                MarkerCategory.SurfaceOre => ("Ore", "Surface Ore"),
                MarkerCategory.DeepOre    => ("Ore", "Deep Ore"),
                MarkerCategory.Other       => ("Other", null),
                _                         => ("Other", null)
            };
        }

        /// <summary>
        /// Builds the GUI layout from definitions + current settings.
        /// Returns a MarkerSettingLayout with fixed tabs, subgroups, and custom entries.
        /// </summary>
        public static MarkerSettingLayout BuildSettingLayout(
            MapMarkerConfig.Settings settings, List<MarkerCategoryDef> definitions)
        {
            // Initialize tabs in fixed order with lang keys
            var tabDefs = new[]
            {
                ("Flora",   "egocarib-mapmarkers:organic-matter"),
                ("Ore",     "egocarib-mapmarkers:ore"),
                ("Other",   "Other"),
            };
            var tabsByKey = new Dictionary<string, MarkerSettingTab>();
            var tabs = new List<MarkerSettingTab>();
            foreach (var (tabKey, langKey) in tabDefs)
            {
                var tab = new MarkerSettingTab { TabKey = tabKey, LangKey = langKey };
                tabs.Add(tab);
                tabsByKey[tabKey] = tab;
            }

            // For each definition category, resolve tab mapping and populate entries
            foreach (var category in definitions)
            {
                var (tabKey, subgroupKey) = ResolveTabMapping(category.Category);

                if (!tabsByKey.TryGetValue(tabKey, out var tab))
                {
                    // Shouldn't happen since unrecognized maps to Other, but be safe
                    tab = tabsByKey["Other"];
                }

                // Find or create the subgroup within the tab
                var subgroup = tab.Subgroups.FirstOrDefault(sg => sg.SubgroupName == subgroupKey);
                if (subgroup == null)
                {
                    subgroup = new MarkerSettingSubgroup { SubgroupName = subgroupKey };
                    tab.Subgroups.Add(subgroup);
                }

                foreach (var entry in category.Entries)
                {
                    string displayName = ResolveLangKey(entry.Label);
                    var markerSetting = GetGrouperField(settings, entry.Label);
                    bool isExpandable = entry.ExpandableEntries?.Count > 0;

                    if (markerSetting == null && !isExpandable)
                        continue;

                    var layoutEntry = new MarkerSettingEntry
                    {
                        DisplayName = displayName,
                        Setting = markerSetting,
                        IsCore = entry.IsCore,
                        EntryLabel = entry.Label,
                        IsExpandable = isExpandable,
                        IsExpanded = settings.IsExpanded(entry)
                    };

                    if (isExpandable)
                    {
                        layoutEntry.SubEntries = new List<MarkerSettingEntry>();
                        foreach (var subEntry in entry.ExpandableEntries)
                        {
                            var subSetting = GetGrouperField(settings, subEntry.Label);
                            if (subSetting == null) continue;
                            layoutEntry.SubEntries.Add(new MarkerSettingEntry
                            {
                                DisplayName = ResolveLangKey(subEntry.Label),
                                Setting = subSetting,
                                IsCore = subEntry.IsCore,
                                EntryLabel = subEntry.Label
                            });
                        }
                    }

                    subgroup.Entries.Add(layoutEntry);
                }
            }

            // Remove empty subgroups and tabs
            foreach (var tab in tabs)
                tab.Subgroups.RemoveAll(sg => sg.Entries.Count == 0);
            tabs.RemoveAll(t => t.Subgroups.Count == 0);

            // Build custom entries
            var customEntries = new Vintagestory.API.Datastructures.OrderedDictionary<string, AutoMapMarkerSetting>
            {
                { Lang.Get("egocarib-mapmarkers:custom-marker-1"), settings.AutoMapMarkers.Custom.CustomMarker1 },
                { Lang.Get("egocarib-mapmarkers:custom-marker-2"), settings.AutoMapMarkers.Custom.CustomMarker2 },
                { Lang.Get("egocarib-mapmarkers:custom-marker-3"), settings.AutoMapMarkers.Custom.CustomMarker3 }
            };

            return new MarkerSettingLayout
            {
                Tabs = tabs,
                CustomEntries = customEntries
            };
        }

        /// <summary>
        /// Resolves a lang key that may use the "domain:key" format.
        /// Keys starting with "game:" use Lang.Get/Lang.GetMatching with the key portion.
        /// Keys starting with "egocarib-mapmarkers:" are passed to Lang.Get directly.
        /// Keys containing "*" use Lang.GetMatching.
        /// </summary>
        private static string ResolveLangKey(string label)
        {
            if (string.IsNullOrEmpty(label))
                return label;

            // Strip "game:" prefix for the game domain
            string langKey = label;
            if (langKey.StartsWith("game:", StringComparison.Ordinal))
                langKey = langKey.Substring("game:".Length);

            // Use GetMatching for wildcard patterns, Get for exact keys
            if (langKey.Contains("*"))
                return Lang.GetMatching(langKey);

            return Lang.Get(langKey);
        }

        /// <summary>
        /// Creates an AutoMapMarkerSetting from definition defaults, applying any user overrides.
        /// </summary>
        private static AutoMapMarkerSetting CreateMarkerSetting(MarkerDefaultsDef defaults, MarkerOverride over)
        {
            string resolvedTitle = ResolveLangKey(defaults.Title);

            return new AutoMapMarkerSetting(
                enabled: over?.Enabled ?? defaults.Enabled,
                pinned: over?.Pinned ?? defaults.Pinned,
                markerTitle: over?.Title != null ? ResolveLangKey(over.Title) : resolvedTitle,
                markerColor: over?.Color ?? defaults.Color,
                markerIcon: over?.Icon ?? defaults.Icon,
                markerCoverageRadius: over?.CoverageRadius ?? defaults.CoverageRadius
            );
        }

        /// <summary>
        /// Computes the diff between current setting and definition defaults.
        /// Returns null if no differences exist.
        /// </summary>
        private static MarkerOverride ComputeOverride(MarkerDefaultsDef defaults, AutoMapMarkerSetting current)
        {
            string resolvedTitle = ResolveLangKey(defaults.Title);
            var over = new MarkerOverride();
            bool hasDiff = false;

            if (current.Enabled != defaults.Enabled)
            {
                over.Enabled = current.Enabled;
                hasDiff = true;
            }
            if (!string.Equals(current.MarkerTitle, resolvedTitle, StringComparison.Ordinal))
            {
                over.Title = current.MarkerTitle;
                hasDiff = true;
            }
            if (!string.Equals(current.MarkerIcon, defaults.Icon, StringComparison.OrdinalIgnoreCase))
            {
                over.Icon = current.MarkerIcon;
                hasDiff = true;
            }
            if (!string.Equals(current.MarkerColor, defaults.Color, StringComparison.OrdinalIgnoreCase))
            {
                over.Color = current.MarkerColor;
                hasDiff = true;
            }
            if (current.MarkerPinned != defaults.Pinned)
            {
                over.Pinned = current.MarkerPinned;
                hasDiff = true;
            }
            if (current.MarkerCoverageRadius != defaults.CoverageRadius)
            {
                over.CoverageRadius = current.MarkerCoverageRadius;
                hasDiff = true;
            }

            return hasDiff ? over : null;
        }

        #region Label→Grouper Field Mapping

        /// <summary>
        /// Maps definition labels to getter/setter pairs for the corresponding grouper fields.
        /// This single dictionary replaces the duplicate switch statements in PopulateGrouperField and GetGrouperField.
        /// </summary>
        private static readonly Dictionary<string, (
            Func<MapMarkerConfig.Settings, AutoMapMarkerSetting> Get,
            Action<MapMarkerConfig.Settings, AutoMapMarkerSetting> Set
        )> GrouperFieldMap = new()
        {
            // Organic Matter
            ["game:item-resin"] = (s => s.AutoMapMarkers.OrganicMatter.Resin, (s, v) => s.AutoMapMarkers.OrganicMatter.Resin = v),
            ["egocarib-mapmarkers:berries"] = (s => s.AutoMapMarkers.OrganicMatter.Berries, (s, v) => s.AutoMapMarkers.OrganicMatter.Berries = v),
            ["game:item-fruit-blueberry"] = (s => s.AutoMapMarkers.OrganicMatter.Blueberry, (s, v) => s.AutoMapMarkers.OrganicMatter.Blueberry = v),
            ["game:item-fruit-beautyberry"] = (s => s.AutoMapMarkers.OrganicMatter.Beautyberry, (s, v) => s.AutoMapMarkers.OrganicMatter.Beautyberry = v),
            ["game:item-fruit-cranberry"] = (s => s.AutoMapMarkers.OrganicMatter.Cranberry, (s, v) => s.AutoMapMarkers.OrganicMatter.Cranberry = v),
            ["game:item-fruit-blackcurrant"] = (s => s.AutoMapMarkers.OrganicMatter.BlackCurrant, (s, v) => s.AutoMapMarkers.OrganicMatter.BlackCurrant = v),
            ["game:item-fruit-redcurrant"] = (s => s.AutoMapMarkers.OrganicMatter.RedCurrant, (s, v) => s.AutoMapMarkers.OrganicMatter.RedCurrant = v),
            ["game:item-fruit-whitecurrant"] = (s => s.AutoMapMarkers.OrganicMatter.WhiteCurrant, (s, v) => s.AutoMapMarkers.OrganicMatter.WhiteCurrant = v),
            ["game:item-fruit-strawberry"] = (s => s.AutoMapMarkers.OrganicMatter.Strawberry, (s, v) => s.AutoMapMarkers.OrganicMatter.Strawberry = v),
            ["game:item-fruit-raspberry"] = (s => s.AutoMapMarkers.OrganicMatter.Raspberry, (s, v) => s.AutoMapMarkers.OrganicMatter.Raspberry = v),
            ["game:item-fruit-blackberry"] = (s => s.AutoMapMarkers.OrganicMatter.Blackberry, (s, v) => s.AutoMapMarkers.OrganicMatter.Blackberry = v),
            ["game:item-fruit-cloudberry"] = (s => s.AutoMapMarkers.OrganicMatter.Cloudberry, (s, v) => s.AutoMapMarkers.OrganicMatter.Cloudberry = v),
            ["egocarib-mapmarkers:safe-mushrooms"] = (s => s.AutoMapMarkers.OrganicMatter.SafeMushroom, (s, v) => s.AutoMapMarkers.OrganicMatter.SafeMushroom = v),
            ["egocarib-mapmarkers:unsafe-mushrooms"] = (s => s.AutoMapMarkers.OrganicMatter.UnsafeMushroom, (s, v) => s.AutoMapMarkers.OrganicMatter.UnsafeMushroom = v),
            ["egocarib-mapmarkers:flowers"] = (s => s.AutoMapMarkers.OrganicMatter.Flower, (s, v) => s.AutoMapMarkers.OrganicMatter.Flower = v),
            ["egocarib-mapmarkers:fruit-trees"] = (s => s.AutoMapMarkers.OrganicMatter.FruitTree, (s, v) => s.AutoMapMarkers.OrganicMatter.FruitTree = v),
            ["egocarib-mapmarkers:wild-crops"] = (s => s.AutoMapMarkers.OrganicMatter.WildCrop, (s, v) => s.AutoMapMarkers.OrganicMatter.WildCrop = v),
            ["egocarib-mapmarkers:reeds"] = (s => s.AutoMapMarkers.OrganicMatter.Reed, (s, v) => s.AutoMapMarkers.OrganicMatter.Reed = v),
            ["egocarib-mapmarkers:tule"] = (s => s.AutoMapMarkers.OrganicMatter.Tule, (s, v) => s.AutoMapMarkers.OrganicMatter.Tule = v),

            // Surface Ore
            ["game:block-looseores-anthracite-*"] = (s => s.AutoMapMarkers.SurfaceOre.LooseOreAnthracite, (s, v) => s.AutoMapMarkers.SurfaceOre.LooseOreAnthracite = v),
            ["game:block-looseores-bituminouscoal-*"] = (s => s.AutoMapMarkers.SurfaceOre.LooseOreBlackCoal, (s, v) => s.AutoMapMarkers.SurfaceOre.LooseOreBlackCoal = v),
            ["game:block-looseores-borax-*"] = (s => s.AutoMapMarkers.SurfaceOre.LooseOreBorax, (s, v) => s.AutoMapMarkers.SurfaceOre.LooseOreBorax = v),
            ["game:block-looseores-lignite-*"] = (s => s.AutoMapMarkers.SurfaceOre.LooseOreBrownCoal, (s, v) => s.AutoMapMarkers.SurfaceOre.LooseOreBrownCoal = v),
            ["game:block-looseores-cinnabar-*"] = (s => s.AutoMapMarkers.SurfaceOre.LooseOreCinnabar, (s, v) => s.AutoMapMarkers.SurfaceOre.LooseOreCinnabar = v),
            ["egocarib-mapmarkers:copper-ore-bits"] = (s => s.AutoMapMarkers.SurfaceOre.LooseOreCopper, (s, v) => s.AutoMapMarkers.SurfaceOre.LooseOreCopper = v),
            ["egocarib-mapmarkers:gold-ore-bits"] = (s => s.AutoMapMarkers.SurfaceOre.LooseOreGold, (s, v) => s.AutoMapMarkers.SurfaceOre.LooseOreGold = v),
            ["game:block-looseores-lapislazuli-*"] = (s => s.AutoMapMarkers.SurfaceOre.LooseOreLapisLazuli, (s, v) => s.AutoMapMarkers.SurfaceOre.LooseOreLapisLazuli = v),
            ["game:block-looseores-galena-*"] = (s => s.AutoMapMarkers.SurfaceOre.LooseOreLead, (s, v) => s.AutoMapMarkers.SurfaceOre.LooseOreLead = v),
            ["game:block-looseores-olivine-peridotite-*"] = (s => s.AutoMapMarkers.SurfaceOre.LooseOreOlivine, (s, v) => s.AutoMapMarkers.SurfaceOre.LooseOreOlivine = v),
            ["game:block-looseores-quartz-*"] = (s => s.AutoMapMarkers.SurfaceOre.LooseOreQuartz, (s, v) => s.AutoMapMarkers.SurfaceOre.LooseOreQuartz = v),
            ["egocarib-mapmarkers:silver-ore-bits"] = (s => s.AutoMapMarkers.SurfaceOre.LooseOreSilver, (s, v) => s.AutoMapMarkers.SurfaceOre.LooseOreSilver = v),
            ["game:block-looseores-sulfur-*"] = (s => s.AutoMapMarkers.SurfaceOre.LooseOreSulfur, (s, v) => s.AutoMapMarkers.SurfaceOre.LooseOreSulfur = v),
            ["game:block-looseores-cassiterite-*"] = (s => s.AutoMapMarkers.SurfaceOre.LooseOreTin, (s, v) => s.AutoMapMarkers.SurfaceOre.LooseOreTin = v),

            // Deep Ore
            ["game:block-ore-anthracite-*"] = (s => s.AutoMapMarkers.DeepOre.DeepOreAnthracite, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreAnthracite = v),
            ["game:block-ore-*-bismuthinite-*"] = (s => s.AutoMapMarkers.DeepOre.DeepOreBismuth, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreBismuth = v),
            ["game:block-ore-bituminouscoal-*"] = (s => s.AutoMapMarkers.DeepOre.DeepOreBlackCoal, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreBlackCoal = v),
            ["game:block-ore-borax-*"] = (s => s.AutoMapMarkers.DeepOre.DeepOreBorax, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreBorax = v),
            ["game:block-ore-lignite-*"] = (s => s.AutoMapMarkers.DeepOre.DeepOreBrownCoal, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreBrownCoal = v),
            ["game:block-ore-cinnabar-*"] = (s => s.AutoMapMarkers.DeepOre.DeepOreCinnabar, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreCinnabar = v),
            ["egocarib-mapmarkers:copper-ore"] = (s => s.AutoMapMarkers.DeepOre.DeepOreCopper, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreCopper = v),
            ["egocarib-mapmarkers:gold-ore"] = (s => s.AutoMapMarkers.DeepOre.DeepOreGold, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreGold = v),
            ["game:block-ore-*-limonite-*"] = (s => s.AutoMapMarkers.DeepOre.DeepOreIron, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreIron = v),
            ["game:block-ore-lapislazuli-*"] = (s => s.AutoMapMarkers.DeepOre.DeepOreLapisLazuli, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreLapisLazuli = v),
            ["game:block-ore-*-galena-*"] = (s => s.AutoMapMarkers.DeepOre.DeepOreLead, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreLead = v),
            ["game:block-ore-*-pentlandite-*"] = (s => s.AutoMapMarkers.DeepOre.DeepOreNickel, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreNickel = v),
            ["game:ore-olivine"] = (s => s.AutoMapMarkers.DeepOre.DeepOreOlivine, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreOlivine = v),
            ["game:ore-quartz"] = (s => s.AutoMapMarkers.DeepOre.DeepOreQuartz, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreQuartz = v),
            ["egocarib-mapmarkers:silver-ore"] = (s => s.AutoMapMarkers.DeepOre.DeepOreSilver, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreSilver = v),
            ["game:ore-sulfur"] = (s => s.AutoMapMarkers.DeepOre.DeepOreSulfur, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreSulfur = v),
            ["game:block-ore-*-cassiterite-*"] = (s => s.AutoMapMarkers.DeepOre.DeepOreTin, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreTin = v),
            ["game:block-ore-*-ilmenite-*"] = (s => s.AutoMapMarkers.DeepOre.DeepOreTitanium, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreTitanium = v),
            ["game:block-ore-*-sphalerite-*"] = (s => s.AutoMapMarkers.DeepOre.DeepOreZinc, (s, v) => s.AutoMapMarkers.DeepOre.DeepOreZinc = v),

            // Misc
            ["egocarib-mapmarkers:beehive"] = (s => s.AutoMapMarkers.MiscBlocks.Beehive, (s, v) => s.AutoMapMarkers.MiscBlocks.Beehive = v),
            ["game:wpSuggestion-spiral"] = (s => s.AutoMapMarkers.MiscBlocks.Translocator, (s, v) => s.AutoMapMarkers.MiscBlocks.Translocator = v),
            ["game:item-clay-red"] = (s => s.AutoMapMarkers.MiscBlocks.BlockRedClay, (s, v) => s.AutoMapMarkers.MiscBlocks.BlockRedClay = v),
            ["game:item-clay-blue"] = (s => s.AutoMapMarkers.MiscBlocks.BlockBlueClay, (s, v) => s.AutoMapMarkers.MiscBlocks.BlockBlueClay = v),
            ["game:item-clay-fire"] = (s => s.AutoMapMarkers.MiscBlocks.BlockFireClay, (s, v) => s.AutoMapMarkers.MiscBlocks.BlockFireClay = v),
            ["game:block-peat-none"] = (s => s.AutoMapMarkers.MiscBlocks.BlockPeat, (s, v) => s.AutoMapMarkers.MiscBlocks.BlockPeat = v),
            ["game:block-soil-compost-none"] = (s => s.AutoMapMarkers.MiscBlocks.BlockHighFertilitySoil, (s, v) => s.AutoMapMarkers.MiscBlocks.BlockHighFertilitySoil = v),
            ["game:block-meteorite-iron"] = (s => s.AutoMapMarkers.MiscBlocks.BlockMeteoriticIron, (s, v) => s.AutoMapMarkers.MiscBlocks.BlockMeteoriticIron = v),
            ["game:block-saltpeter-d"] = (s => s.AutoMapMarkers.MiscBlocks.BlockCoatingSaltpeter, (s, v) => s.AutoMapMarkers.MiscBlocks.BlockCoatingSaltpeter = v),
            ["egocarib-mapmarkers:raft-menu"] = (s => s.AutoMapMarkers.MiscBlocks.Raft, (s, v) => s.AutoMapMarkers.MiscBlocks.Raft = v),
            ["egocarib-mapmarkers:sailboat-menu"] = (s => s.AutoMapMarkers.MiscBlocks.Sailboat, (s, v) => s.AutoMapMarkers.MiscBlocks.Sailboat = v),

            // Traders
            ["egocarib-mapmarkers:trader-agriculture"] = (s => s.AutoMapMarkers.Traders.TraderAgriculture, (s, v) => s.AutoMapMarkers.Traders.TraderAgriculture = v),
            ["egocarib-mapmarkers:trader-artisan"] = (s => s.AutoMapMarkers.Traders.TraderArtisan, (s, v) => s.AutoMapMarkers.Traders.TraderArtisan = v),
            ["egocarib-mapmarkers:trader-buildmaterials"] = (s => s.AutoMapMarkers.Traders.TraderBuildingMaterials, (s, v) => s.AutoMapMarkers.Traders.TraderBuildingMaterials = v),
            ["egocarib-mapmarkers:trader-clothing"] = (s => s.AutoMapMarkers.Traders.TraderClothing, (s, v) => s.AutoMapMarkers.Traders.TraderClothing = v),
            ["egocarib-mapmarkers:trader-commodities"] = (s => s.AutoMapMarkers.Traders.TraderCommodities, (s, v) => s.AutoMapMarkers.Traders.TraderCommodities = v),
            ["egocarib-mapmarkers:trader-furniture"] = (s => s.AutoMapMarkers.Traders.TraderFurniture, (s, v) => s.AutoMapMarkers.Traders.TraderFurniture = v),
            ["egocarib-mapmarkers:trader-luxuries"] = (s => s.AutoMapMarkers.Traders.TraderLuxuries, (s, v) => s.AutoMapMarkers.Traders.TraderLuxuries = v),
            ["egocarib-mapmarkers:trader-survivalgoods"] = (s => s.AutoMapMarkers.Traders.TraderSurvivalGoods, (s, v) => s.AutoMapMarkers.Traders.TraderSurvivalGoods = v),
            ["egocarib-mapmarkers:trader-treasurehunter"] = (s => s.AutoMapMarkers.Traders.TraderTreasureHunter, (s, v) => s.AutoMapMarkers.Traders.TraderTreasureHunter = v),
        };

        /// <summary>
        /// Maps a definition label to the corresponding grouper field and sets the value.
        /// Falls back to the addon dictionary for unrecognized labels.
        /// </summary>
        private static void PopulateGrouperField(MapMarkerConfig.Settings settings, string label, AutoMapMarkerSetting value)
        {
            if (GrouperFieldMap.TryGetValue(label, out var accessor))
                accessor.Set(settings, value);
            else
                settings.AddonMarkerSettings[label] = value;
        }

        /// <summary>
        /// Gets the current AutoMapMarkerSetting from the grouper for a given label.
        /// Falls back to the addon dictionary for unrecognized labels.
        /// </summary>
        internal static AutoMapMarkerSetting GetGrouperField(MapMarkerConfig.Settings settings, string label)
        {
            if (GrouperFieldMap.TryGetValue(label, out var accessor))
                return accessor.Get(settings);
            return settings.AddonMarkerSettings.GetValueOrDefault(label);
        }

        #endregion
    }
}
