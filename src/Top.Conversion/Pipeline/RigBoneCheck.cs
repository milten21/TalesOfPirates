using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Conversion.Gltf;
using Top.Logging;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// Compares the bones a converted part binds with the bones of its
    /// converted rig. The client binds a part to the rig by bone name, and a
    /// name the rig lacks binds to the rig's first bone.
    /// </summary>
    public class RigBoneCheck
    {
        private static readonly IReadOnlyList<string> NoBones = Array.Empty<string>();

        private readonly Dictionary<string, HashSet<string>> _rigBones = new Dictionary<string, HashSet<string>>();

        public IReadOnlyList<string> FindUnknownBones(ModelArtifact rig, ModelArtifact part)
        {
            var partBones = ReadJoints(part.ModelPath);
            var rigBones = ReadRigBones(rig);

            if (partBones == null || rigBones == null)
            {
                return NoBones;
            }

            return partBones.Where(bone => !rigBones.Contains(bone)).ToList();
        }

        private static IReadOnlyList<string> ReadJoints(string glbPath)
        {
            using var stream = File.OpenRead(glbPath);
            var document = GltfReader.Read(stream).Document;
            var skin = document.Skins?.FirstOrDefault();

            return skin?.Joints.Select(joint => document.Nodes[joint].Name).ToList();
        }

        private HashSet<string> ReadRigBones(ModelArtifact rig)
        {
            if (_rigBones.TryGetValue(rig.ModelPath, out var known))
            {
                return known;
            }

            var joints = ReadJoints(rig.ModelPath);

            if (joints == null)
            {
                Log.Warning($"rig '{rig.Name}' carries no skin, convert it again to check its parts");
            }

            var bones = joints == null ? null : new HashSet<string>(joints);

            _rigBones[rig.ModelPath] = bones;

            return bones;
        }
    }
}
