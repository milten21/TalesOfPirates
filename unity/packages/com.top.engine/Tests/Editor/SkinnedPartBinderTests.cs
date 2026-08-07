using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Top.Engine.Tests
{
    public class SkinnedPartBinderTests
    {
        [Test]
        public void Maps_every_bone_by_name_keeping_the_first_of_a_repeat()
        {
            var rigRoot = new GameObject("rig").transform;
            var first = new GameObject("shared").transform;
            var second = new GameObject("shared").transform;
            var only = new GameObject("only").transform;
            first.SetParent(rigRoot);
            second.SetParent(rigRoot);
            only.SetParent(first);

            try
            {
                LogAssert.Expect(LogType.Warning, new Regex("more than one node named 'shared'"));

                var bones = SkinnedPartBinder.MapBones(rigRoot);

                Assert.That(bones["rig"], Is.EqualTo(rigRoot));
                Assert.That(bones["shared"], Is.EqualTo(first));
                Assert.That(bones["only"], Is.EqualTo(only));
            }
            finally
            {
                Object.DestroyImmediate(rigRoot.gameObject);
            }
        }

        [Test]
        public void Binds_bones_by_name_with_root_fallback()
        {
            var rigRoot = new GameObject("rig").transform;
            var a = new GameObject("boneA").transform;
            var b = new GameObject("boneB").transform;
            a.SetParent(rigRoot);
            b.SetParent(rigRoot);

            var bones = new Dictionary<string, Transform>
            {
                ["rig"] = rigRoot,
                ["boneA"] = a,
                ["boneB"] = b,
            };

            var partGo = new GameObject("part");
            var sourceA = new GameObject("boneA").transform;
            var stray = new GameObject("missing").transform;
            sourceA.SetParent(partGo.transform);
            stray.SetParent(partGo.transform);

            var source = partGo.AddComponent<SkinnedMeshRenderer>();
            source.bones = new[] { sourceA, stray };
            source.rootBone = sourceA;

            try
            {
                LogAssert.Expect(LogType.Warning, new Regex("no bone named 'missing'"));

                var target = SkinnedPartBinder.Bind(source, rigRoot, bones);

                Assert.That(target.bones[0], Is.EqualTo(a));
                Assert.That(target.bones[1], Is.EqualTo(rigRoot));
                Assert.That(target.rootBone, Is.EqualTo(a));
                Assert.That(target.transform.parent, Is.EqualTo(rigRoot));
                Assert.That(target.sharedMesh, Is.EqualTo(source.sharedMesh));
            }
            finally
            {
                Object.DestroyImmediate(partGo);
                Object.DestroyImmediate(rigRoot.gameObject);
            }
        }
    }
}
