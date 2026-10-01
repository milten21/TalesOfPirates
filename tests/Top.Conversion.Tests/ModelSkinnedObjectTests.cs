using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Conversion.Models.Gltf;
using Top.Conversion.Gltf;
using Top.Legacy.MindPower.Animation;
using Top.Legacy.MindPower.Geometry;

namespace Top.Conversion.Tests
{
    public class ModelSkinnedObjectTests
    {
        private static BoneAnimation MakeSkeleton()
        {
            return new BoneAnimation
            {
                FrameCount = 2,
                KeyType = BoneKeyType.Mat43,
                Bones =
                [
                    new Bone { Name = "root", Id = 0, ParentId = -1, InvBindMatrix = Matrix4x4.Identity },
                    new Bone
                    {
                        Name = "child", Id = 1, ParentId = 0,
                        InvBindMatrix = Matrix4x4.CreateTranslation(0f, 0f, -1f),
                    }
                ],
                Dummies =
                [
                    new BoneDummy
                    {
                        Id = 7, ParentBoneId = 1,
                        Matrix = Matrix4x4.CreateTranslation(1f, 0f, 0f),
                    }
                ],
                Tracks =
                [
                    new MatrixBoneTrack
                    {
                        Frames = [Matrix4x4.Identity, Matrix4x4.Identity],
                    },
                    new MatrixBoneTrack
                    {
                        Frames =
                        [
                            Matrix4x4.CreateTranslation(0f, 0f, 1f),
                            Matrix4x4.CreateTranslation(0f, 0f, 2f)
                        ],
                    }
                ],
            };
        }

        private static Mesh MakeMesh()
        {
            return new Mesh
            {
                PointType = 4,
                Vertices =
                [
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0)
                ],
                Indices = [0, 1, 2],
                Subsets = [new MeshSubset { PrimitiveCount = 1, StartIndex = 0 }],
                SkinBlends =
                [
                    new SkinBlend { BoneIndex = 0x00000100, Weight0 = 0.75f, Weight1 = 0.25f },
                    new SkinBlend { BoneIndex = 0x00000000, Weight0 = 1f },
                    new SkinBlend { BoneIndex = 0x00000001, Weight0 = 1f }
                ],
                BoneIndices = [0, 1],
            };
        }

        private static SceneModel MakeModel(bool skinned = true)
        {
            var animated = new GeometryObject
            {
                Id = 1,
                ParentId = uint.MaxValue,
                LocalMatrix = Matrix4x4.Identity,
                Mesh = MakeMesh(),
                Animation = new AnimationData { Bone = skinned ? MakeSkeleton() : null },
            };

            var stat = new GeometryObject
            {
                Id = 0,
                ParentId = uint.MaxValue,
                LocalMatrix = Matrix4x4.Identity,
                Mesh = new Mesh
                {
                    PointType = 4,
                    Vertices = [new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0)],
                    Indices = [0, 1, 2],
                    Subsets = [new MeshSubset { PrimitiveCount = 1, StartIndex = 0 }],
                },
            };

