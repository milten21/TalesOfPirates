using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Conversion.Models;
using Top.Conversion.Models.Characters;
using Top.Conversion.Models.Gltf;
using Top.Conversion.Gltf;
using Top.Legacy.MindPower.Animation;
using Top.Legacy.MindPower.Geometry;
using Top.Legacy.Tables.Custom;

namespace Top.Conversion.Tests
{
    public class RigTests
    {
        /// <summary>
        /// The child's inverse bind is T(0,0,-1) with 0.5 of y added to x, so
        /// its bind carries shear.
        /// </summary>
        private static BoneAnimation MakeShearedSkeleton()
        {
            var skeleton = MakeSkeleton();
            var inverseBind = Matrix4x4.CreateTranslation(0f, 0f, -1f);

            inverseBind.M21 = 0.5f;
            skeleton.Bones[1].InvBindMatrix = inverseBind;

            return skeleton;
        }

        private static BoneAnimation MakeSkeleton()
        {
            return new BoneAnimation
            {
                FrameCount = 2,
                KeyType = BoneKeyType.Quat,
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
                        Id = 9, ParentBoneId = 1,
                        Matrix = Matrix4x4.CreateTranslation(1f, 0f, 0f),
                    }
                ],
                Tracks =
                [
                    new QuaternionBoneTrack
                    {
                        Positions = [Vector3.Zero, Vector3.Zero],
                        Rotations = [Quaternion.Identity, Quaternion.Identity],
                    },
                    new QuaternionBoneTrack
                    {
                        Positions = [new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 2f)],
                        Rotations = [Quaternion.Identity, Quaternion.Identity],
                    }
                ],
            };
        }

        private static GltfFile Clip(BoneAnimation skeleton, int startFrame, int endFrame, string name = "clip")
        {
            return GltfExport.Clip(name, skeleton, BoneTracks.Convert(skeleton), startFrame, endFrame);
        }

        /// <summary>
        /// A node's local transform in row-vector form: scale, then rotation,
        /// then translation.
        /// </summary>
        private static Matrix4x4 Local(GltfNode node)
        {
            if (node.Matrix is { } matrix)
            {
                return new Matrix4x4(
                    matrix[0], matrix[1], matrix[2], matrix[3],
                    matrix[4], matrix[5], matrix[6], matrix[7],
                    matrix[8], matrix[9], matrix[10], matrix[11],
                    matrix[12], matrix[13], matrix[14], matrix[15]);
            }

            var translation = node.Translation ?? [0f, 0f, 0f];
            var rotation = node.Rotation ?? [0f, 0f, 0f, 1f];
            var scale = node.Scale ?? [1f, 1f, 1f];

            return Matrix4x4.CreateScale(scale[0], scale[1], scale[2])
                   * Matrix4x4.CreateFromQuaternion(new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]))
                   * Matrix4x4.CreateTranslation(translation[0], translation[1], translation[2]);
        }

        private static IEnumerable<string> Paths(GltfDocument document, GltfNode node)
        {
            var index = document.Nodes.IndexOf(node);

            return document.Animations.Single().Channels
                .Where(channel => channel.Target.Node == index)
                .Select(channel => channel.Target.Path);
        }

        /// <summary>
        /// A node's local transform at one frame of the file's clip: its rest,
        /// with each property a channel animates taken from the channel.
        /// </summary>
        private static Matrix4x4 Key(GltfFile file, GltfNode node, int frame)
        {
            return Local(new GltfNode
            {
                Translation = Channel(file, node, "translation", frame) ?? node.Translation,
                Rotation = Channel(file, node, "rotation", frame) ?? node.Rotation,
                Scale = Channel(file, node, "scale", frame) ?? node.Scale,
            });
        }

        private static float[] Channel(GltfFile file, GltfNode node, string path, int frame)
        {
            var document = file.Document;
            var index = document.Nodes.IndexOf(node);
            var channel = document.Animations.Single().Channels
                .SingleOrDefault(channel => channel.Target.Node == index && channel.Target.Path == path);

            if (channel == null)
            {
                return null;
            }

            var output = new GltfData(file).ReadFloats(document.Animations.Single().Samplers[channel.Sampler].Output);
            var width = path == "rotation" ? 4 : 3;

            return output.Skip(frame * width).Take(width).ToArray();
        }

        private static float[] InverseBinds(GltfFile file)
        {
            return new GltfData(file).ReadFloats(file.Document.Skins.Single().InverseBindMatrices.Value);
        }

        private static float[] Floats(Matrix4x4 matrix)
        {
            return
            [
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44,
            ];
        }

        [Test]
        public void A_rig_carries_no_clips()
        {
            var file = GltfExport.Rig("rig", MakeSkeleton());

            Assert.That(file.Document.Animations, Is.Null);
        }

        [Test]
        public void A_clip_is_one_animation_named_after_the_clip()
        {
            var file = Clip(MakeSkeleton(), 0, 1, "05_run");

            Assert.That(file.Document.Animations.Select(animation => animation.Name), Is.EqualTo(new[] { "05_run" }));
        }

        [Test]
        public void A_clip_carries_the_skeleton_and_skin_of_its_rig_and_no_dummy_or_mesh()
        {
            var rig = GltfExport.Rig("rig", MakeSkeleton());
            var clip = Clip(MakeSkeleton(), 0, 1);
            var document = clip.Document;
            var rigData = new GltfData(new GltfFile { Document = rig.Document, BinChunk = rig.BinChunk });
            var clipData = new GltfData(new GltfFile { Document = document, BinChunk = clip.BinChunk });

            Assert.That(document.Nodes.Select(node => node.Name), Is.EqualTo(new[] { "root", "child" }));
            Assert.That(document.Nodes.Select(node => (node.Translation, node.Rotation, node.Scale, node.Matrix)),
                Is.EqualTo(rig.Document.Nodes.Take(2).Select(node => (node.Translation, node.Rotation, node.Scale,
                    node.Matrix))), "the bones rest where the rig's bones rest");
            Assert.That(document.Skins.Single().Joints.Select(joint => document.Nodes[joint].Name),
                Is.EqualTo(new[] { "root", "child" }));
            Assert.That(clipData.ReadFloats(document.Skins[0].InverseBindMatrices.Value),
                Is.EqualTo(rigData.ReadFloats(rig.Document.Skins[0].InverseBindMatrices.Value)));
            Assert.That(document.Meshes, Is.Null);
        }

        [Test]
        public void Clip_times_rebase_to_zero()
        {
            var file = Clip(MakeSkeleton(), 1, 1);
            var data = new GltfData(new GltfFile
            {
                Document = file.Document,
                BinChunk = file.BinChunk,
            });

            var times = data.ReadFloats(file.Document.Animations[0].Samplers[0].Input);

            Assert.That(times, Is.EqualTo(new[] { 0f }));
        }

        [Test]
        public void Clips_sample_the_action_range()
        {
            // Child track position at frame 1 is (0, 0, 2); the axis swap
            // puts the translation in glTF y.
            var file = Clip(MakeSkeleton(), 1, 1);
            var doc = file.Document;
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = file.BinChunk });
            var channel = doc.Animations[0].Channels
                .First(c => c.Target.Node == 1 && c.Target.Path == "translation");

            var values = data.ReadFloats(doc.Animations[0].Samplers[channel.Sampler].Output);

            Assert.That(values, Is.EqualTo(new[] { 0f, 2f, 0f }));
        }

        [Test]
        public void Single_position_quaternion_tracks_hold_the_position()
        {
            // Old-format .lab files store one position for non-root bones.
            var skeleton = MakeSkeleton();
            ((QuaternionBoneTrack)skeleton.Tracks[1]).Positions =
                [new Vector3(0f, 0f, 3f)];

            var file = Clip(skeleton, 0, 1);
            var doc = file.Document;
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = file.BinChunk });
            var channel = doc.Animations[0].Channels
                .First(c => c.Target.Node == 1 && c.Target.Path == "translation");

            var values = data.ReadFloats(doc.Animations[0].Samplers[channel.Sampler].Output);

            Assert.That(values, Is.EqualTo(new[] { 0f, 3f, 0f, 0f, 3f, 0f }));
        }

        [Test]
        public void A_quaternion_key_longer_than_one_turns_further_and_stretches_across_its_axis()
        {
            // lwQuaternionToMatrix44 ignores the key's length: (0, 0, 1, 1)
            // gives the rows (-1, 2, 0) and (-2, -1, 0), a turn of 116.6
            // degrees about z with a stretch of sqrt(5) across it. The axis
            // swap moves z to glTF y. The stretch lies on the bone's own axes,
            // so one node holds the key exactly.
            var skeleton = MakeSkeleton();
            ((QuaternionBoneTrack)skeleton.Tracks[1]).Rotations =
                [new Quaternion(0f, 0f, 1f, 1f), new Quaternion(0f, 0f, 1f, 1f)];

            var file = Clip(skeleton, 0, 1);
            var child = file.Document.Nodes.Single(node => node.Name == "child");

            Assert.That(Floats(Key(file, child, 0)), Is.EqualTo(new[]
            {
                -1f, 0f, 2f, 0f,
                0f, 1f, 0f, 0f,
                -2f, 0f, -1f, 0f,
                0f, 1f, 0f, 1f,
            }).Within(1e-5f));
        }

        [Test]
        public void A_mirrored_bind_rests_as_godot_splits_it_and_its_keys_keep_its_turn()
        {
            // The child's bind mirrors y, glTF z. Godot splits a mirrored rest
            // into a scale negative on all three axes and half a turn about z.
            // Its keys are unturned and do not mirror.
            var skeleton = MakeSkeleton();
            skeleton.Bones[1].InvBindMatrix = Matrix4x4.CreateScale(1f, -1f, 1f)
                                              * Matrix4x4.CreateTranslation(0f, 0f, -1f);

            var file = Clip(skeleton, 0, 1);
            var child = file.Document.Nodes.Single(node => node.Name == "child");

            Assert.That(child.Rotation.Select(MathF.Abs), Is.EqualTo(new[] { 0f, 0f, 1f, 0f }).Within(1e-6f));
            Assert.That(child.Scale, Is.EqualTo(new[] { -1f, -1f, -1f }).Within(1e-6f));
            Assert.That(Channel(file, child, "rotation", 1), Is.EqualTo(child.Rotation).Within(1e-6f));
            Assert.That(Channel(file, child, "scale", 1), Is.EqualTo(new[] { -1f, -1f, 1f }).Within(1e-6f));
            Assert.That(Floats(Key(file, child, 1)), Is.EqualTo(Floats(Matrix4x4.CreateTranslation(0f, 2f, 0f)))
                .Within(1e-5f));
        }

        [Test]
        public void Clips_without_usable_tracks_are_skipped()
        {
            var skeleton = MakeSkeleton();
            skeleton.Tracks = null;

            var file = Clip(skeleton, 0, 1);

            Assert.That(file.Document.Animations, Is.Null);
        }

        [Test]
        public void A_bone_with_a_sheared_bind_rests_on_translation_rotation_and_scale_and_moves()
        {
            // glTF forbids channels on a node that holds a matrix.
            var file = Clip(MakeShearedSkeleton(), 0, 1);
            var child = file.Document.Nodes.Single(node => node.Name == "child");

            Assert.That(child.Matrix, Is.Null);
            Assert.That(Paths(file.Document, child), Is.SupersetOf(new[] { "translation", "rotation" }));
        }

        [Test]
        public void A_rig_with_a_sheared_bind_rests_where_its_skin_binds_it()
        {
            var skin = new ConvertedSkin(GltfExport.Rig("rig", MakeShearedSkeleton())).At(null);

            Assert.That(skin.Select(Floats), Has.All.EqualTo(Floats(Matrix4x4.Identity)).Within(1e-5f),
                "each joint at rest, times its inverse bind, leaves every vertex where it is");
        }

        [Test]
        public void A_part_on_a_sheared_skeleton_binds_the_rigs_inverse_binds()
        {
            var part = Fixtures.MakeObject(1, uint.MaxValue);

            part.Mesh.BoneIndices = [0, 1];
            part.Mesh.SkinBlends =
            [
                new SkinBlend { BoneIndex = 0x00000000, Weight0 = 1f },
                new SkinBlend { BoneIndex = 0x00000001, Weight0 = 1f },
                new SkinBlend { BoneIndex = 0x00000001, Weight0 = 1f }
            ];

            var rig = GltfExport.Rig("rig", MakeShearedSkeleton());
            var partFile = GltfExport.Object("part", part, MakeShearedSkeleton());

            Assert.That(InverseBinds(partFile), Is.EqualTo(InverseBinds(rig)));
        }

        [Test]
        public void A_dummy_on_a_sheared_bone_rests_at_its_own_matrix_as_the_mesh_does()
        {
            // The dummy's matrix is T(1,0,0) in the model, glTF (1,0,0) too.
            var document = GltfExport.Rig("rig", MakeShearedSkeleton()).Document;
            var root = document.Nodes.Single(node => node.Name == "root");
            var child = document.Nodes.Single(node => node.Name == "child");
            var dummy = document.Nodes.Single(node => node.Name == "dummy_9");

            Assert.That(Floats(Local(dummy) * Local(child) * Local(root)),
                Is.EqualTo(Floats(Matrix4x4.CreateTranslation(1f, 0f, 0f))).Within(1e-5f));
        }

        [Test]
        public void Matrix_tracks_decompose_to_channels()
        {
            var skeleton = MakeSkeleton();
            skeleton.KeyType = BoneKeyType.Mat43;
            skeleton.Tracks =
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
            ];

            var file = Clip(skeleton, 0, 1);
            var doc = file.Document;
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = file.BinChunk });
            var channel = doc.Animations[0].Channels
                .First(c => c.Target.Node == 1 && c.Target.Path == "translation");

            var values = data.ReadFloats(doc.Animations[0].Samplers[channel.Sampler].Output);

            Assert.That(values, Is.EqualTo(new[] { 0f, 1f, 0f, 0f, 2f, 0f }));
        }

        [Test]
        public void Bones_without_tracks_stay_in_bind_pose()
        {
            var skeleton = MakeSkeleton();
            skeleton.Tracks = [skeleton.Tracks[0]];

            var file = Clip(skeleton, 0, 1);
            var doc = file.Document;

            Assert.That(doc.Animations[0].Channels.Any(c => c.Target.Node == 1), Is.False);
            Assert.That(doc.Animations[0].Channels.Any(c => c.Target.Node == 0), Is.True);
        }

        [Test]
        public void Tracks_shorter_than_the_clip_warn()
        {
            var skeleton = MakeSkeleton();
            var track = (QuaternionBoneTrack)skeleton.Tracks[1];
            track.Positions = [Vector3.Zero];
            track.Rotations = [Quaternion.Identity];

            var file = Clip(skeleton, 0, 1);
            var doc = file.Document;

            Assert.That(doc.Animations[0].Channels.Any(c => c.Target.Node == 1), Is.False);
        }

        [Test]
        public void Rotation_keys_keep_hemisphere_continuity()
        {
            // The glTF turns are 230 and 250 degrees about x. Read from their
            // matrices, the first comes with a positive x and the second with
            // a positive w: a pair of opposite sign that would interpolate
            // the long way round.
            var skeleton = MakeSkeleton();
            ((QuaternionBoneTrack)skeleton.Tracks[1]).Rotations =
            [
                Quaternion.CreateFromAxisAngle(Vector3.UnitX, -230f * MathF.PI / 180f),
                Quaternion.CreateFromAxisAngle(Vector3.UnitX, -250f * MathF.PI / 180f)
            ];

            var file = Clip(skeleton, 0, 1);
            var doc = file.Document;
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = file.BinChunk });
            var channel = doc.Animations[0].Channels
                .First(c => c.Target.Node == 1 && c.Target.Path == "rotation");

            var values = data.ReadFloats(doc.Animations[0].Samplers[channel.Sampler].Output);

            Assert.That(values[0] * values[4] + values[1] * values[5] + values[2] * values[6] + values[3] * values[7],
                Is.GreaterThan(0f));
        }

        [Test]
        public void Skeleton_dummies_attach_to_bones()
        {
            var file = GltfExport.Rig("rig", MakeSkeleton());
            var doc = file.Document;

            var dummy = doc.Nodes.FindIndex(n => n.Name == "dummy_9");

            Assert.That(dummy, Is.GreaterThanOrEqualTo(0));
            Assert.That(doc.Nodes[1].Children, Does.Contain(dummy));
            Assert.That(doc.Scenes[0].Nodes, Does.Not.Contain(dummy));

            // Bone 1's InvBindMatrix is T(0,0,-1) and the dummy's matrix is
            // T(1,0,0); the dummy's local transform under its bone is
            // T(1,0,0) * T(0,0,-1) = T(1,0,-1) in source space, which the
            // axis swap turns into glTF translation (1,-1,0). Emitting the
            // model-space matrix as the local would instead read (1,0,0) and
            // place the dummy at the bone's bind pose twice over.
            var matrix = doc.Nodes[dummy].Matrix;

            Assert.That(matrix, Is.Not.Null);
            Assert.That(new[] { matrix[12], matrix[13], matrix[14] },
                Is.EqualTo(new[] { 1f, -1f, 0f }));
        }

        [Test]
        public void A_rig_carries_a_skin_of_its_bones_that_no_mesh_uses()
        {
            var file = GltfExport.Rig("rig", MakeSkeleton());
            var document = file.Document;
            var skin = document.Skins.Single();

            Assert.That(skin.Joints.Select(joint => document.Nodes[joint].Name),
                Is.EqualTo(new[] { "root", "child" }), "every bone is a joint and no dummy is");
            Assert.That(document.Nodes.Any(node => node.Skin != null), Is.False);
            Assert.That(document.Meshes, Is.Null);
        }

        [Test]
        public void The_rig_skin_binds_the_skeleton_at_its_lab_bind_pose()
        {
            // Bone 1's InvBindMatrix is T(0,0,-1) in source space; the axis
            // swap turns it into glTF translation (0,-1,0).
            var file = GltfExport.Rig("rig", MakeSkeleton());
            var data = new GltfData(new GltfFile { Document = file.Document, BinChunk = file.BinChunk });
            var matrices = data.ReadFloats(file.Document.Skins[0].InverseBindMatrices.Value);

            Assert.That(matrices, Has.Length.EqualTo(32));
            Assert.That(new[] { matrices[28], matrices[29], matrices[30] }, Is.EqualTo(new[] { 0f, -1f, 0f }));
        }

        [Test]
        public void Out_of_range_clips_clamp()
        {
            var file = Clip(MakeSkeleton(), 0, 9);
            var data = new GltfData(new GltfFile
            {
                Document = file.Document,
                BinChunk = file.BinChunk,
            });

            var times = data.ReadFloats(file.Document.Animations[0].Samplers[0].Input);

            Assert.That(times, Has.Length.EqualTo(2));
        }

        [Test]
        public void Converts_a_real_skeleton_into_a_rig_and_its_clips()
        {
            BoneAnimation skeleton;
            using (var stream = File.OpenRead(Fixtures.Path("lab/0001.lab")))
            {
                skeleton = LabFile.Read(stream).Animation;
            }

            CharacterActionTable table;
            using (var stream = File.OpenRead(Fixtures.Path("tables/CharacterAction.tx")))
            {
                table = CharacterActionTable.Read(stream);
            }

            // Skeleton 0001 belongs to action type 2 (characterinfo maps
            // framework 1 to Action ID 2).
            Assert.That(table.TryGetActions(2, out var actions), Is.True);

            var rig = GltfExport.Rig("0001", skeleton).Document;

            Assert.That(rig.Meshes, Is.Null, "empty arrays are invalid glTF");
            Assert.That(rig.Skins.Single().Joints, Has.Count.EqualTo(skeleton.Bones.Length));
            Assert.That(rig.Animations, Is.Null);

            var waiting = actions.Single(action => action.ActionNo == 1);
            var file = Clip(skeleton, waiting.StartFrame, waiting.EndFrame, "01_waiting");
            var data = new GltfData(new GltfFile { Document = file.Document, BinChunk = file.BinChunk });
            var times = data.ReadFloats(file.Document.Animations.Single().Samplers[0].Input);

            Assert.That(file.Document.Skins.Single().Joints, Has.Count.EqualTo(skeleton.Bones.Length));
            Assert.That(times, Has.Length.EqualTo(49), "frames 0..48 inclusive");
        }

        [Test]
        public void Packaging_a_rig_writes_a_glb_and_no_textures()
        {
            var outputDir = Path.Combine(Path.GetTempPath(), "top-rig-tests");

            try
            {
                var file = GltfExport.Rig("rig", MakeSkeleton());
                var result = new ModelPackaging(Path.Combine(outputDir, "rig.glb")).Write(file);

                Assert.That(result.ModelPath, Does.EndWith("rig.glb"));
                Assert.That(File.Exists(result.ModelPath), Is.True);
                Assert.That(result.TexturePaths, Is.Empty);
            }
            finally
            {
                if (Directory.Exists(outputDir))
                {
                    Directory.Delete(outputDir, recursive: true);
                }
            }
        }
    }
}
