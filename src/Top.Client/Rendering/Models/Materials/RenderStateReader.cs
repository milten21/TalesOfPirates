using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Top.Contracts.Assets.Models.Extras;
using Top.Contracts.Assets.Models.Materials;
using Top.Logging;

namespace Top.Client.Rendering.Models.Materials;

public static class RenderStateReader
{
    public static RenderState Read(JObject material)
    {
        var name = material.Value<string>("name") ?? "unnamed";
        var state = new RenderState();

        if (material.Value<bool?>("doubleSided") == true)
        {
            state.Cull = FaceCulling.None;
        }

        var mode = material.Value<string>("alphaMode") ?? "OPAQUE";

        switch (mode)
        {
            case "OPAQUE":
                break;
            case "MASK":
                state.AlphaTest = true;
                state.Cutoff = material.Value<float?>("alphaCutoff") ?? 0.5f;
                break;
            case "BLEND":
                state.SrcBlend = BlendFactor.SrcAlpha;
                state.DstBlend = BlendFactor.OneMinusSrcAlpha;
                state.BlendEnabled = true;
                state.ZWrite = false;
                break;
            default:
                Log.Warning($"material '{name}': unknown alpha mode '{mode}'");
                break;
        }

        if (material["pbrMetallicRoughness"] is JObject pbr
            && pbr["baseColorFactor"] is JArray color)
        {
            if (color.Count < 4)
            {
                Log.Warning($"material '{name}': base color factor has {color.Count} components, not 4");
            }
            else
            {
                state.Opacity = color[3].Value<float>();

                if (color[0].Value<float>() != 1f || color[1].Value<float>() != 1f
                    || color[2].Value<float>() != 1f)
                {
                    Log.Warning($"material '{name}': base color tint has no counterpart");
                }
            }
        }

        if (TryReadExtras(material, out var extras))
        {
            extras.Refine(state);
        }

        return state;
    }

    public static bool TryReadExtras(JObject material, [NotNullWhen(true)] out MaterialExtras? extras)
    {
        extras = null;

        if (material["extras"] is not JObject payload)
        {
            return false;
        }

        try
        {
            return MaterialExtras.TryRead(payload, out extras);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