            return new SceneModel
            {
                Version = 4100,
                GeometryObjects = [stat, animated],
                Helpers = [],
            };
        }

        [Test]
        public void Skinned_objects_get_a_skin_and_a_skeleton_subtree()
        {
            var file = GltfExport.Model("prop", MakeModel());
            var doc = file.Document;
            var meshNode = doc.Nodes.Find(n => n.Name == "geom_1");
            var container = doc.Nodes.FindIndex(n => n.Name == "geom_1_skeleton");

            Assert.That(container, Is.GreaterThanOrEqualTo(0));
            Assert.That(doc.Scenes[0].Nodes, Does.Contain(container));
            Assert.That(meshNode.Skin, Is.EqualTo(0));
            Assert.That(meshNode.Matrix, Is.Null);

            var rootBone = doc.Nodes[container].Children.Single();

            Assert.That(doc.Nodes[rootBone].Name, Is.EqualTo("root"));
            Assert.That(doc.Skins, Has.Count.EqualTo(1));
            Assert.That(doc.Skins[0].Skeleton, Is.EqualTo(rootBone));
            Assert.That(doc.Skins[0].Joints, Has.Count.EqualTo(2));
            Assert.That(doc.Scenes[0].Nodes, Does.Not.Contain(rootBone));
        }

        [Test]
        public void Bone_clips_target_the_skeleton()
        {
            var file = GltfExport.Model("prop", MakeModel());
            var doc = file.Document;
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = file.BinChunk });
            var clip = doc.Animations.Single(a => a.Name == "geom_1");
            var joints = doc.Skins[0].Joints;

            Assert.That(clip.Channels, Has.Count.EqualTo(4));
            Assert.That(clip.Channels.Select(c => c.Target.Node.Value).Distinct().OrderBy(n => n),
                Is.EqualTo(joints.OrderBy(n => n)));
            Assert.That(data.ReadFloats(clip.Samplers[0].Input),
                Is.EqualTo(new[] { 0f, 1f / 30f }));

            var channel = clip.Channels
                .Single(c => c.Target.Node == joints[1] && c.Target.Path == "translation");

            // Child track position at frame 1 is (0, 0, 2); the axis swap
            // puts the translation in glTF y.
            Assert.That(data.ReadFloats(clip.Samplers[channel.Sampler].Output),
                Is.EqualTo(new[] { 0f, 1f, 0f, 0f, 2f, 0f }));
        }

        [Test]
        public void Object_with_both_bone_and_matrix_animation_emits_only_the_bone_clip()
        {
            var model = MakeModel();

            model.GeometryObjects[1].Animation.Matrix = new MatrixAnimation
            {
                Frames = [Matrix4x4.Identity, Matrix4x4.CreateTranslation(1f, 0f, 0f)],
            };

            var file = GltfExport.Model("prop", model);
            var doc = file.Document;
            var clips = doc.Animations.Where(a => a.Name == "geom_1").ToList();
            var joints = doc.Skins[0].Joints;

            Assert.That(clips, Has.Count.EqualTo(1));
            Assert.That(clips[0].Channels, Has.Count.EqualTo(4), "2 bones, translation and rotation");
            Assert.That(clips[0].Channels.Select(c => c.Target.Node.Value).Distinct().OrderBy(n => n),
                Is.EqualTo(joints.OrderBy(n => n)));
        }

        [Test]
        public void Bone_dummies_hang_off_their_bone()
        {
            var doc = GltfExport.Model("prop", MakeModel()).Document;
            var dummy = doc.Nodes.FindIndex(n => n.Name == "dummy_7");
            var childBone = doc.Skins[0].Joints[1];

            Assert.That(dummy, Is.GreaterThanOrEqualTo(0));
            Assert.That(doc.Nodes[childBone].Children, Does.Contain(dummy));
            Assert.That(doc.Scenes[0].Nodes, Does.Not.Contain(dummy));

            // Bone 1's InvBindMatrix is T(0,0,-1) and the dummy's matrix is
            // T(1,0,0); the dummy's local transform under its bone is
            // T(1,0,0) * T(0,0,-1) = T(1,0,-1) in source space, which the
            // axis swap turns into glTF translation (1,-1,0).
            var matrix = doc.Nodes[dummy].Matrix;

            Assert.That(matrix, Is.Not.Null);
            Assert.That(new[] { matrix[12], matrix[13], matrix[14] },
                Is.EqualTo(new[] { 1f, -1f, 0f }));
        }

        [Test]
        public void Local_matrix_on_a_skinned_object_is_ignored()
        {
            var model = MakeModel();
            model.GeometryObjects[1].LocalMatrix = Matrix4x4.CreateTranslation(5f, 0f, 0f);

            var file = GltfExport.Model("prop", model);
            var meshNode = file.Document.Nodes.Find(n => n.Name == "geom_1");

            Assert.That(meshNode.Matrix, Is.Null);
            Assert.That(meshNode.Translation, Is.Null);
        }

        [Test]
        public void Parent_of_a_skinned_object_is_ignored()
        {
            var model = MakeModel();
            model.GeometryObjects[1].ParentId = model.GeometryObjects[0].Id;

            var file = GltfExport.Model("prop", model);
            var doc = file.Document;
            var statNode = doc.Nodes.FindIndex(n => n.Name == "geom_0");
            var meshNode = doc.Nodes.FindIndex(n => n.Name == "geom_1");

            Assert.That(doc.Nodes[statNode].Children, Does.Contain(meshNode));
        }

        [Test]
        public void Converts_by_bd005_with_its_embedded_skeleton()
        {
            SceneModel model;
            using (var stream = File.OpenRead(Fixtures.Path("lmo/by-bd005.lmo")))
            {
                model = LmoFile.Read(stream).Model;
            }

            var file = GltfExport.Model("by-bd005", model);
            var doc = file.Document;
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = file.BinChunk });
            var container = doc.Nodes.FindIndex(n => n.Name == "geom_4_skeleton");

            Assert.That(doc.Skins, Has.Count.EqualTo(1));
            Assert.That(doc.Skins[0].Name, Is.EqualTo("geom_4"));
            Assert.That(doc.Skins[0].Joints, Has.Count.EqualTo(5));
            Assert.That(container, Is.GreaterThanOrEqualTo(0));
            Assert.That(doc.Nodes.Find(n => n.Name == "geom_4").Skin, Is.EqualTo(0));
            Assert.That(doc.Nodes.Exists(n => n.Name == "dummy_0"), Is.True);

            Assert.That(doc.Animations.Select(a => a.Name),
                Is.EquivalentTo(new[] { "geom_4", "geom_10", "geom_11" }));

            var clip = doc.Animations.Single(a => a.Name == "geom_4");

            Assert.That(clip.Channels, Has.Count.EqualTo(12),
                "5 bones, translation and rotation, and scale on Bone01 and Bone02, which squash to 0.84 and 0.75");
            Assert.That(data.ReadFloats(clip.Samplers[0].Input), Has.Length.EqualTo(101));
        }
    }
}
