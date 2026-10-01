using System.Collections.Generic;
using Godot;
using Newtonsoft.Json.Linq;

namespace Top.Client.Rendering.Models.Gltf;

[Tool]
public partial class MaterialDescriptions : RefCounted
{
    private const string Key = "TOP_material_descriptions";

    public IReadOnlyList<JObject> Materials { get; private set; } = [];

    public static MaterialDescriptions Resolve(GltfState state)
    {
        if (state.GetAdditionalData(Key).As<MaterialDescriptions>() is { } known)
        {
            return known;
        }

        var resolved = new MaterialDescriptions();

        if (state.Json.TryGetValue("materials", out var described))
        {
            resolved.Materials = ToMaterials(described.AsGodotArray());
        }

        state.SetAdditionalData(Key, resolved);

        return resolved;
    }

    private static JObject[] ToMaterials(Godot.Collections.Array described)
    {
        var materials = new JObject[described.Count];

        for (var i = 0; i < materials.Length; i++)
        {
            materials[i] = ToJson(described[i]) as JObject ?? new JObject();
        }

        return materials;
    }

    private static JToken ToJson(Variant value)
    {
        return value.VariantType switch
        {
            Variant.Type.Dictionary => ToJson(value.AsGodotDictionary()),
            Variant.Type.Array => ToJson(value.AsGodotArray()),
            Variant.Type.Bool => new JValue(value.AsBool()),
            Variant.Type.Int => new JValue(value.AsInt64()),
            Variant.Type.Float => new JValue(value.AsDouble()),
            Variant.Type.String or Variant.Type.StringName => new JValue(value.AsString()),
            _ => JValue.CreateNull(),
        };
    }

    private static JObject ToJson(Godot.Collections.Dictionary members)
    {
        var described = new JObject();

        foreach (var member in members)
        {
            described[member.Key.AsString()] = ToJson(member.Value);
        }

        return described;
    }

    private static JArray ToJson(Godot.Collections.Array items)
    {
        var described = new JArray();

        foreach (var item in items)
        {
            described.Add(ToJson(item));
        }

        return described;
    }
}
