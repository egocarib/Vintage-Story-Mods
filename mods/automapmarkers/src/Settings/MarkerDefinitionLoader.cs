using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Egocarib.AutoMapMarkers.Utilities;
using Newtonsoft.Json;
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
            var coreAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
                            foreach (var assetPath in entry.AssetPaths)
                                coreAssetPaths.Add(assetPath);
                            if (entry.ExpandableEntries != null)
                            {
                                foreach (var subEntry in entry.ExpandableEntries)
                                {
                                    subEntry.IsCore = true;
                                    subEntry.Source = CoreFileName;
                                    foreach (var ap in subEntry.AssetPaths)
                                        coreAssetPaths.Add(ap);
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
                LoadAssetBasedAddons(api, mergedCategories, coreAssetPaths);
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

                    MergeAddonCategories(addonCategories, mergedCategories, coreAssetPaths, Path.GetFileName(addonFile));
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
        private static void LoadAssetBasedAddons(ICoreAPI api, Dictionary<MarkerCategory, MarkerCategoryDef> mergedCategories, HashSet<string> coreAssetPaths)
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

                MergeAddonCategories(addonCategories, mergedCategories, coreAssetPaths, assetPath);
                MessageUtil.Log($"    Loaded asset-based addon definitions from '{assetPath}'");
            }
        }

        /// <summary>
        /// Merges addon categories into the merged dictionary, protecting core entries.
        /// </summary>
        private static void MergeAddonCategories(List<MarkerCategoryDef> addonCategories, Dictionary<MarkerCategory, MarkerCategoryDef> mergedCategories, HashSet<string> coreAssetPaths, string sourceName)
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

                        var conflicts = FindCoreAssetPathConflicts(entry, coreAssetPaths);
                        if (conflicts.Count > 0)
                        {
                            var details = string.Join(", ", conflicts.Select(c => $"'{c.addonPath}' overlaps core '{c.corePath}'"));
                            MessageUtil.Log($"    Addon '{sourceName}' entry '{entry.Label}' conflicts with core asset paths - skipped. Conflicts: {details}");
                            continue;
                        }

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
                    category.Entries = category.Entries
                        .Where(e =>
                        {
                            var conflicts = FindCoreAssetPathConflicts(e, coreAssetPaths);
                            if (conflicts.Count > 0)
                            {
                                var details = string.Join(", ", conflicts.Select(c => $"'{c.addonPath}' overlaps core '{c.corePath}'"));
                                MessageUtil.Log($"    Addon '{sourceName}' entry '{e.Label}' conflicts with core asset paths - skipped. Conflicts: {details}");
                                return false;
                            }
                            e.Source = sourceName;
                            return true;
                        })
                        .ToList();
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
        /// Finds conflicting asset path pairs between an entry and core-protected asset paths.
        /// Uses wildcard-aware overlap detection so that e.g. "log-resin-oak" conflicts with
        /// core's "log-resin-*", and "log-*" also conflicts.
        /// Returns a list of (addonPath, corePath) pairs that conflict, empty if no conflicts.
        /// </summary>
        private static List<(string addonPath, string corePath)> FindCoreAssetPathConflicts(MarkerEntryDef entry, HashSet<string> coreAssetPaths)
        {
            var conflicts = new List<(string, string)>();
            foreach (var addonPath in entry.AssetPaths)
            {
                foreach (var corePath in coreAssetPaths)
                {
                    if (GlobsOverlap(addonPath, corePath))
                        conflicts.Add((addonPath, corePath));
                }
            }
            return conflicts;
        }

        /// <summary>
        /// Determines whether two glob patterns (using * as wildcard) could match any common string.
        /// Walks both patterns simultaneously — a * on either side can consume characters from the other.
        /// </summary>
        private static bool GlobsOverlap(string a, string b, int ai = 0, int bi = 0)
        {
            while (ai < a.Length && bi < b.Length)
            {
                if (a[ai] == '*')
                {
                    // '*' matches zero or more characters: try skipping it, or consuming one char from b
                    return GlobsOverlap(a, b, ai + 1, bi) || GlobsOverlap(a, b, ai, bi + 1);
                }
                if (b[bi] == '*')
                {
                    return GlobsOverlap(a, b, ai, bi + 1) || GlobsOverlap(a, b, ai + 1, bi);
                }
                if (a[ai] != b[bi])
                    return false;
                ai++;
                bi++;
            }
            // Consume any trailing *s on either side
            while (ai < a.Length && a[ai] == '*') ai++;
            while (bi < b.Length && b[bi] == '*') bi++;
            return ai == a.Length && bi == b.Length;
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
