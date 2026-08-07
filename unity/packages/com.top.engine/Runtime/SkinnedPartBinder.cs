using System.Collections.Generic;
using Top.Logging;
using UnityEngine;

namespace Top.Engine
{
    /// <summary>
    /// Binds a skinned part onto a rig it did not ship with: MapBones indexes
    /// the rig's transforms by name and Bind clones the source
    /// <see cref="SkinnedMeshRenderer"/> under the rig, rewiring its bones and
    /// root bone to that map (missing bones fall back to the rig root, first
    /// duplicate wins).
    /// </summary>
    public static class SkinnedPartBinder
    {
        public static Dictionary<string, Transform> MapBones(Transform rigRoot)
        {
            var bones = new Dictionary<string, Transform>();

            foreach (var node in rigRoot.GetComponentsInChildren<Transform>(true))
            {
                if (!bones.TryAdd(node.name, node))
                {
                    Log.Warning($"rig has more than one node named '{node.name}', parts bind to the first");
                }
            }

            return bones;
        }

        public static SkinnedMeshRenderer Bind(SkinnedMeshRenderer source, Transform root,
            IReadOnlyDictionary<string, Transform> bones)
        {
            var child = new GameObject(source.name);

            child.transform.SetParent(root, false);

            var target = child.AddComponent<SkinnedMeshRenderer>();

            target.sharedMesh = source.sharedMesh;
            target.sharedMaterials = source.sharedMaterials;
            target.localBounds = source.localBounds;
            target.updateWhenOffscreen = source.updateWhenOffscreen;

            var sourceBones = source.bones;
            var mapped = new Transform[sourceBones.Length];

            for (var i = 0; i < sourceBones.Length; i++)
            {
                if (sourceBones[i] != null && bones.TryGetValue(sourceBones[i].name, out var bone))
                {
                    mapped[i] = bone;
                }
                else
                {
                    var name = sourceBones[i] != null ? sourceBones[i].name : "<null>";

                    Log.Warning($"part '{source.name}': no bone named '{name}' in the rig");
                    mapped[i] = root;
                }
            }

            target.bones = mapped;
            target.rootBone = source.rootBone != null && bones.TryGetValue(source.rootBone.name, out var rootBone)
                ? rootBone
                : root;

            return target;
        }
    }
}
