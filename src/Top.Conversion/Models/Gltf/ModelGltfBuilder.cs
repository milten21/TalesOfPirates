using System;
using System.Collections.Generic;
using System.Numerics;
using Top.Contracts.Assets.Models;
using Top.Conversion.Models.Characters;
using Top.Conversion.Gltf;
using Top.Logging;
using Top.Legacy.MindPower.Animation;
using Top.Legacy.MindPower.Geometry;
using Top.Legacy.Tables.Custom;

namespace Top.Conversion.Models.Gltf
{
    /// <summary>
    /// Builds the glTF document for one model. It wraps <see cref="GltfBuilder"/>
    /// with the vocabulary of the original client's model formats and holds the
    /// state that spans a whole model.
    /// </summary>
    public class ModelGltfBuilder
    {
        private const string Generator = "Top.Conversion";

        private static readonly Helper[] NoHelpers = Array.Empty<Helper>();

        private readonly GltfBuilder _gltf;
        private readonly string _name;
        private readonly MeshWriter _meshes;
        private readonly SkeletonWriter _skeletons;
        private readonly List<SkinnedObject> _skinnedObjects = new List<SkinnedObject>();
        private readonly HashSet<GeometryObject> _skinnedGeometry = new HashSet<GeometryObject>();

        private GltfSkinRef _skin;
        private int _helperIndex;

        public ModelGltfBuilder(string name, string textureUriPrefix = null, int? litSubset = null,
            Func<TextureStage, float, bool> isTextureTranslucent = null)
        {
            _gltf = new GltfBuilder(name, Generator);
            _name = name;
            _meshes = new MeshWriter(_gltf,
                new MaterialWriter(_gltf, name, textureUriPrefix, isTextureTranslucent), litSubset);
            _skeletons = new SkeletonWriter(_gltf);
        }

        public GltfFile Build()
        {
            return _gltf.Build();
        }

        public void AddRigged(BoneAnimation skeleton, GeometryObject[] parts, CharacterAction[] actions,
            bool emitClips)
        {
            var boneNodes = _skeletons.AddSkeleton(skeleton);

            _skeletons.AddDummies(skeleton, boneNodes);

            foreach (var part in parts)
            {
                AddPart(part, skeleton, boneNodes).AsRoot();
            }

            if (emitClips)
            {
                _skeletons.AddClips(skeleton, boneNodes, actions, _name);
            }
        }

        public void AddRig(BoneAnimation skeleton)
        {
            var boneNodes = _skeletons.AddSkeleton(skeleton);

            _skeletons.AddDummies(skeleton, boneNodes);
            _skeletons.AddSkin(skeleton, boneNodes, _name);
        }

        public void AddClip(BoneAnimation skeleton, Matrix4x4[][] keys, int startFrame, int endFrame)
        {
            var boneNodes = _skeletons.AddSkeleton(skeleton);

            _skeletons.AddSkin(skeleton, boneNodes, _name);
            _skeletons.AddBoneAnimation(skeleton, boneNodes, keys, startFrame, endFrame, _name);
        }

        public void AddRiggedObject(GeometryObject obj, BoneAnimation skeleton)
        {
            var boneNodes = _skeletons.AddSkeleton(skeleton);

            AddPart(obj, skeleton, boneNodes).AsRoot();
            AddHelpers(HelpersOf(obj));
        }

        public void AddObject(GeometryObject obj)
        {
            AddObjects(new[] { obj }, HelpersOf(obj));
        }

        public void AddRigidPart(GeometryObject part, IReadOnlyList<ClipRange> clips)
        {
            var node = AddGeometry(new[] { part })[0].AsRoot();

            AddHelpers(HelpersOf(part));

            if (DecomposeMatrixFrames(part) is not { } track)
            {
                return;
            }

            if (clips.Count == 0)
            {
                AddMatrixAnimation(track, node, 0, track.Length - 1, node.Name);

                return;
            }

            foreach (var clip in clips)
            {
                AddMatrixAnimation(track, node, clip.StartFrame, clip.EndFrame, clip.Name);
            }
        }

        public void AddObjects(GeometryObject[] objects, Helper[] helpers)
        {
            var nodes = AddGeometry(objects);

            Parent(objects, nodes);
            AddHelpers(helpers);

            for (var i = 0; i < objects.Length; i++)
            {
                if (!_skinnedGeometry.Contains(objects[i]))
                {
                    AddMatrixAnimation(objects[i], nodes[i]);
                }
            }

            foreach (var entry in _skinnedObjects)
            {
                _skeletons.AddBoneAnimation(entry.Skeleton, entry.BoneNodes,
                    BoneTracks.Convert(entry.Skeleton), 0, entry.Skeleton.FrameCount - 1,
                    Naming.Geometry(entry.Object.Id));
            }
        }

