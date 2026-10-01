using System;
using Top.Contracts.Assets.Models.Materials;
using Top.Logging;
using Top.Legacy.MindPower.Geometry;

namespace Top.Conversion.Models.Materials
{
    /// <summary>
    /// Turns one material's MindPower render-state atoms into the contract's
    /// <see cref="RenderState"/>: the D3D flags map straight across, the
    /// transparency mode supplies default blend factors, and alpha-test cutoff
    /// falls out of the material's opacity times the device alpha ref. A
    /// Filter material blends only when its texture keeps a translucent texel
    /// past that cutoff.
    /// </summary>
    public static class RenderStateResolver
    {
        private const uint Terminator = 0xFFFFFFFF;
        private const uint AlphaFuncGreater = 5;
        private const uint DeviceAlphaRef = 129;

        public static RenderState Resolve(GeometryObject obj, int materialIndex,
            Func<TextureStage, float, bool> isTextureTranslucent = null)
        {
            var material = obj.Materials[materialIndex];
            var context = $"obj {obj.Id} material {materialIndex}";
            var state = new RenderState
            {
                UvAnimated = HasUvAnimation(obj, materialIndex),
                OpacityAnimated = HasOpacityAnimation(obj, materialIndex),
            };

            var stage0 = material.Stages != null && material.Stages.Length > 0
                ? material.Stages[0]
                : null;
            var cutoff = (EffectiveAlphaRef(material.Opacity) + 1) / 255f;

            if (material.Transparency == TransparencyType.Filter
                && isTextureTranslucent != null
                && isTextureTranslucent(stage0, HasAlphaTest(material) ? cutoff : 0f))
            {
                SetBlend(state, BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);
            }

            uint alphaFunc = AlphaFuncGreater;

            if (material.RenderStates != null)
            {
                foreach (var atom in material.RenderStates)
                {
                    switch (atom.State)
                    {
                        case Terminator:
                            break;
                        case D3DRenderState.ZWriteEnable:
                            state.ZWrite = atom.Value0 != 0;
                            break;
                        case D3DRenderState.AlphaTestEnable:
                            state.AlphaTest = atom.Value0 != 0;
                            break;
                        case D3DRenderState.SourceBlend:
                            state.SrcBlend = ToBlend(atom.Value0);
                            break;
                        case D3DRenderState.DestinationBlend:
                            state.DstBlend = ToBlend(atom.Value0);
                            break;
                        case D3DRenderState.CullMode:
                            state.Cull = ToCull(atom.Value0);
                            break;
                        case D3DRenderState.AlphaRef:
                            break;
                        case D3DRenderState.AlphaFunc:
                            alphaFunc = atom.Value0;
                            break;
                        case D3DRenderState.AlphaBlendEnable:
                            state.BlendEnabled = atom.Value0 != 0;
                            break;
                        case D3DRenderState.Lighting:
                            state.Lit = atom.Value0 != 0;
                            break;
                        case 23:
                        case 26:
                        case 28:
                            break;
                        default:
                            Log.Warning($"{context}: unmapped render state {atom.State}={atom.Value0}");
                            break;
                    }
                }
            }

            state.Opacity = material.Opacity;
            state.Transparency = ToTransparency(material.Transparency);
            ApplyTransparencyDefaults(state, material.Transparency);

            if (state.AlphaTest)
            {
                if (alphaFunc != AlphaFuncGreater)
                {
                    Log.Warning($"{context}: unsupported alpha func {alphaFunc}");
                }

                state.Cutoff = cutoff;
            }

            state.TextureFile = stage0 != null && !string.IsNullOrEmpty(stage0.FileName)
                ? stage0.FileName
                : null;

            return state;
        }

        private static bool HasAlphaTest(MaterialTexture material)
        {
            var alphaTest = false;

            if (material.RenderStates != null)
            {
                foreach (var atom in material.RenderStates)
                {
                    if (atom.State == D3DRenderState.AlphaTestEnable)
                    {
                        alphaTest = atom.Value0 != 0;
                    }
                }
            }

            return alphaTest;
        }

        private static bool HasUvAnimation(GeometryObject obj, int materialIndex)
        {
            var tracks = obj.Animation?.TextureUv;

            if (tracks == null || materialIndex >= tracks.GetLength(0))
            {
                return false;
            }

            var animation = tracks[materialIndex, 0];

            return animation?.Frames != null && animation.Frames.Length > 0;
        }

