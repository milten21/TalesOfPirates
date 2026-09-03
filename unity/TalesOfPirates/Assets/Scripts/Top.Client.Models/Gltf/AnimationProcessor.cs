using System.Collections.Generic;
using GLTFast;
using GLTFast.Addons;
using GLTFast.Animations;
using GLTFast.Schema;
using Top.Logging;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Top.Client.Models.Gltf
{
    public class AnimationProcessor : IAnimationProcessor
    {
        private static readonly string[] TranslationProperties =
            { "localPosition.x", "localPosition.y", "localPosition.z" };

        private static readonly string[] RotationProperties =
            { "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" };

        private static readonly string[] ScaleProperties =
            { "localScale.x", "localScale.y", "localScale.z" };

        private class PendingCurve
        {
            public int TargetNode;
            public string[] Properties;
            public AnimationCurve[] Curves;
        }

        private class PendingClip
        {
            public string Name;
            public INodeHierarchyInfo Hierarchy;
            public readonly List<PendingCurve> Curves = new List<PendingCurve>();
        }

        private readonly PendingClip[] _clips;

        public List<HostedClip> Clips { get; private set; }

        public AnimationProcessor(int clipCount)
        {
            _clips = new PendingClip[clipCount];
        }

        public void AddClip(int index, string name)
        {
            _clips[index] = new PendingClip { Name = name };
        }

        public void AddTranslationCurves(int clipIndex, int targetNode,
            INodeHierarchyInfo nodeHierarchyInfo, NativeArray<float>.ReadOnly times,
            NativeArray<float3>.ReadOnly values, InterpolationType interpolationType)
        {
            var curves = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };

            BuildLinear(times, i => values[i], curves);
            WarnIfNotLinear(interpolationType);
            Store(clipIndex, targetNode, nodeHierarchyInfo, TranslationProperties, curves);
        }

        public void AddRotationCurves(int clipIndex, int targetNode,
            INodeHierarchyInfo nodeHierarchyInfo, NativeArray<float>.ReadOnly times,
            NativeArray<quaternion>.ReadOnly values, InterpolationType interpolationType)
        {
            var curves = new[]
            {
                new AnimationCurve(), new AnimationCurve(), new AnimationCurve(), new AnimationCurve(),
            };

            var previous = values[0].value;

            BuildLinear4(times, i =>
            {
                var value = values[i].value;

                if (i > 0 && math.dot(previous, value) < 0f)
                {
                    value = -value;
                }

                previous = value;

                return value;
            }, curves);

            WarnIfNotLinear(interpolationType);
            Store(clipIndex, targetNode, nodeHierarchyInfo, RotationProperties, curves);
        }

        public void AddScaleCurves(int clipIndex, int targetNode,
            INodeHierarchyInfo nodeHierarchyInfo, NativeArray<float>.ReadOnly times,
            NativeArray<float3>.ReadOnly values, InterpolationType interpolationType)
        {
            var curves = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };

            BuildLinear(times, i => values[i], curves);
            WarnIfNotLinear(interpolationType);
            Store(clipIndex, targetNode, nodeHierarchyInfo, ScaleProperties, curves);
        }

        public void AddMorphTargetWeightCurves(int clipIndex, int targetNode, int meshNumeration,
            string meshName, INodeHierarchyInfo nodeHierarchyInfo, NativeArray<float>.ReadOnly times,
            NativeArray<float>.ReadOnly values, InterpolationType interpolationType,
            string[] morphTargetNames = null)
        {
        }

        public GLTFast.Addons.IDataInstanceApplierFactory Complete()
        {
            Clips = new List<HostedClip>();

            foreach (var pending in _clips)
            {
                if (pending == null || pending.Curves.Count == 0)
                {
                    continue;
                }

                var targets = new HashSet<int>();

                foreach (var curve in pending.Curves)
                {
                    targets.Add(curve.TargetNode);
                }

                var host = CommonAncestor(targets, pending.Hierarchy);
                var clip = new AnimationClip
                {
                    name = pending.Name,
                    legacy = true,
                    wrapMode = WrapMode.Loop,
                };

                foreach (var curve in pending.Curves)
                {
                    var path = PathBetween(curve.TargetNode, host, pending.Hierarchy);

                    for (var i = 0; i < curve.Properties.Length; i++)
                    {
                        clip.SetCurve(path, typeof(Transform), curve.Properties[i], curve.Curves[i]);
                    }
                }

                Clips.Add(new HostedClip { Clip = clip, HostNode = host });
            }

            return null;
        }

        public void Dispose()
        {
        }

        private static void WarnIfNotLinear(InterpolationType interpolationType)
        {
            if (interpolationType != InterpolationType.Linear)
            {
                Log.Warning($"{interpolationType} animation interpolation played as linear");
            }
        }

        private void Store(int clipIndex, int targetNode, INodeHierarchyInfo hierarchy,
            string[] properties, AnimationCurve[] curves)
        {
            var clip = _clips[clipIndex];

            clip.Hierarchy = hierarchy;
            clip.Curves.Add(new PendingCurve
            {
                TargetNode = targetNode,
                Properties = properties,
                Curves = curves,
            });
        }

        private delegate float3 Vec3At(int index);

        private delegate float4 Vec4At(int index);

        private static void BuildLinear(NativeArray<float>.ReadOnly times, Vec3At valueAt,
            AnimationCurve[] curves)
        {
            var previousTime = times[0];
            var previousValue = valueAt(0);
            var inTangent = float3.zero;

            for (var i = 1; i < times.Length; i++)
            {
                var time = times[i];

                if (previousTime >= time)
                {
                    continue;
                }

                var value = valueAt(i);
                var outTangent = (value - previousValue) / (time - previousTime);

                AddKeys(curves, previousTime, previousValue, inTangent, outTangent);

                inTangent = outTangent;
                previousTime = time;
                previousValue = value;
            }

            AddKeys(curves, previousTime, previousValue, inTangent, float3.zero);
        }

        private static void BuildLinear4(NativeArray<float>.ReadOnly times, Vec4At valueAt,
            AnimationCurve[] curves)
        {
            var previousTime = times[0];
            var previousValue = valueAt(0);
            var inTangent = float4.zero;

            for (var i = 1; i < times.Length; i++)
            {
                var time = times[i];

                if (previousTime >= time)
                {
                    continue;
                }

                var value = valueAt(i);
                var outTangent = (value - previousValue) / (time - previousTime);

                AddKeys4(curves, previousTime, previousValue, inTangent, outTangent);

                inTangent = outTangent;
                previousTime = time;
                previousValue = value;
            }

            AddKeys4(curves, previousTime, previousValue, inTangent, float4.zero);
        }

        private static void AddKeys(AnimationCurve[] curves, float time, float3 value,
            float3 inTangent, float3 outTangent)
        {
            curves[0].AddKey(new Keyframe(time, value.x, inTangent.x, outTangent.x));
            curves[1].AddKey(new Keyframe(time, value.y, inTangent.y, outTangent.y));
            curves[2].AddKey(new Keyframe(time, value.z, inTangent.z, outTangent.z));
        }

        private static void AddKeys4(AnimationCurve[] curves, float time, float4 value,
            float4 inTangent, float4 outTangent)
        {
            curves[0].AddKey(new Keyframe(time, value.x, inTangent.x, outTangent.x));
            curves[1].AddKey(new Keyframe(time, value.y, inTangent.y, outTangent.y));
            curves[2].AddKey(new Keyframe(time, value.z, inTangent.z, outTangent.z));
            curves[3].AddKey(new Keyframe(time, value.w, inTangent.w, outTangent.w));
        }

        private static int CommonAncestor(HashSet<int> targets, INodeHierarchyInfo hierarchy)
        {
            var ancestor = -2;

            foreach (var target in targets)
            {
                ancestor = ancestor == -2 ? target : Ancestor(ancestor, target, hierarchy);
            }

            return ancestor;
        }

        private static int Ancestor(int first, int second, INodeHierarchyInfo hierarchy)
        {
            if (first < 0 || second < 0)
            {
                return -1;
            }

            var chain = new HashSet<int>();

            for (var node = first; node >= 0; node = hierarchy.GetParentIndex(node))
            {
                chain.Add(node);
            }

            for (var node = second; node >= 0; node = hierarchy.GetParentIndex(node))
            {
                if (chain.Contains(node))
                {
                    return node;
                }
            }

            return -1;
        }

        private static string PathBetween(int target, int host, INodeHierarchyInfo hierarchy)
        {
            if (target == host)
            {
                return "";
            }

            var names = new List<string>();

            for (var node = target; node >= 0 && node != host; node = hierarchy.GetParentIndex(node))
            {
                names.Add(hierarchy.GetNodeName(node));
            }

            names.Reverse();

            return string.Join("/", names);
        }
    }
}