        private static Helper[] HelpersOf(GeometryObject obj)
        {
            return obj.Helper == null ? NoHelpers : new[] { obj.Helper };
        }

        private static bool HasBlendData(Mesh mesh)
        {
            return mesh.SkinBlends != null && mesh.SkinBlends.Length > 0
                                           && mesh.BoneIndices != null && mesh.BoneIndices.Length > 0;
        }

        private static Matrix4x4[] MatrixFrames(GeometryObject obj)
        {
            var frames = obj.Animation?.Matrix?.Frames;

            return frames != null && frames.Length > 0 ? frames : null;
        }

        private static void WarnAboutIgnoredData(GeometryObject obj, BoneAnimation skeleton, bool hasBlends)
        {
            if (skeleton == null && obj.Mesh.SkinBlends != null && obj.Mesh.SkinBlends.Length > 0)
            {
                Log.Warning($"skinning data on object {obj.Id} ignored");
            }

            if (skeleton != null && !hasBlends)
            {
                Log.Warning($"bone animation on object {obj.Id} ignored, no blend data");
            }

            if (obj.Mesh.PointType != 4)
            {
                Log.Warning($"object {obj.Id} uses primitive type {obj.Mesh.PointType}, " +
                            "only triangle lists convert");
            }
        }

        private GltfNodeRef[] AddGeometry(GeometryObject[] objects)
        {
            var nodes = new GltfNodeRef[objects.Length];
            var ids = new HashSet<uint>();

            for (var i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];
                var skeleton = obj.Animation?.Bone;
                var hasBlends = HasBlendData(obj.Mesh);
                var skinned = skeleton != null && hasBlends;

                WarnAboutIgnoredData(obj, skeleton, hasBlends);

                var attributes = skinned ? _meshes.AddSkinAttributes(obj.Mesh) : null;
                var nodeName = Naming.Geometry(obj.Id);
                var meshes = _meshes.Add(obj, nodeName, attributes);
                var node = _gltf.AddNode(nodeName).WithMesh(meshes.Mesh);

                PlaceNode(obj, node, skinned);

                if (!ids.Add(obj.Id))
                {
                    Log.Warning($"object id {obj.Id} occurs more than once, nodes share the name '{nodeName}'");
                }

                nodes[i] = node;

                if (meshes.LitMesh != null)
                {
                    node.AddChild(_gltf.AddNode(Naming.LitShell(nodeName)).WithMesh(meshes.LitMesh));
                }

                if (skinned)
                {
                    AddOwnSkeleton(obj, skeleton, node, nodeName);
                }
            }

            return nodes;
        }

        private void AddHelpers(Helper[] helpers)
        {
            foreach (var helper in helpers)
            {
                foreach (var dummy in helper.Dummies ?? Array.Empty<HelperDummy>())
                {
                    var dummyNode = _gltf.AddNode(Naming.Dummy(dummy.Id));

                    Axis.Place(dummyNode, dummy.Matrix);

                    dummyNode.AsRoot();
                }

                foreach (var helperMesh in helper.Meshes ?? Array.Empty<HelperMesh>())
                {
                    var nodeName = Naming.Helper(helperMesh.Name, _helperIndex++);
                    var node = _gltf.AddNode(nodeName).WithMesh(_meshes.AddHelper(helperMesh, nodeName));

                    Axis.Place(node, helperMesh.Matrix);

                    node.AsRoot();
                }
            }
        }

        private static MatrixTrack DecomposeMatrixFrames(GeometryObject obj)
        {
            var frames = MatrixFrames(obj);

            if (frames == null)
            {
                return null;
            }

            var track = new MatrixTrack(frames.Length);
            var previous = Quaternion.Identity;

            for (var i = 0; i < frames.Length; i++)
            {
                if (!Matrix4x4.Decompose(Axis.ToGltf(frames[i]),
                        out var scale, out var rotation, out var translation))
                {
                    Log.Warning($"matrix animation on object {obj.Id} is not TRS-decomposable, skipped");

                    return null;
                }

                if (i > 0 && Quaternion.Dot(previous, rotation) < 0)
                {
                    rotation = new Quaternion(-rotation.X, -rotation.Y, -rotation.Z, -rotation.W);
                }

                previous = rotation;
                track.Translations[i] = translation;
                track.Rotations[i] = new Vector4(rotation.X, rotation.Y, rotation.Z, rotation.W);
                track.Scales[i] = scale;
            }

            return track;
        }

        private void AddMatrixAnimation(GeometryObject obj, GltfNodeRef node)
        {
            if (DecomposeMatrixFrames(obj) is { } track)
            {
                AddMatrixAnimation(track, node, 0, track.Length - 1, node.Name);
            }
        }

