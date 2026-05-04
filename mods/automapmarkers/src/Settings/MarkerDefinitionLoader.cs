using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Egocarib.AutoMapMarkers.Utilities;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;


namespace Egocarib.AutoMapMarkers.Settings
{
    /// <summary>
    /// Loads and merges marker definition files from the MarkerDefinitions directory.
    /// _core.json is extracted from the embedded DLL resource on first run and is protected
    /// from being overridden by addon files.
    /// </summary>
    public static class MarkerDefinitionLoader
    {
        public const string DefinitionsFolder = "AutoMapMarkers/MarkerDefinitions";
        public const string CoreFileName = "_core.json";
        private const string EmbeddedResourceName = "automapmarkers.resources._core.json";

        /// <summary>
        /// Ensures the _core.json file exists in the MarkerDefinitions directory,
        /// extracting it from the embedded DLL resource if necessary.
        /// </summary>
        public static void EnsureCoreDefinitionsExist(string modConfigPath)
        {
            string definitionsDir = Path.Combine(modConfigPath, DefinitionsFolder);
            string coreFilePath = Path.Combine(definitionsDir, CoreFileName);

            if (!Directory.Exists(definitionsDir))
                Directory.CreateDirectory(definitionsDir);

            // Always overwrite _core.json from the embedded resource to pick up
            // new/changed definitions. User customizations belong in settings.json.
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(EmbeddedResourceName))
                {
                    if (stream == null)
                    {
                        MessageUtil.LogError($"Embedded resource '{EmbeddedResourceName}' not found in assembly.");
                        return;
                    }
                    using (var fileStream = File.Create(coreFilePath))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
                MessageUtil.Log($"Extracted {CoreFileName} to {definitionsDir}");
            }
            catch (Exception ex)
            {
                MessageUtil.LogError($"Failed to extract {CoreFileName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads all definition files and returns the merged list of category definitions.
        /// _core.json is loaded first and its entries are marked as protected.
        /// Other .json files are loaded alphabetically; among addon files, last loaded wins
        /// for overlapping labels. Core entries are never overridden.
        ///
        /// Asset-based addon discovery (via api.Assets.GetMany) only runs on the client side,
        /// since addon definitions are needed for GUI display and the asset system is reliably
        /// available on the client. The server only needs core + ModConfig definitions for
        /// populating grouper fields and sending default settings via protobuf.
        /// </summary>
        public static List<MarkerCategoryDef> LoadDefinitions(string modConfigPath, ICoreAPI api)
        {
            MessageUtil.Log($"Loading marker definitions ({api.Side})...");

            string definitionsDir = Path.Combine(modConfigPath, DefinitionsFolder);
            var mergedCategories = new Dictionary<MarkerCategory, MarkerCategoryDef>();

            // Load _core.json first
            string coreFilePath = Path.Combine(definitionsDir, CoreFileName);
            if (File.Exists(coreFilePath))
            {
                var coreCategories = LoadDefinitionFile(coreFilePath);
                if (coreCategories != null)
                {
                    foreach (var category in coreCategories)
                    {
                        foreach (var entry in category.Entries)
                        {
                            entry.IsCore = true;
                            entry.Source = CoreFileName;
                            if (entry.ExpandableEntries != null)
                            {
                                foreach (var subEntry in entry.ExpandableEntries)
                                {
                                    subEntry.IsCore = true;
                                    subEntry.Source = CoreFileName;
                                }
                            }
                        }
                        mergedCategories[category.Category] = category;
                    }
                }
            }
            else
            {
                MessageUtil.LogError($"    Core definitions file not found at {coreFilePath}");
            }

            // Load asset-based addon definitions from all mods (client side only)
            if (api.Side == EnumAppSide.Client)
            {
                LoadAssetBasedAddons(api, mergedCategories);
            }

            // Load ModConfig addon files alphabetically (excluding _core.json)
            if (Directory.Exists(definitionsDir))
            {
                var addonFiles = Directory.GetFiles(definitionsDir, "*.json")
                    .Where(f => !Path.GetFileName(f).Equals(CoreFileName, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase);

                foreach (var addonFile in addonFiles)
                {
                    var addonCategories = LoadDefinitionFile(addonFile);
                    if (addonCategories == null) continue;

                    MergeAddonCategories(addonCategories, mergedCategories, Path.GetFileName(addonFile));
                }
            }

            var result = mergedCategories.Values.ToList();
            int totalEntries = result.Sum(c => c.Entries.Count);
            MessageUtil.Log($"Marker definitions loaded: {result.Count} categories, {totalEntries} entries.");
            return result;
        }


        /// <summary>
        /// Discovers and loads marker definition files from all mods' asset folders.
        /// Any mod can place JSON files at assets/&lt;modid&gt;/config/automapmarkers/ to add markers.
        /// </summary>
        private static void LoadAssetBasedAddons(ICoreAPI api, Dictionary<MarkerCategory, MarkerCategoryDef> mergedCategories)
        {
            List<IAsset> addonAssets;
            try
            {
                addonAssets = api.Assets.GetMany("config/automapmarkers/", null, loadAsset: true);
            }
            catch (Exception ex)
            {
                MessageUtil.LogError($"    Failed to search for asset-based addon definitions: {ex.Message}");
                return;
            }

            if (addonAssets == null || addonAssets.Count == 0)
                return;

            // Sort by asset location for deterministic load order
            var sortedAssets = addonAssets.OrderBy(a => a.Location.ToString(), StringComparer.OrdinalIgnoreCase);

            foreach (var asset in sortedAssets)
            {
                string assetPath = asset.Location.ToString();

                // Only process .json files
                if (!assetPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    continue;

                var addonCategories = LoadDefinitionJson(asset.ToText(), assetPath);
                if (addonCategories == null) continue;

                MergeAddonCategories(addonCategories, mergedCategories, assetPath);
                MessageUtil.Log($"    Loaded asset-based addon definitions from '{assetPath}'");
            }
        }

        /// <summary>
        /// Merges addon categories into the merged dictionary. Conflict detection against core
        /// is deferred to <see cref="RunDeferredConflictCheck"/>, which runs after blocks/entities
        /// are registered.
        /// </summary>
        private static void MergeAddonCategories(List<MarkerCategoryDef> addonCategories, Dictionary<MarkerCategory, MarkerCategoryDef> mergedCategories, string sourceName)
        {
            foreach (var category in addonCategories)
            {
                if (mergedCategories.TryGetValue(category.Category, out var existing))
                {
                    var existingLabels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < existing.Entries.Count; i++)
                        existingLabels[existing.Entries[i].Label] = i;

                    foreach (var entry in category.Entries)
                    {
                        entry.Source = sourceName;

                        if (existingLabels.TryGetValue(entry.Label, out int idx))
                        {
                            if (!existing.Entries[idx].IsCore)
                            {
                                MessageUtil.Log($"    Entry '{entry.Label}' from '{sourceName}' overrides entry from '{existing.Entries[idx].Source}' in category '{category.Category}'.");
                                existing.Entries[idx] = entry;
                            }
                        }
                        else
                        {
                            existing.Entries.Add(entry);
                            existingLabels[entry.Label] = existing.Entries.Count - 1;
                        }
                    }
                }
                else
                {
                    foreach (var e in category.Entries)
                        e.Source = sourceName;
                    mergedCategories[category.Category] = category;
                }

                // Process patches
                if (category.Patches != null)
                {
                    foreach (var patch in category.Patches)
                    {
                        ApplyPatch(patch, mergedCategories, category.Category, sourceName);
                    }
                }
            }
        }

        /// <summary>
        /// Runs after blocks/entities are loaded. For each non-core entry, checks whether any of its
        /// AssetPaths matches a real block or entity code that a core pattern also matches. If so,
        /// removes the entry and logs the conflict. Patterns that overlap only theoretically (no
        /// actual loaded asset matches both) are left alone.
        /// </summary>
        public static void RunDeferredConflictCheck(ICoreClientAPI api, List<MarkerCategoryDef> mergedCategories)
        {
            if (api == null || mergedCategories == null) return;

            var coreMatchers = CollectCoreMatchers(mergedCategories);
            if (coreMatchers.Count == 0) return;

            // Build per-entry checks for every non-core entry up front, so the single pass over
            // real block/entity codes can apply all of them in one go.
            var addonChecks = new List<AddonEntryCheck>();
            foreach (var category in mergedCategories)
            {
                foreach (var entry in category.Entries)
                {
                    if (entry.IsCore) continue;
                    var check = AddonEntryCheck.Build(entry, category);
                    if (check != null)
                        addonChecks.Add(check);
                }
            }
            if (addonChecks.Count == 0) return;

            MessageUtil.Log($"Running deferred conflict check ({addonChecks.Count} addon entries vs {coreMatchers.Count} core patterns)...");
            var stopwatch = Stopwatch.StartNew();
            int blockCount = 0;
            int entityCount = 0;

            // Single pass: iterate the game's loaded block and entity registries directly,
            // testing each code against every addon entry.
            if (api.World?.Blocks != null)
            {
                foreach (var block in api.World.Blocks)
                {
                    var code = block?.Code?.Path;
                    if (string.IsNullOrEmpty(code)) continue;
                    blockCount++;
                    TestCodeAgainstAddons(code, addonChecks, coreMatchers);
                }
            }
            if (api.World?.EntityTypes != null)
            {
                foreach (var entityType in api.World.EntityTypes)
                {
                    var code = entityType?.Code?.Path;
                    if (string.IsNullOrEmpty(code)) continue;
                    entityCount++;
                    TestCodeAgainstAddons(code, addonChecks, coreMatchers);
                }
            }

            // Apply pruning + logging.
            int conflictingEntries = 0;
            foreach (var check in addonChecks)
            {
                if (check.Conflicts.Count == 0) continue;
                conflictingEntries++;
                var details = string.Join(", ", check.Conflicts.Select(c => $"'{c.addonPath}' overlaps core '{c.corePath}' (e.g. '{c.exampleCode}')"));
                MessageUtil.Log($"    Addon '{check.Entry.Source}' entry '{check.Entry.Label}' conflicts with core asset paths - skipped. Conflicts: {details}");
                check.Category.Entries.Remove(check.Entry);
            }

            stopwatch.Stop();
            MessageUtil.Log($"Deferred conflict check finished in {stopwatch.Elapsed.TotalMilliseconds:F2}ms (scanned {blockCount} blocks + {entityCount} entities, {conflictingEntries} addon entries pruned).");
        }

        private static void TestCodeAgainstAddons(
            string code,
            List<AddonEntryCheck> addonChecks,
            List<(string pattern, AssetPatternMatcher matcher)> coreMatchers)
        {
            foreach (var check in addonChecks)
            {
                foreach (var (addonPattern, addonMatcher) in check.AddonMatchers)
                {
                    if (!addonMatcher.Matches(code)) continue;

                    // Skip if addon's own excludes filter this code out.
                    bool excluded = false;
                    foreach (var ex in check.ExcludeMatchers)
                    {
                        if (ex.Matches(code)) { excluded = true; break; }
                    }
                    if (excluded) continue;

                    // Find the first core pattern that also matches this code.
                    foreach (var (corePattern, coreMatcher) in coreMatchers)
                    {
                        if (!coreMatcher.Matches(code)) continue;
                        if (check.ReportedPairs.Add((addonPattern, corePattern)))
                            check.Conflicts.Add((addonPattern, corePattern, code));
                        break; // one example per (addon, core) pair is enough
                    }
                }
            }
        }

        private static List<(string pattern, AssetPatternMatcher matcher)> CollectCoreMatchers(List<MarkerCategoryDef> mergedCategories)
        {
            var list = new List<(string, AssetPatternMatcher)>();
            foreach (var category in mergedCategories)
            {
                foreach (var entry in category.Entries)
                {
                    if (!entry.IsCore) continue;
                    AddPatternsFromEntry(entry, list);
                    if (entry.ExpandableEntries != null)
                    {
                        foreach (var sub in entry.ExpandableEntries)
                        {
                            if (sub.IsCore)
                                AddPatternsFromEntry(sub, list);
                        }
                    }
                }
            }
            return list;
        }

        private static void AddPatternsFromEntry(MarkerEntryDef entry, List<(string, AssetPatternMatcher)> list)
        {
            if (entry.AssetPaths == null) return;
            foreach (var pattern in entry.AssetPaths)
            {
                if (string.IsNullOrEmpty(pattern)) continue;
                if (pattern[0] == '*') continue; // matches existing skip-leading-star behavior in registry build
                list.Add((pattern, new AssetPatternMatcher(pattern)));
            }
        }

        /// <summary>
        /// Per-entry compiled state for the deferred conflict check. Built once per non-core entry
        /// before the single pass over real block/entity codes.
        /// </summary>
        private class AddonEntryCheck
        {
            public MarkerEntryDef Entry;
            public MarkerCategoryDef Category;
            public List<(string pattern, AssetPatternMatcher matcher)> AddonMatchers;
            public List<AssetPatternMatcher> ExcludeMatchers;
            public HashSet<(string addonPattern, string corePattern)> ReportedPairs = new HashSet<(string, string)>();
            public List<(string addonPath, string corePath, string exampleCode)> Conflicts = new List<(string, string, string)>();

            public static AddonEntryCheck Build(MarkerEntryDef entry, MarkerCategoryDef category)
            {
                if (entry.AssetPaths == null || entry.AssetPaths.Count == 0) return null;

                var addonMatchers = new List<(string, AssetPatternMatcher)>();
                foreach (var p in entry.AssetPaths)
                {
                    if (string.IsNullOrEmpty(p) || p[0] == '*') continue;
                    addonMatchers.Add((p, new AssetPatternMatcher(p)));
                }
                if (addonMatchers.Count == 0) return null;

                var excludeMatchers = new List<AssetPatternMatcher>();
                if (entry.ExcludePaths != null)
                {
                    foreach (var ex in entry.ExcludePaths)
                    {
                        if (!string.IsNullOrEmpty(ex))
                            excludeMatchers.Add(new AssetPatternMatcher(ex));
                    }
                }

                return new AddonEntryCheck
                {
                    Entry = entry,
                    Category = category,
                    AddonMatchers = addonMatchers,
                    ExcludeMatchers = excludeMatchers,
                };
            }
        }

        /// <summary>
        /// Applies a single patch to an existing entry within the specified category.
        /// Patches are strictly additive — they can add asset paths and expandable sub-entries.
        /// </summary>
        private static void ApplyPatch(MarkerPatchDef patch, Dictionary<MarkerCategory, MarkerCategoryDef> mergedCategories, MarkerCategory categoryKey, string sourceName)
        {
            if (string.IsNullOrEmpty(patch.TargetLabel))
            {
                MessageUtil.LogError($"    Patch from '{sourceName}' has no Target — skipped.");
                return;
            }

            if (!mergedCategories.TryGetValue(categoryKey, out var category))
            {
                MessageUtil.LogError($"    Patch from '{sourceName}' targets category '{categoryKey}' which doesn't exist — skipped.");
                return;
            }

            var (target, isSubEntry) = FindEntryByLabel(category, patch.TargetLabel);
            if (target == null)
            {
                MessageUtil.LogError($"    Patch from '{sourceName}' targets '{patch.TargetLabel}' which was not found in category '{categoryKey}' — skipped.");
                return;
            }

            // Add asset paths (allowed for both top-level and sub-entries)
            if (patch.AddAssetPaths != null)
            {
                foreach (var path in patch.AddAssetPaths)
                {
                    if (!target.AssetPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
                    {
                        target.AssetPaths.Add(path);
                        MessageUtil.Log($"    Patch from '{sourceName}': added asset path '{path}' to '{patch.TargetLabel}'");
                    }
                }
            }

            // Add expandable sub-entries (only allowed for top-level entries — no nesting)
            if (patch.AddExpandableEntries != null && patch.AddExpandableEntries.Count > 0)
            {
                if (isSubEntry)
                {
                    MessageUtil.LogError($"    Patch from '{sourceName}': cannot add expandable entries to sub-entry '{patch.TargetLabel}' — nesting not supported. Skipped.");
                }
                else
                {
                    if (target.ExpandableEntries == null)
                        target.ExpandableEntries = new List<MarkerEntryDef>();

                    var existingLabels = new HashSet<string>(
                        target.ExpandableEntries.Select(e => e.Label),
                        StringComparer.OrdinalIgnoreCase);

                    foreach (var subEntry in patch.AddExpandableEntries)
                    {
                        if (existingLabels.Contains(subEntry.Label))
                        {
                            MessageUtil.Log($"    Patch from '{sourceName}': sub-entry '{subEntry.Label}' already exists in '{patch.TargetLabel}' — skipped.");
                            continue;
                        }
                        subEntry.Source = sourceName;
                        subEntry.IsCore = false;
                        subEntry.ExpandableEntries = null;  // Strip any nested expandable entries
                        target.ExpandableEntries.Add(subEntry);
                        existingLabels.Add(subEntry.Label);
                        MessageUtil.Log($"    Patch from '{sourceName}': added sub-entry '{subEntry.Label}' to '{patch.TargetLabel}'");
                    }
                }
            }
        }

        /// <summary>
        /// Searches a category's entries and their sub-entries for an entry with the given label.
        /// Returns the entry and whether it was found as a sub-entry (inside ExpandableEntries).
        /// </summary>
        private static (MarkerEntryDef entry, bool isSubEntry) FindEntryByLabel(MarkerCategoryDef category, string label)
        {
            foreach (var entry in category.Entries)
            {
                if (string.Equals(entry.Label, label, StringComparison.OrdinalIgnoreCase))
                    return (entry, false);
                if (entry.ExpandableEntries != null)
                {
                    foreach (var sub in entry.ExpandableEntries)
                    {
                        if (string.Equals(sub.Label, label, StringComparison.OrdinalIgnoreCase))
                            return (sub, true);
                    }
                }
            }
            return (null, false);
        }

        /// <summary>
        /// Parses a JSON string into a list of MarkerCategoryDef objects.
        /// </summary>
        private static List<MarkerCategoryDef> LoadDefinitionJson(string json, string sourceName)
        {
            try
            {
                return JsonConvert.DeserializeObject<List<MarkerCategoryDef>>(json);
            }
            catch (Exception ex)
            {
                MessageUtil.LogError($"    Failed to parse definition from '{sourceName}': {ex.Message}");
                return null;
            }
        }

        private static List<MarkerCategoryDef> LoadDefinitionFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<MarkerCategoryDef>>(json);
            }
            catch (Exception ex)
            {
                MessageUtil.LogError($"    Failed to load definition file '{Path.GetFileName(filePath)}': {ex.Message}");
                return null;
            }
        }
    }
}
