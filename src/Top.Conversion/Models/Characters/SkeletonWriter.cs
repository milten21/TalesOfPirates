using System;
using System.Linq;
using System.Numerics;
using Top.Contracts.Assets.Models;
using Top.Conversion.Models.Gltf;
using Top.Conversion.Gltf;
using Top.Logging;
using Top.Legacy.MindPower.Animation;
using Top.Legacy.Tables.Custom;

namespace Top.Conversion.Models.Characters
{
    /// <summary>
    /// Writes a skeleton and everything that hangs off it into a glTF document:
    /// the bone nodes, the dummies parented to them, the skin a mesh binds, and
    /// the clips cut out of the timeline.
    /// </summary>
    public class SkeletonWriter
    {
        private const float ScaleTolerance = 1e-4f;

        private readonly GltfBuilder _gltf;

        public SkeletonWriter(GltfBuilder gltf)
        {
            _gltf = gltf;
        }

        /// <summary>
        /// One node per bone, parented to each other. Bones with no parent hang
        /// off <paramref name="parent"/>, or become roots when it is absent. A
        /// bone rests at the translation, rotation and scale nearest to its
        /// bind, which a sheared bind cannot reach exactly.
        /// </summary>
        public GltfNodeRef[] AddSkeleton(BoneAnimation skeleton, GltfNodeRef parent = null)
        {
            var boneNodes = new GltfNodeRef[skeleton.Bones.Length];

            for (var i = 0; i < skeleton.Bones.Length; i++)
            {
                var bone = skeleton.Bones[i];
                var node = _gltf.AddNode(string.IsNullOrEmpty(bone.Name) ? $"bone_{i}" : bone.Name);

                if (!BindLocal(skeleton, i).IsIdentity)
                {
                    var rest = Rest(skeleton, i);

                    node.WithTrs(rest.Translation, rest.Rotation, rest.Scale);
                }

                boneNodes[i] = node;
            }

            for (var i = 0; i < skeleton.Bones.Length; i++)
            {
                var parentId = skeleton.Bones[i].ParentId;

                if (parentId >= 0 && parentId < skeleton.Bones.Length)
                {
                    boneNodes[parentId].AddChild(boneNodes[i]);
                }
                else if (parent != null)
                {
                    parent.AddChild(boneNodes[i]);
                }
                else
                {
                    boneNodes[i].AsRoot();
                }
            }

            return boneNodes;
        }

        public void AddDummies(BoneAnimation skeleton, GltfNodeRef[] boneNodes)
        {
            if (skeleton.Dummies == null)
            {
                return;
            }

            var inverseBind = InverseBinds(skeleton);

            foreach (var dummy in skeleton.Dummies)
            {
                var node = _gltf.AddNode(Naming.Dummy(dummy.Id));
                var parented = dummy.ParentBoneId < boneNodes.Length;

                var local = parented
                    ? Axis.ToGltf(dummy.Matrix) * inverseBind[dummy.ParentBoneId]
                    : Axis.ToGltf(dummy.Matrix);

                if (!local.IsIdentity)
                {
                    node.WithMatrix(Axis.ToGltfMatrix(local));
                }

                if (parented)
                {
                    boneNodes[dummy.ParentBoneId].AddChild(node);
                }
                else
                {
                    Log.Warning($"dummy {dummy.Id} references missing bone {dummy.ParentBoneId}");
                    node.AsRoot();
                }
            }
        }

        public GltfSkinRef AddSkin(BoneAnimation skeleton, GltfNodeRef[] boneNodes, string name)
        {
            var skin = _gltf.AddSkin(name, boneNodes, _gltf.Buffer.AddMatrices(InverseBinds(skeleton)));
            var root = Array.FindIndex(skeleton.Bones, bone => bone.ParentId < 0);

            if (root >= 0)
            {
                skin.WithSkeletonRoot(boneNodes[root]);
            }

            return skin;
        }

        /// <summary>
        /// One clip per action. Without an action table the whole timeline
        /// becomes a single clip.
        /// </summary>
        public void AddClips(BoneAnimation skeleton, GltfNodeRef[] boneNodes, CharacterAction[] actions,
            string modelName)
        {
            if (actions == null || actions.Length == 0)
            {
                actions = new[]
                {
                    new CharacterAction { StartFrame = 0, EndFrame = skeleton.FrameCount - 1 },
                };

                Log.Warning("no action table entry, emitting the full timeline as one clip");
            }

            var keys = BoneTracks.Convert(skeleton);

            foreach (var action in actions)
            {
                AddBoneAnimation(skeleton, boneNodes, keys, action.StartFrame, action.EndFrame,
                    Naming.ActionClip(modelName, action.ActionNo));
            }
        }