        private void AddMatrixAnimation(MatrixTrack track, GltfNodeRef node, int startFrame, int endFrame,
            string name)
        {
            var start = Math.Max(startFrame, 0);
            var end = Math.Min(endFrame, track.Length - 1);

            if (start != startFrame || end != endFrame)
            {
                Log.Warning($"clip {name} range {startFrame}..{endFrame} clamped to 0..{track.Length - 1}");
            }

            if (end < start)
            {
                Log.Warning($"clip {name} has an empty range, skipped");

                return;
            }

            var input = _gltf.Buffer.AddScalars(Timeline.Seconds(end - start + 1), withMinMax: true);

            _gltf
                .AddAnimation(name)
                .AddChannel(input, _gltf.Buffer.AddVec3(track.Translations[start..(end + 1)], withMinMax: false,
                    vertexData: false), node, "translation")
                .AddChannel(input, _gltf.Buffer.AddVec4(track.Rotations[start..(end + 1)]), node, "rotation")
                .AddChannel(input, _gltf.Buffer.AddVec3(track.Scales[start..(end + 1)], withMinMax: false,
                    vertexData: false), node, "scale");
        }

        private GltfNodeRef AddPart(GeometryObject part, BoneAnimation skeleton, GltfNodeRef[] boneNodes)
        {
            var skinned = HasBlendData(part.Mesh);
            SkinAttributes attributes = null;

            if (skinned)
            {
                attributes = _meshes.AddSkinAttributes(part.Mesh);
            }
            else
            {
                Log.Warning($"part {part.Id} carries no blend data, mesh stays rigid");
            }

            var nodeName = Naming.Geometry(part.Id);
            var node = _gltf.AddNode(nodeName).WithMesh(_meshes.Add(part, nodeName, attributes).Mesh);

            if (skinned)
            {
                _skin ??= _skeletons.AddSkin(skeleton, boneNodes, _name);
                node.WithSkin(_skin);
            }
            else
            {
                Axis.Place(node, part.LocalMatrix);
            }

            return node;
        }

        private void AddOwnSkeleton(GeometryObject obj, BoneAnimation skeleton, GltfNodeRef node, string nodeName)
        {
            var container = _gltf.AddNode(Naming.SkeletonContainer(nodeName)).AsRoot();
            var boneNodes = _skeletons.AddSkeleton(skeleton, container);

            _skeletons.AddDummies(skeleton, boneNodes);

            node.WithSkin(_skeletons.AddSkin(skeleton, boneNodes, nodeName));
            _skinnedObjects.Add(new SkinnedObject { Object = obj, Skeleton = skeleton, BoneNodes = boneNodes });
            _skinnedGeometry.Add(obj);
        }

        private void PlaceNode(GeometryObject obj, GltfNodeRef node, bool skinned)
        {
            var animated = MatrixFrames(obj) != null;

            if (skinned)
            {
                if (!obj.LocalMatrix.IsIdentity)
                {
                    Log.Warning($"object {obj.Id} is skinned, its local matrix is ignored");
                }

                if (animated)
                {
                    Log.Warning($"object {obj.Id} has both bone and matrix animation, " +
                                "the matrix animation is ignored");
                }

                return;
            }

            if (animated && Matrix4x4.Decompose(Axis.ToGltf(obj.LocalMatrix),
                    out var scale, out var rotation, out var translation))
            {
                node.WithTrs(translation, rotation, scale);
            }
            else
            {
                Axis.Place(node, obj.LocalMatrix);
            }
        }

        private void Parent(GeometryObject[] objects, GltfNodeRef[] nodes)
        {
            for (var i = 0; i < objects.Length; i++)
            {
                var parentIndex = objects[i].ParentId;

                if (parentIndex != uint.MaxValue && parentIndex < objects.Length && parentIndex != i)
                {
                    if (_skinnedGeometry.Contains(objects[i]))
                    {
                        Log.Warning($"object {objects[i].Id} is skinned, its parent link is ignored");
                    }

                    nodes[parentIndex].AddChild(nodes[i]);
                }
                else
                {
                    if (parentIndex != uint.MaxValue)
                    {
                        Log.Warning($"object {objects[i].Id} has no resolvable parent " +
                                    $"at index {parentIndex}, kept as a root");
                    }

                    nodes[i].AsRoot();
                }
            }
        }

        private class MatrixTrack
        {
            public MatrixTrack(int length)
            {
                Translations = new Vector3[length];
                Rotations = new Vector4[length];
                Scales = new Vector3[length];
            }

            public int Length => Translations.Length;

            public Vector3[] Translations { get; }

            public Vector4[] Rotations { get; }

            public Vector3[] Scales { get; }
        }

        private class SkinnedObject
        {
            public GeometryObject Object;
            public BoneAnimation Skeleton;
            public GltfNodeRef[] BoneNodes;
        }
    }
}
