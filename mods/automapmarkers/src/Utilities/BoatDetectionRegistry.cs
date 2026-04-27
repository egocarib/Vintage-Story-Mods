using System.Collections.Generic;
using Egocarib.AutoMapMarkers.Settings;
using static Egocarib.AutoMapMarkers.Settings.MapMarkerConfig.Settings;

namespace Egocarib.AutoMapMarkers.Utilities
{
    /// <summary>
    /// Detection registry consulted by the boat Harmony patches on mount/dismount. Registers
    /// only entries whose <see cref="MarkerEntryDef.TriggerType"/> is <c>Mount</c>; all other
    /// entries are owned by <see cref="MarkerDetectionRegistry"/>. The Harmony hooks fire on
    /// any mountable entity (boats today, pack animals or other mountable creatures via
    /// addon-defined Mount entries), so the registry is not boat-specific despite the name.
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
                    RegisterEntry(entryDef, settings, entries, MarkerTriggerType.Default);
                    if (entryDef.ExpandableEntries != null)
                    {
                        foreach (var sub in entryDef.ExpandableEntries)
                            RegisterEntry(sub, settings, entries, entryDef.TriggerType);
                    }
                }
            }

            return new BoatDetectionRegistry(entries);
        }

        private static void RegisterEntry(MarkerEntryDef entryDef, MapMarkerConfig.Settings settings,
            List<RegistryEntry> entries, MarkerTriggerType parentTriggerType)
        {
            // Sub-entries inherit the parent's TriggerType when left at the schema default.
            var effectiveTrigger = entryDef.TriggerType != MarkerTriggerType.Default
                ? entryDef.TriggerType
                : parentTriggerType;
            if (effectiveTrigger != MarkerTriggerType.Mount)
                return;

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