        private static bool HasOpacityAnimation(GeometryObject obj, int materialIndex)
        {
            var tracks = obj.Animation?.MaterialOpacity;

            if (tracks == null || materialIndex >= tracks.Length)
            {
                return false;
            }

            var animation = tracks[materialIndex];

            return animation?.Keys != null && animation.Keys.Length > 0;
        }

        private static uint EffectiveAlphaRef(float opacity)
        {
            if (opacity == 1f)
            {
                return DeviceAlphaRef;
            }

            var value = (uint)(opacity * 255) - 1;

            return value > DeviceAlphaRef ? DeviceAlphaRef : value;
        }

        private static void ApplyTransparencyDefaults(RenderState state, TransparencyType transparency)
        {
            switch (transparency)
            {
                case TransparencyType.Filter:
                    break;
                case TransparencyType.Additive:
                    SetBlend(state, BlendFactor.One, BlendFactor.One);
                    break;
                case TransparencyType.Additive1:
                    SetBlend(state, BlendFactor.SrcColor, BlendFactor.One);
                    break;
                case TransparencyType.Additive2:
                    SetBlend(state, BlendFactor.SrcColor, BlendFactor.OneMinusSrcColor);
                    break;
                case TransparencyType.Additive3:
                    SetBlend(state, BlendFactor.SrcAlpha, BlendFactor.DstAlpha);
                    break;
                case TransparencyType.Subtractive:
                    SetBlend(state, BlendFactor.Zero, BlendFactor.OneMinusSrcColor);
                    break;
                default:
                    SetBlend(state, BlendFactor.One, BlendFactor.One);
                    Log.Warning($"transparency {transparency} has no reference mapping");
                    break;
            }
        }

        private static void SetBlend(RenderState state, BlendFactor src, BlendFactor dst)
        {
            state.SrcBlend = src;
            state.DstBlend = dst;
            state.BlendEnabled = true;
        }

        private static TransparencyMode ToTransparency(TransparencyType transparency)
        {
            return transparency switch
            {
                TransparencyType.Additive => TransparencyMode.Additive,
                TransparencyType.Additive1 => TransparencyMode.Additive1,
                TransparencyType.Additive2 => TransparencyMode.Additive2,
                TransparencyType.Additive3 => TransparencyMode.Additive3,
                TransparencyType.Subtractive => TransparencyMode.Subtractive,
                TransparencyType.Subtractive1 => TransparencyMode.Subtractive1,
                TransparencyType.Subtractive2 => TransparencyMode.Subtractive2,
                TransparencyType.Subtractive3 => TransparencyMode.Subtractive3,
                _ => TransparencyMode.Filter
            };
        }

        private static BlendFactor ToBlend(uint value)
        {
            switch ((D3DBlend)value)
            {
                case D3DBlend.Zero: return BlendFactor.Zero;
                case D3DBlend.One: return BlendFactor.One;
                case D3DBlend.SrcColor: return BlendFactor.SrcColor;
                case D3DBlend.InvSrcColor: return BlendFactor.OneMinusSrcColor;
                case D3DBlend.SrcAlpha: return BlendFactor.SrcAlpha;
                case D3DBlend.InvSrcAlpha: return BlendFactor.OneMinusSrcAlpha;
                case D3DBlend.DestAlpha: return BlendFactor.DstAlpha;
                case D3DBlend.InvDestAlpha: return BlendFactor.OneMinusDstAlpha;
                case D3DBlend.DestColor: return BlendFactor.DstColor;
                case D3DBlend.InvDestColor: return BlendFactor.OneMinusDstColor;
                default:
                    Log.Warning($"unmapped blend factor {value}");
                    return BlendFactor.One;
            }
        }

        private static FaceCulling ToCull(uint value)
        {
            switch ((D3DCull)value)
            {
                case D3DCull.None: return FaceCulling.None;
                case D3DCull.Cw: return FaceCulling.Front;
                case D3DCull.Ccw: return FaceCulling.Back;
                default:
                    Log.Warning($"unmapped cull mode {value}");
                    return FaceCulling.Back;
            }
        }
    }
}
