using System.Collections.Generic;
using Egocarib.AutoMapMarkers.Settings;
using static Egocarib.AutoMapMarkers.Settings.MapMarkerConfig.Settings;

namespace Egocarib.AutoMapMarkers.Utilities
{
    /// <summary>
    /// Detection registry consulted by the boat Harmony patches on mount/dismount to look up the
    /// right marker setting for a boat-like entity. Built from the same <c>AssetPaths</c> field as
    /// <see cref="MarkerDetectionRegistry"/> — the only difference between the two is the trigger
    /// context. Trader/block patterns will be evaluated here too, but they harmlessly fail to
    /// match any boat entity code.
    /// </summary>
    public class BoatDetectionRegistry
    {
        private readonly List<RegistryEntry> _entries;

        private BoatDetectionRegistry(List<RegistryEntry> entries)
        {
            _entries = entries;
        }

        public class MatchResult
        {
            public string Label { get; }
            public AutoMapMarkerSetting Setting { get; }

            public MatchResult(string label, AutoMapMarkerSetting setting)
            {
                Label = label;
                Setting = setting;
            }
        }

        private class RegistryEntry
        {
            public string Label;
            public AssetPatternMatcher[] Matchers;
            public AutoMapMarkerSetting Setting;
        }

        public static BoatDetectionRegistry Build(
            List<MarkerCategoryDef> definitions,
            MapMarkerConfig.Settings settings)
        {
            var entries = new List<RegistryEntry>();

            foreach (var category in definitions)
            {
                foreach (var entryDef in category.Entries)
                {
                    RegisterEntry(entryDef, settings, entries);
                    if (entryDef.ExpandableEntries != null)
                    {
                        foreach (var sub in entryDef.ExpandableEntries)
                            RegisterEntry(sub, settings, entries);
                    }
                }
            }

            return new BoatDetectionRegistry(entries);
        }

        private static void RegisterEntry(MarkerEntryDef entryDef, MapMarkerConfig.Settings settings, List<RegistryEntry> entries)
        {
            if (entryDef.AssetPaths == null || entryDef.AssetPaths.Count == 0)
                return;

            var setting = MarkerSettingsPersistence.GetGrouperField(settings, entryDef.Label);
            if (setting == null)
                return;

            var matchers = new List<AssetPatternMatcher>();
            foreach (var pattern in entryDef.AssetPaths)
            {
                if (string.IsNullOrEmpty(pattern)) continue;
                matchers.Add(new AssetPatternMatcher(pattern));
            }
            if (matchers.Count == 0)
                return;

            entries.Add(new RegistryEntry
            {
                Label = entryDef.Label,
                Matchers = matchers.ToArray(),
                Setting = setting
            });
        }

        public MatchResult TryMatch(string entityCodePath)
        {
            if (string.IsNullOrEmpty(entityCodePath)) return null;

            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                for (int j = 0; j < e.Matchers.Length; j++)
                {
                    if (e.Matchers[j].Matches(entityCodePath))
                        return new MatchResult(e.Label, e.Setting);
                }
            }

            return null;
        }
    }
}