        public void AddBoneAnimation(BoneAnimation skeleton, GltfNodeRef[] boneNodes, Matrix4x4[][] keys,
            int startFrame, int endFrame, string name)
        {
            var start = startFrame;
            var end = endFrame;

            if (start < 0 || end >= skeleton.FrameCount)
            {
                Log.Warning($"clip {name} range {start}..{end} clamped to 0..{skeleton.FrameCount - 1}");
                start = Math.Max(start, 0);
                end = Math.Min(end, skeleton.FrameCount - 1);
            }

            if (end < start)
            {
                Log.Warning($"clip {name} has an empty range, skipped");

                return;
            }

            var animation = _gltf.AddAnimation(name);
            var input = _gltf.Buffer.AddScalars(Timeline.Seconds(end - start + 1), withMinMax: true);

            for (var i = 0; i < boneNodes.Length; i++)
            {
                if (keys[i] == null)
                {
                    continue;
                }

                if (end >= keys[i].Length)
                {
                    Log.Warning($"clip {name}: track of bone {skeleton.Bones[i].Name} " +
                                "is shorter than the timeline, the bone stays in bind pose");
                    continue;
                }

                AddChannels(animation, input, boneNodes[i], Rest(skeleton, i), keys[i][start..(end + 1)]);
            }

            if (animation.ChannelCount == 0)
            {
                // The builder drops it, glTF rejects a channelless animation.
                Log.Warning($"clip {name} has no usable tracks, skipped");
            }
        }

        /// <summary>
        /// The bone's local bind matrix in glTF axes: its bind pose relative
        /// to its parent's.
        /// </summary>
        private static Matrix4x4 BindLocal(BoneAnimation skeleton, int bone)
        {
            var parentId = skeleton.Bones[bone].ParentId;

            Matrix4x4.Invert(skeleton.Bones[bone].InvBindMatrix, out var bindWorld);

            return Axis.ToGltf(parentId >= 0 && parentId < skeleton.Bones.Length
                ? bindWorld * skeleton.Bones[parentId].InvBindMatrix
                : bindWorld);
        }

        private static Trs Rest(BoneAnimation skeleton, int bone)
        {
            return Trs.Of(BindLocal(skeleton, bone));
        }

        /// <summary>
        /// The inverse of each bone's rest as the nodes hold it, times its
        /// parents', in glTF axes. The skin and the dummies bind each bone here
        /// rather than at its .lab bind, which a sheared bind would miss, so
        /// that the rest pose leaves every vertex and dummy where it is.
        /// </summary>
        private static Matrix4x4[] InverseBinds(BoneAnimation skeleton)
        {
            var world = new Matrix4x4[skeleton.Bones.Length];
            var known = new bool[skeleton.Bones.Length];

            Matrix4x4 Of(int bone)
            {
                if (!known[bone])
                {
                    var parentId = skeleton.Bones[bone].ParentId;
                    var local = Rest(skeleton, bone).Matrix;

                    world[bone] = parentId >= 0 && parentId < skeleton.Bones.Length ? local * Of(parentId) : local;
                    known[bone] = true;
                }

                return world[bone];
            }

            var inverseBind = new Matrix4x4[world.Length];

            for (var i = 0; i < world.Length; i++)
            {
                Matrix4x4.Invert(Of(i), out inverseBind[i]);
            }

            return inverseBind;
        }

        private void AddChannels(GltfAnimationRef animation, int input, GltfNodeRef bone, Trs rest, Matrix4x4[] keys)
        {
            var poses = new Trs[keys.Length];
            var previous = rest;

            for (var k = 0; k < keys.Length; k++)
            {
                poses[k] = previous = Trs.Of(keys[k], previous);
            }

            animation
                .AddChannel(input, _gltf.Buffer.AddVec3(poses.Select(pose => pose.Translation).ToArray(),
                    withMinMax: false, vertexData: false), bone, "translation")
                .AddChannel(input, _gltf.Buffer.AddVec4(poses
                    .Select(pose => new Vector4(pose.Rotation.X, pose.Rotation.Y, pose.Rotation.Z, pose.Rotation.W))
                    .ToArray()), bone, "rotation");

            if (poses.Any(pose => Vector3.Distance(pose.Scale, rest.Scale) > ScaleTolerance))
            {
                animation.AddChannel(input, _gltf.Buffer.AddVec3(poses.Select(pose => pose.Scale).ToArray(),
                    withMinMax: false, vertexData: false), bone, "scale");
            }
        }
    }
}
