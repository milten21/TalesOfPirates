using GLTFast.Schema;
using Newtonsoft.Json.Linq;
using Top.Contracts.Assets.Models.Extras;
using Top.Contracts.Assets.Models.Materials;
using Top.Logging;
using Material = GLTFast.Newtonsoft.Schema.Material;

namespace Top.Client.Models.Materials
{
    public static class MaterialStateReader
    {
        public static RenderState Read(MaterialBase material)
        {
            var name = material.name ?? "unnamed";
            var state = new RenderState();

            if (material.doubleSided)
            {
                state.Cull = FaceCulling.None;
            }

            var mode = material.GetAlphaMode();

            switch (mode)
            {
                case MaterialBase.AlphaMode.Opaque:
                    break;
                case MaterialBase.AlphaMode.Mask:
                    state.AlphaTest = true;
                    state.Cutoff = material.alphaCutoff;
                    break;
                case MaterialBase.AlphaMode.Blend:
                    state.SrcBlend = BlendFactor.SrcAlpha;
                    state.DstBlend = BlendFactor.OneMinusSrcAlpha;
                    state.BlendEnabled = true;
                    state.ZWrite = false;
                    break;
                default:
                    Log.Warning($"material '{name}': unknown alpha mode '{mode}'");
                    break;
            }

            var pbr = material.PbrMetallicRoughness;

            if (pbr != null)
            {
                var color = pbr.BaseColor;

                state.Opacity = color.a;

                if (color.r != 1f || color.g != 1f || color.b != 1f)
                {
                    Log.Warning($"material '{name}': base color tint has no counterpart");
                }
            }

            if (TryReadExtras(material, out var extras))
            {
                extras.Refine(state);
            }

            return state;
        }

        public static bool TryReadExtras(MaterialBase material, out MaterialExtras extras)
        {
            extras = null;

            return material is Material newtonsoft
                   && newtonsoft.extras != null
                   && newtonsoft.extras.TryGetValue<JToken>(MaterialExtras.Key, out var payload)
                   && MaterialExtras.TryRead(new JObject { [MaterialExtras.Key] = payload }, out extras);
        }
    }
}
