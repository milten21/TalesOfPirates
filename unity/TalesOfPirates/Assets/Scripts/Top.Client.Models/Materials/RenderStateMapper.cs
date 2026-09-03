using Top.Contracts.Assets.Models.Materials;
using UnityEngine;
using UnityEngine.Rendering;

namespace Top.Client.Models.Materials
{
    /// <summary>
    /// Projects a resolved render state onto a Top/Model material.
    /// </summary>
    public static class RenderStateMapper
    {
        public static Material CreateMaterial(RenderState state, Texture2D texture, Shader shader)
        {
            var material = new Material(shader);

            material.SetFloat("_SrcBlend", (float)ToUnity(state.EffectiveSrcBlend));
            material.SetFloat("_DstBlend", (float)ToUnity(state.EffectiveDstBlend));
            material.SetFloat("_ZWrite", state.EffectiveZWrite ? 1f : 0f);
            material.SetFloat("_Cull", (float)ToUnity(state.Cull));

            if (state.AlphaTest)
            {
                material.SetFloat("_AlphaTest", 1f);
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetFloat("_Cutoff", state.Cutoff);
            }

            if (state.UvAnimated)
            {
                material.SetFloat("_UvAnimated", 1f);
            }

            if (!state.Lit)
            {
                material.SetFloat("_Unlit", 1f);
                material.EnableKeyword("_UNLIT_ON");
            }

            if (state.OpacityDriven)
            {
                material.SetFloat("_Opacity", state.Opacity);
            }

            if (state.Blended)
            {
                material.SetFloat("_ReceiveShadows", 0f);
                material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
                material.SetFloat("_CastShadows", 0f);
                material.EnableKeyword("_CAST_SHADOWS_OFF");
            }

            material.mainTexture = texture;
            material.renderQueue = state.Blended ? 3000 : state.AlphaTest ? 2450 : 2000;

            return material;
        }

        public static BlendMode ToUnity(BlendFactor blend)
        {
            return blend switch
            {
                BlendFactor.Zero => BlendMode.Zero,
                BlendFactor.SrcColor => BlendMode.SrcColor,
                BlendFactor.OneMinusSrcColor => BlendMode.OneMinusSrcColor,
                BlendFactor.SrcAlpha => BlendMode.SrcAlpha,
                BlendFactor.OneMinusSrcAlpha => BlendMode.OneMinusSrcAlpha,
                BlendFactor.DstAlpha => BlendMode.DstAlpha,
                BlendFactor.OneMinusDstAlpha => BlendMode.OneMinusDstAlpha,
                BlendFactor.DstColor => BlendMode.DstColor,
                BlendFactor.OneMinusDstColor => BlendMode.OneMinusDstColor,
                _ => BlendMode.One
            };
        }

        public static CullMode ToUnity(FaceCulling cull)
        {
            return cull switch
            {
                FaceCulling.None => CullMode.Off,
                FaceCulling.Front => CullMode.Front,
                _ => CullMode.Back
            };
        }
    }
}
