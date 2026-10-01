using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Newtonsoft.Json.Linq;
using Top.Client.Rendering.Models.Animations;
using Top.Client.Rendering.Models.Materials;
using Top.Contracts.Assets.Models.Extras;
using Top.Logging;

namespace Top.Client.Rendering.Models.Gltf;

[Tool]
public partial class TopMaterialExtension : GltfDocumentExtension
{
    public override Error _ImportPostParse(GltfState state)
    {
        var descriptions = MaterialDescriptions.Resolve(state).Materials;

        if (descriptions.Count == 0)
        {
            return Error.Ok;
        }

        GenerateMipmaps(state);

        var parsed = state.Materials;
        var replacements = new Array<Material>();

        for (var i = 0; i < parsed.Count; i++)
        {
            var description = i < descriptions.Count ? descriptions[i] : new JObject();
            var material = ModelShaderBuilder.CreateMaterial(
                RenderStateReader.Read(description),
                ReadBaseColorTexture(state, description));

            material.ResourceName = parsed[i].ResourceName;
            replacements.Add(material);
        }

        ReplaceSurfaceMaterials(state, parsed, replacements);
        state.Materials = replacements;

        return Error.Ok;
    }

    public override Error _ImportPost(GltfState state, Node root)
    {
        TopImportMarker.Stamp(root);

        MaterialTracks.AddPlayers(root, state.Materials,
            ReadExtras(MaterialDescriptions.Resolve(state).Materials),
            index => ResolveTexture(state, index));

        return Error.Ok;
    }

    private static IReadOnlyList<MaterialExtras?> ReadExtras(IReadOnlyList<JObject> descriptions)
    {
        var extras = new MaterialExtras?[descriptions.Count];

        for (var i = 0; i < extras.Length; i++)
        {
            extras[i] = RenderStateReader.TryReadExtras(descriptions[i], out var read) ? read : null;
        }

        return extras;
    }

    private static void GenerateMipmaps(GltfState state)
    {
        var images = state.Images;

        for (var i = 0; i < images.Count; i++)
        {
            if (images[i]?.GetImage() is not { } image || image.HasMipmaps())
            {
                continue;
            }

            if (image.IsCompressed())
            {
                Log.Warning(
                    $"{state.GetSceneName()}: image {i} '{images[i].ResourceName}' is compressed and has no mipmaps");

                continue;
            }

            if (image.GenerateMipmaps() is var error and not Error.Ok)
            {
                Log.Warning($"{state.GetSceneName()}: image {i} '{images[i].ResourceName}' has no mipmaps: {error}");

                continue;
            }

            var texture = ImageTexture.CreateFromImage(image);

            texture.ResourceName = images[i].ResourceName;
            images[i] = texture;
        }

        state.Images = images;
    }

    private static void ReplaceSurfaceMaterials(GltfState state, Array<Material> parsed,
        Array<Material> replacements)
    {
        foreach (var mesh in state.Meshes)
        {
            var surfaces = mesh.Mesh;

            for (var surface = 0; surface < surfaces.GetSurfaceCount(); surface++)
            {
                var index = parsed.IndexOf(surfaces.GetSurfaceMaterial(surface));

                if (index >= 0)
                {
                    surfaces.SetSurfaceMaterial(surface, replacements[index]);
                }
            }
        }
    }

    private static Texture2D? ReadBaseColorTexture(GltfState state, JObject description)
    {
        if (description["pbrMetallicRoughness"] is not JObject pbr
            || pbr["baseColorTexture"] is not JObject described
            || described.Value<int?>("index") is not { } index)
        {
            return null;
        }

        return ResolveTexture(state, index);
    }

    private static Texture2D? ResolveTexture(GltfState state, int index)
    {
        var textures = state.Textures;

        if (index < 0 || index >= textures.Count)
        {
            return null;
        }

        var images = state.Images;
        var source = textures[index].SrcImage;

        return source >= 0 && source < images.Count ? images[source] : null;
    }
}
