using System.Collections.Generic;
using System.Text;
using Godot;
using Top.Contracts.Assets.Models.Materials;
using Top.Logging;

namespace Top.Client.Rendering.Models.Materials;

public static class ModelShaderBuilder
{
    private const string BodyPath = "res://Rendering/Assets/Model.gdshaderinc";

    private static readonly Dictionary<string, Shader> Variants = new Dictionary<string, Shader>();

    public static ShaderMaterial CreateMaterial(RenderState state, Texture2D? texture)
    {
        var material = new ShaderMaterial { Shader = ResolveShader(state) };

        material.SetShaderParameter("base_map", texture is null ? default : (Variant)texture);
        material.SetShaderParameter("alpha_cutoff", state.Cutoff);
        material.SetShaderParameter("opacity", state.Opacity);

        return material;
    }

    public static Shader ResolveShader(RenderState state)
    {
        var code = BuildCode(state);

        lock (Variants)
        {
            if (Variants.TryGetValue(code, out var known))
            {
                return known;
            }

            var shader = new Shader { Code = code };

            Variants[code] = shader;

            return shader;
        }
    }

    private static string BuildCode(RenderState state)
    {
        var modes = new List<string> { SelectCullMode(state.Cull) };
        var defines = new List<string>();

        if (state.Blended)
        {
            AppendBlendMode(state, modes, defines);

            if (state.Lit)
            {
                modes.Add("shadows_disabled");
            }

            if (CastsShadow(state))
            {
                modes.Add("depth_prepass_alpha");
            }
        }
        else
        {
            defines.Add("TOP_OPAQUE");
        }

        modes.Add(state.EffectiveZWrite ? "depth_draw_always" : "depth_draw_never");

        if (state.Lit)
        {
            modes.Add("ambient_light_disabled");
            modes.Add("specular_disabled");
        }
        else
        {
            modes.Add("unshaded");
            defines.Add("TOP_UNLIT");
        }

        if (state.AlphaTest)
        {
            defines.Add("TOP_ALPHA_TEST");
        }

        if (state.UvAnimated)
        {
            defines.Add("TOP_UV_ANIMATED");
        }

        var code = new StringBuilder("shader_type spatial;\n")
            .Append("render_mode ")
            .Append(string.Join(", ", modes))
            .Append(";\n");

        foreach (var define in defines)
        {
            code.Append("#define ").Append(define).Append('\n');
        }

        return code.Append("#include \"").Append(BodyPath).Append("\"\n").ToString();
    }

    private static bool CastsShadow(RenderState state)
    {
        return state.Lit
               && state.EffectiveZWrite
               && state.EffectiveSrcBlend == BlendFactor.SrcAlpha
               && state.EffectiveDstBlend == BlendFactor.OneMinusSrcAlpha;
    }

    private static void AppendBlendMode(RenderState state, List<string> modes, List<string> defines)
    {
        var source = state.EffectiveSrcBlend;
        var destination = state.EffectiveDstBlend;

        switch (source, destination)
        {
            case (BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha):
                modes.Add("blend_mix");

                return;
            case (BlendFactor.One, BlendFactor.OneMinusSrcAlpha):
                modes.Add("blend_premul_alpha");

                return;
            case (BlendFactor.DstColor, BlendFactor.Zero):
            case (BlendFactor.Zero, BlendFactor.SrcColor):
                modes.Add("blend_mul");

                return;
            case (BlendFactor.One, BlendFactor.Zero):
                modes.Add("blend_mix");
                defines.Add("TOP_SRC_ONE");

                return;
        }

        if (destination != BlendFactor.One)
        {
            modes.Add("blend_mix");
        }
        else
        {
            modes.Add("blend_add");
            defines.Add("TOP_SRGB_ADD");

            switch (source)
            {
                case BlendFactor.SrcAlpha:
                    return;
                case BlendFactor.One:
                    defines.Add("TOP_SRC_ONE");

                    return;
                case BlendFactor.SrcColor:
                    defines.Add("TOP_SRC_COLOR");

                    return;
            }
        }

        Log.Warning($"blend {source}/{destination} has no Godot render mode");
    }

    private static string SelectCullMode(FaceCulling cull)
    {
        return cull switch
        {
            FaceCulling.None => "cull_disabled",
            FaceCulling.Front => "cull_front",
            _ => "cull_back"
        };
    }
}
