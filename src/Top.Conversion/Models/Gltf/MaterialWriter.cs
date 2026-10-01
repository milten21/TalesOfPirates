using System;
using Newtonsoft.Json.Linq;
using Top.Contracts.Assets.Models;
using Top.Contracts.Assets.Models.Extras;
using Top.Contracts.Assets.Models.Materials;
using Top.Conversion.Models.Materials;
using Top.Conversion.Textures;
using Top.Conversion.Gltf;
using Top.Logging;
using Top.Legacy.MindPower.Geometry;

namespace Top.Conversion.Models.Gltf
{
    /// <summary>
    /// Writes the materials and textures of one model into a glTF document.
    /// A material carries the standard fields a viewer needs plus the payload
    /// under <see cref="MaterialExtras"/> for everything glTF has no field for.
    /// </summary>
    public class MaterialWriter
    {
        private const int Stage = 0;
        private const uint LinearSlerp = 1;

        private readonly GltfBuilder _gltf;
        private readonly string _modelName;
        private readonly string _textureUriPrefix;
        private readonly Func<TextureStage, float, bool> _isTextureTranslucent;

        public MaterialWriter(GltfBuilder gltf, string modelName, string textureUriPrefix,
            Func<TextureStage, float, bool> isTextureTranslucent = null)
        {
            _gltf = gltf;
            _modelName = modelName;
            _textureUriPrefix = textureUriPrefix;
            _isTextureTranslucent = isTextureTranslucent;
        }

        public static MaterialExtras Extras(GeometryObject obj, int materialIndex, RenderState state,
            Func<string, int> textureIndex)
        {
            return new MaterialExtras
            {
                RenderState = MapRenderState(state),
                UvAnimation = MapUvAnimation(obj, materialIndex),
                OpacityAnimation = MapOpacityAnimation(obj, materialIndex),
                Flipbook = MapFlipbook(obj, materialIndex, textureIndex),
            };
        }

        public GltfMaterialRef Add(GeometryObject obj, int materialIndex)
        {
            if (_textureUriPrefix == null || obj.Materials == null || materialIndex >= obj.Materials.Length)
            {
                return null;
            }

            var state = RenderStateResolver.Resolve(obj, materialIndex, _isTextureTranslucent);
            var fileName = state.TextureFile;
            var material = _gltf.AddMaterial(Naming.Material(_modelName, obj.Id, materialIndex))
                .WithDoubleSided(state.Cull == FaceCulling.None);

            if (state.Opacity != 1f)
            {
                material.WithBaseColorFactor(new[] { 1f, 1f, 1f, state.Opacity });
            }

            if (!string.IsNullOrEmpty(fileName))
            {
                material.WithTexture(AddTexture(fileName));
            }

            if (state.Blended)
            {
                material.WithAlphaBlend();
            }
            else if (state.AlphaTest)
            {
                material.WithAlphaMask(state.Cutoff);
            }

            material.WithExtras(new JObject
            {
                [MaterialExtras.Key] = Extras(obj, materialIndex, state, file => AddTexture(file).Index).ToJson(),
            });

            return material;
        }

        public GltfTextureRef AddTexture(string fileName)
        {
            return _gltf.AddTexture($"{_textureUriPrefix}/{TextureConversion.PngName(fileName)}");
        }

        private static RenderStateExtras MapRenderState(RenderState state)
        {
            return new RenderStateExtras
            {
                SrcBlend = state.SrcBlend,
                DstBlend = state.DstBlend,
                BlendEnabled = state.BlendEnabled,
                ZWrite = state.ZWrite,
                Cull = state.Cull == FaceCulling.Front ? FaceCulling.Front : (FaceCulling?)null,
                AlphaTestCutoff = state.AlphaTest && state.Blended ? state.Cutoff : (float?)null,
                Lit = state.Lit,
                Transparency = state.Transparency,
            };
        }

        private static UvAnimationExtras MapUvAnimation(GeometryObject obj, int materialIndex)
        {
            var tracks = obj.Animation?.TextureUv;

            if (tracks == null || materialIndex >= tracks.GetLength(0))
            {
                return null;
            }

            for (var stage = 1; stage < tracks.GetLength(1); stage++)
            {
                var ignored = tracks[materialIndex, stage];

                if (ignored?.Frames != null && ignored.Frames.Length > 0)
                {
                    Log.Warning($"UV animation on stage {stage} ignored (obj {obj.Id})");
                }
            }

            var source = tracks[materialIndex, Stage]?.Frames;

            if (source == null || source.Length == 0)
            {
                return null;
            }

            var frames = new float[source.Length][];

            for (var i = 0; i < source.Length; i++)
            {
                frames[i] = new[]
                {
                    source[i].M11, source[i].M12,
                    source[i].M21, source[i].M22,
                    source[i].M31, source[i].M32,
                };
            }

            return new UvAnimationExtras { Frames = frames };
        }

        private static OpacityAnimationExtras MapOpacityAnimation(GeometryObject obj, int materialIndex)
        {
            var tracks = obj.Animation?.MaterialOpacity;

            if (tracks == null || materialIndex >= tracks.Length)
            {
                return null;
            }

            var keys = tracks[materialIndex]?.Keys;

            if (keys == null || keys.Length == 0)
            {
                return null;
            }

            var keyFrames = new int[keys.Length];
            var values = new float[keys.Length];

            for (var i = 0; i < keys.Length; i++)
            {
                if (keys[i].SlerpType != LinearSlerp)
                {
                    Log.Warning($"opacity key slerp type {keys[i].SlerpType} treated as linear (obj {obj.Id})");
                }

                keyFrames[i] = (int)keys[i].Key;
                values[i] = keys[i].Value;
            }

            return new OpacityAnimationExtras { KeyFrames = keyFrames, Values = values };
        }

        private static FlipbookExtras MapFlipbook(GeometryObject obj, int materialIndex,
            Func<string, int> textureIndex)
        {
            var tracks = obj.Animation?.TextureImage;

            if (tracks == null || materialIndex >= tracks.GetLength(0))
            {
                return null;
            }

            for (var stage = 1; stage < tracks.GetLength(1); stage++)
            {
                var ignored = tracks[materialIndex, stage];

                if (ignored?.DataSequence != null && ignored.DataSequence.Length > 0)
                {
                    Log.Warning($"texture-image animation on stage {stage} ignored (obj {obj.Id})");
                }
            }

            var sequence = tracks[materialIndex, Stage]?.DataSequence;

            if (sequence == null || sequence.Length == 0)
            {
                return null;
            }

            var frames = new int[sequence.Length];

            for (var i = 0; i < sequence.Length; i++)
            {
                var fileName = sequence[i]?.FileName;

                frames[i] = string.IsNullOrEmpty(fileName)
                    ? FlipbookExtras.NoTexture
                    : textureIndex(fileName);
            }

            return new FlipbookExtras { Frames = frames };
        }
    }
}
