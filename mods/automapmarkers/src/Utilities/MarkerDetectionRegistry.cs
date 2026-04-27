using System;
using System.Collections.Generic;
using Egocarib.AutoMapMarkers.Settings;
using static Egocarib.AutoMapMarkers.Settings.MapMarkerConfig.Settings;

namespace Egocarib.AutoMapMarkers.Utilities
{
    /// <summary>
    /// Definition-driven detection registry. Built from loaded marker definitions and settings,
    /// provides efficient asset path matching to resolve blocks/entities to their marker settings.
    /// </summary>
    public class MarkerDetectionRegistry
    {
        private readonly List<RegistryEntry> _entries;
        private readonly HashSet<char> _prefixChars;

        private MarkerDetectionRegistry(List<RegistryEntry> entries, HashSet<char> prefixChars)
        {
            _entries = entries;
            _prefixChars = prefixChars;
        }

        /// <summary>
        /// Result of a successful match against the registry.
        /// </summary>
        public class MatchResult
        {
            public string Label { get; }
            public MarkerCategory Category { get; }
            public AutoMapMarkerSetting Setting { get; }
            public bool DynamicTitle { get; }

            public MatchResult(string label, MarkerCategory category, AutoMapMarkerSetting setting, bool dynamicTitle)
            {
                Label = label;
                Category = category;
                Setting = setting;
                DynamicTitle = dynamicTitle;
            }
        }

        private class RegistryEntry
        {
            public string Label;
            public MarkerCategory Category;
            public AssetPatternMatcher[] IncludeMatchers;
            public AssetPatternMatcher[] ExcludeMatchers;
            public bool DynamicTitle;
            public AutoMapMarkerSetting Setting;
        }

        /// <summary>
        /// Builds a registry from all loaded definitions and the current settings.
        /// Entries with empty AssetPaths are skipped (e.g. Raft/Sailboat).
        /// Patterns starting with * are skipped with a warning.
        /// </summary>
        public static MarkerDetectionRegistry Build(
            List<MarkerCategoryDef> definitions,
            MapMarkerConfig.Settings settings)
        {
            var entries = new List<RegistryEntry>();
            var prefixChars = new HashSet<char>();

            foreach (var category in definitions)
            {
                foreach (var entryDef in category.Entries)
                {
                    if (entryDef.ExpandableEntries != null && entryDef.ExpandableEntries.Count > 0)
                    {
                        if (settings.IsExpanded(entryDef))
                        {
                            // Expanded: register each sub-entry individually
                            foreach (var subDef in entryDef.ExpandableEntries)
                                RegisterEntry(subDef, category.Category, settings, entries, prefixChars, entryDef.TriggerType);
                        }
                        else
                        {
                            // Collapsed: register parent only (broad pattern)
                            RegisterEntry(entryDef, category.Category, settings, entries, prefixChars, MarkerTriggerType.Default);
                        }
                        continue;
                    }

                    RegisterEntry(entryDef, category.Category, settings, entries, prefixChars, MarkerTriggerType.Default);
                }
            }

            return new MarkerDetectionRegistry(entries, prefixChars);
        }

        private static void RegisterEntry(MarkerEntryDef entryDef, MarkerCategory category,
            MapMarkerConfig.Settings settings, List<RegistryEntry> entries, HashSet<char> prefixChars,
            MarkerTriggerType parentTriggerType)
        {
            // Sub-entries inherit the parent's TriggerType when left at the schema default.
            var effectiveTrigger = entryDef.TriggerType != MarkerTriggerType.Default
                ? entryDef.TriggerType
                : parentTriggerType;
            if (effectiveTrigger != MarkerTriggerType.Default)
                return;

            if (entryDef.AssetPaths == null || entryDef.AssetPaths.Count == 0)
                return;

            var setting = MarkerSettingsPersistence.GetGrouperField(settings, entryDef.Label);
            if (setting == null)
                return;

            var includeMatchers = new List<AssetPatternMatcher>();
            foreach (var pattern in entryDef.AssetPaths)
            {
                if (string.IsNullOrEmpty(pattern)) continue;

                if (pattern[0] == '*')
                {
                    MessageUtil.Log($"Skipping pattern '{pattern}' for entry '{entryDef.Label}': patterns starting with * are not supported.");
                    continue;
                }

                var matcher = new AssetPatternMatcher(pattern);
                includeMatchers.Add(matcher);
                prefixChars.Add(pattern[0]);
            }

            if (includeMatchers.Count == 0)
                return;

            var excludeMatchers = new List<AssetPatternMatcher>();
            if (entryDef.ExcludePaths != null)
            {
                foreach (var pattern in entryDef.ExcludePaths)
                {
                    if (!string.IsNullOrEmpty(pattern))
                        excludeMatchers.Add(new AssetPatternMatcher(pattern));
                }
            }

            entries.Add(new RegistryEntry
            {
                Label = entryDef.Label,
                Category = category,
                IncludeMatchers = includeMatchers.ToArray(),
                ExcludeMatchers = excludeMatchers.ToArray(),
                DynamicTitle = entryDef.DynamicTitle,
                Setting = setting
            });
        }

        /// <summary>
        /// Returns all matching entries for the given asset path.
        /// Needed for cases like mushrooms where both safe and unsafe entries share the same pattern.
        /// </summary>
        public List<MatchResult> FindMatches(string assetPath)
        {
            var results = new List<MatchResult>();
            if (string.IsNullOrEmpty(assetPath)) return results;

            // Fast prefix miss: if no entry's pattern starts with this character, skip entirely
            if (!_prefixChars.Contains(assetPath[0]))
                return results;

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (MatchesEntry(entry, assetPath))
                    results.Add(new MatchResult(entry.Label, entry.Category, entry.Setting, entry.DynamicTitle));
            }

            return results;
        }

        /// <summary>
        /// Convenience method that returns the first match, or null if no match.
        /// </summary>
        public MatchResult TryMatch(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;

            if (!_prefixChars.Contains(assetPath[0]))
                return null;

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (MatchesEntry(entry, assetPath))
                    return new MatchResult(entry.Label, entry.Category, entry.Setting, entry.DynamicTitle);
            }

            return null;
        }

        private static bool MatchesEntry(RegistryEntry entry, string assetPath)
        {
            // Check excludes first
            for (int j = 0; j < entry.ExcludeMatchers.Length; j++)
            {
                if (entry.ExcludeMatchers[j].Matches(assetPath))
                    return false;
            }

            // Check includes
            for (int j = 0; j < entry.IncludeMatchers.Length; j++)
            {
                if (entry.IncludeMatchers[j].Matches(assetPath))
                    return true;
            }

            return false;
        }
    }
}
