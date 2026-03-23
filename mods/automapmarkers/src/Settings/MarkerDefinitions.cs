using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Egocarib.AutoMapMarkers.Settings
{
    /// <summary>
    /// The fixed set of marker categories. Each category maps to a GUI tab.
    /// JSON values are case-insensitive: "Flora", "Surface Ore", "Deep Ore", "Other".
    /// </summary>
    [JsonConverter(typeof(MarkerCategoryConverter))]
    public enum MarkerCategory
    {
        Flora,
        SurfaceOre,
        DeepOre,
        Other
    }

    /// <summary>
    /// Case-insensitive JSON converter for <see cref="MarkerCategory"/>.
    /// Accepts "Surface Ore" or "SurfaceOre" (and similar for DeepOre).
    /// </summary>
    public class MarkerCategoryConverter : JsonConverter<MarkerCategory>
    {
        private static readonly Dictionary<string, MarkerCategory> NameMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Flora"] = MarkerCategory.Flora,
                ["Surface Ore"] = MarkerCategory.SurfaceOre,
                ["SurfaceOre"] = MarkerCategory.SurfaceOre,
                ["Deep Ore"] = MarkerCategory.DeepOre,
                ["DeepOre"] = MarkerCategory.DeepOre,
                ["Other"] = MarkerCategory.Other,
            };

        public override MarkerCategory ReadJson(JsonReader reader, Type objectType,
            MarkerCategory existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string value = reader.Value?.ToString();
            if (value != null && NameMap.TryGetValue(value, out var result))
                return result;
            throw new JsonSerializationException(
                $"Unknown Category '{value}'. Valid values: Flora, Surface Ore, Deep Ore, Other");
        }

        public override void WriteJson(JsonWriter writer, MarkerCategory value, JsonSerializer serializer)
        {
            writer.WriteValue(value switch
            {
                MarkerCategory.SurfaceOre => "Surface Ore",
                MarkerCategory.DeepOre => "Deep Ore",
                MarkerCategory.Other => "Other",
                _ => value.ToString()
            });
        }
    }

    /// <summary>
    /// POCO classes for deserializing marker definition JSON files.
    /// These are never sent over the wire (no ProtoContract).
    /// </summary>
    public class MarkerCategoryDef
    {
        public MarkerCategory Category { get; set; }
        public List<MarkerEntryDef> Entries { get; set; } = new List<MarkerEntryDef>();
        public List<MarkerPatchDef> Patches { get; set; }
    }

    public class MarkerPatchDef
    {
        public string TargetLabel { get; set; }
        public List<string> AddAssetPaths { get; set; }
        public List<MarkerEntryDef> AddExpandableEntries { get; set; }
    }

    public class MarkerEntryDef
    {
        public string Label { get; set; }
        public List<string> AssetPaths { get; set; } = new List<string>();
        public List<string> ExcludePaths { get; set; } = new List<string>();
        public bool DynamicTitle { get; set; } = false;
        public MarkerDefaultsDef Defaults { get; set; } = new MarkerDefaultsDef();
        public List<MarkerEntryDef> ExpandableEntries { get; set; }
        public bool DefaultExpanded { get; set; } = false;
        /// <summary>Whether this entry came from the protected _core.json file.</summary>
        internal bool IsCore { get; set; } = false;
        /// <summary>The source that provided this entry (e.g. "_core.json", a mod asset path, or a ModConfig filename).</summary>
        internal string Source { get; set; }
    }

    public class MarkerDefaultsDef
    {
        public bool Enabled { get; set; } = false;
        public string Title { get; set; }
        public string Icon { get; set; } = "circle";
        public string Color { get; set; } = "black";
        public bool Pinned { get; set; } = false;
        public int CoverageRadius { get; set; } = 1;
    }
}
