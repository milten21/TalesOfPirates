using System;
using System.Collections.Generic;
using System.Numerics;
using Top.Conversion.Gltf;
using Top.Conversion.Models.Characters;
using Top.Legacy.MindPower.Animation;
using Top.Legacy.MindPower.Geometry;
using Top.Legacy.Tables.Custom;

namespace Top.Conversion.Models.Gltf
{
    /// <summary>
    /// Turns one of the original client's model shapes into a glTF document.
    /// Each entry point takes the sources of that shape and hands back the
    /// finished file.
    /// </summary>
    public static class GltfExport
    {
        public static GltfFile Character(string name, BoneAnimation skeleton, GeometryObject[] parts,
            CharacterAction[] actions, string textureUriPrefix = null, bool emitClips = true,
            Func<TextureStage, float, bool> isTextureTranslucent = null)
        {
            var builder = new ModelGltfBuilder(name, textureUriPrefix,
                isTextureTranslucent: isTextureTranslucent);

            builder.AddRigged(skeleton, parts, actions, emitClips);

            return builder.Build();
        }

        public static GltfFile Rig(string name, BoneAnimation skeleton)
        {
            var builder = new ModelGltfBuilder(name);

            builder.AddRig(skeleton);

            return builder.Build();
        }

        public static GltfFile Clip(string name, BoneAnimation skeleton, Matrix4x4[][] keys, int startFrame,
            int endFrame)
        {
            var builder = new ModelGltfBuilder(name);

            builder.AddClip(skeleton, keys, startFrame, endFrame);

            return builder.Build();
        }

        public static GltfFile Model(string name, SceneModel model, string textureUriPrefix = null,
            Func<TextureStage, float, bool> isTextureTranslucent = null)
        {
            var builder = new ModelGltfBuilder(name, textureUriPrefix,
                isTextureTranslucent: isTextureTranslucent);

            builder.AddObjects(model.GeometryObjects, model.Helpers);

            return builder.Build();
        }

        public static GltfFile RigidPart(string name, GeometryObject part, IReadOnlyList<ClipRange> clips,
            string textureUriPrefix = null, Func<TextureStage, float, bool> isTextureTranslucent = null)
        {
            var builder = new ModelGltfBuilder(name, textureUriPrefix,
                isTextureTranslucent: isTextureTranslucent);

            builder.AddRigidPart(part, clips);

            return builder.Build();
        }

        public static GltfFile Object(string name, GeometryObject obj, string textureUriPrefix = null,
            int? litSubset = null, Func<TextureStage, float, bool> isTextureTranslucent = null)
        {
            var builder = new ModelGltfBuilder(name, textureUriPrefix, litSubset, isTextureTranslucent);

            builder.AddObject(obj);

            return builder.Build();
        }

        public static GltfFile Object(string name, GeometryObject obj, BoneAnimation skeleton,
            string textureUriPrefix = null, Func<TextureStage, float, bool> isTextureTranslucent = null)
        {
            var builder = new ModelGltfBuilder(name, textureUriPrefix,
                isTextureTranslucent: isTextureTranslucent);

            builder.AddRiggedObject(obj, skeleton);

            return builder.Build();
        }
    }
}
