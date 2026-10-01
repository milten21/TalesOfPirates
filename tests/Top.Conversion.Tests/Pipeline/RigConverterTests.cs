using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using NUnit.Framework;
using Top.Conversion.Models.Gltf;
using Top.Conversion.Pipeline;
using Top.Conversion.Gltf;
using Top.Legacy.MindPower.Animation;
using Top.Legacy.Tables;
using Top.Legacy.Tables.Custom;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Tests.Pipeline
{
    public class RigConverterTests
    {
        private FakeClient _client;
        private CaptureLog _log;

        [SetUp]
        public void SetUp()
        {
            _client = new FakeClient();
            _log = new CaptureLog();
        }

        [TearDown]
        public void TearDown()
        {
            _log.Dispose();
            _client.Dispose();
        }

        /// <summary>
        /// Action set 7 plays frames 0 to 10 in waiting, in show, and in waiting
        /// with a sword but another key frame. Sleep plays frames 5 to 10.
        /// </summary>
        private const string ActionRows = "7\n\t1 0 10\n\t2 0 10\n\t55 0 10 5\n\t3 5 10\n";

        /// <summary>
        /// Action set 1 plays body type 0's waiting over frames 2 to 59, all
        /// that the fixture keeps of its timeline.
        /// </summary>
        private const string WaitingRows = "1\n\t1 2 59\n";

        private const int WaitingStart = 2;

        private const float RestTolerance = 1e-4f;

        /// <summary>
        /// The largest matrix entry, in metres per metre from the joint, by
        /// which the fixture's joints miss the original's matrices at a key.
        /// One node per bone cannot hold the stretch off the bone's axes that
        /// a key longer than one gives, nor the difference between a sheared
        /// .lab bind and the rest the skin binds. Measured 0.372.
        /// </summary>
        private const float KeyTolerance = 0.38f;

        private const float BlendSafeTurn = 90f;

        private static CharacterInfoRecord Character(int id, int actionId,
            CharacterModalType modal = CharacterModalType.MainCharacter, int model = 1)
        {
            return new CharacterInfoRecord
            {
                Id = id,
                Name = $"character {id}",
                Model = model,
                ActionId = actionId,
                ModalType = modal,
                Parts = new int[5],
            };
        }

        private RigConverter Converter(bool overwrite = true, params CharacterInfoRecord[] characters)
        {
            return Converter(ActionRows, overwrite, characters);
        }

        private RigConverter Converter(string actionRows, bool overwrite, params CharacterInfoRecord[] characters)
        {
            using var actions = new MemoryStream(Encoding.ASCII.GetBytes(actionRows));
            var tables = new ClientTables(
                new Table<CharacterInfoRecord>([..characters]),
                null, null, CharacterActionTable.Read(actions));

            return new RigConverter(_client.Settings(overwrite), tables);
        }

        private static GltfFile Read(string path)
        {
            using var stream = File.OpenRead(path);

            return GltfReader.Read(stream);
        }

        private RigArtifacts ConvertBodyType0()
        {
            _client.AddSkeleton("lab/0000-waiting.lab", "0000.lab");

            return Converter(WaitingRows, true, Character(1, actionId: 1, model: 0)).Convert(0);
        }

        private static BoneAnimation BodyType0()
        {
            using var stream = File.OpenRead(Fixtures.Path("lab/0000-waiting.lab"));

            return LabFile.Read(stream).Animation;
        }

        /// <summary>
        /// lwAnimCtrlBone::_BuildRunTimeBoneMatrix at one frame: each key
        /// through lwQuaternionToMatrix44 with its position, times its
        /// parent's, then the inverse bind in front. In glTF axes.
        /// </summary>
        private static Matrix4x4[] OriginalSkin(BoneAnimation skeleton, int frame)
        {
            var world = new Matrix4x4[skeleton.Bones.Length];

            for (var i = 0; i < world.Length; i++)
            {
                var track = (QuaternionBoneTrack)skeleton.Tracks[i];
                var key = Matrix4x4.CreateFromQuaternion(track.Rotations[frame]);
                var parent = skeleton.Bones[i].ParentId;

                key.Translation = track.Positions[frame];
                world[i] = parent < 0 ? key : key * world[parent];
            }

            return world.Select((matrix, i) => Axis.ToGltf(skeleton.Bones[i].InvBindMatrix * matrix)).ToArray();
        }

        private static float Largest(Matrix4x4 difference)
        {
            var largest = 0f;

            for (var i = 0; i < 4; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    largest = MathF.Max(largest, MathF.Abs(difference[i, j]));
                }
            }

            return largest;
        }

        [Test]
        public void Converts_a_skeleton_into_a_rig_without_clips()
        {
            _client.AddSkeleton("lab/0001.lab");

            var rig = Converter(true, Character(10, actionId: 7)).Convert(1).Rig;

            Assert.That(rig.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(rig.Kind, Is.EqualTo("character"));
            Assert.That(rig.ModelPath, Is.EqualTo(_client.ConvertedRig("0001")));
            Assert.That(Read(rig.ModelPath).Document.Animations, Is.Null);
        }

        [Test]
        public void Each_frame_range_the_main_characters_play_becomes_one_clip_file()
        {
            _client.AddSkeleton("lab/0001.lab");

            var clips = Converter(true, Character(10, actionId: 7)).Convert(1).Clips;

            Assert.That(clips.Select(clip => clip.ModelPath), Is.EqualTo(new[]
            {
                _client.ConvertedClip("0001", "01_waiting"),
                _client.ConvertedClip("0001", "03_sleep"),
            }), "show and the sword waiting play the waiting range");
            Assert.That(clips.Select(clip => clip.Outcome), Is.All.EqualTo(ConversionOutcome.Converted));
            Assert.That(Read(clips[1].ModelPath).Document.Animations.Single().Name, Is.EqualTo("03_sleep"));
        }

        [Test]
        public void The_clips_come_from_the_action_set_of_the_main_character_rows_alone()
        {
            _client.AddSkeleton("lab/0001.lab");

            var clips = Converter(true, Character(5, actionId: 9, CharacterModalType.Boat), Character(10, actionId: 7))
                .Convert(1).Clips;

            Assert.That(clips, Has.Count.EqualTo(2));
            Assert.That(_log.Warnings, Has.None.Contains("action set"));
        }

        [Test]
        public void Converting_again_writes_the_same_bytes()
        {
            _client.AddSkeleton("lab/0001.lab");

            var first = Converter(true, Character(10, actionId: 7)).Convert(1);
            var rig = File.ReadAllBytes(first.Rig.ModelPath);
            var clip = File.ReadAllBytes(first.Clips[0].ModelPath);

            var second = Converter(true, Character(10, actionId: 7)).Convert(1);

            Assert.That(second.Rig.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(File.ReadAllBytes(second.Rig.ModelPath), Is.EqualTo(rig));
            Assert.That(File.ReadAllBytes(second.Clips[0].ModelPath), Is.EqualTo(clip));
        }

        [Test]
        public void Characters_disagreeing_on_the_action_set_are_reported_and_the_first_wins()
        {
            _client.AddSkeleton("lab/0001.lab");

            Converter(true, Character(10, actionId: 7), Character(11, actionId: 9)).Convert(1);

            Assert.That(_log.Warnings, Has.Some.Contains("action set 9"));
            Assert.That(_log.Warnings, Has.Some.Contains("keeping 7"));
        }

        [Test]
        public void A_skeleton_no_main_character_uses_converts_without_clips()
        {
            _client.AddSkeleton("lab/0001.lab");

            var artifacts = Converter(true, Character(10, actionId: 7, CharacterModalType.Boat)).Convert(1);

            Assert.That(artifacts.Rig.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(artifacts.Clips, Is.Empty);
            Assert.That(_log.Warnings, Has.Some.Contains("no main character row uses model 1"));
        }

        [Test]
        public void A_main_character_with_no_action_set_converts_without_clips()
        {
            _client.AddSkeleton("lab/0001.lab");

            var artifacts = Converter(true, Character(10, actionId: 8)).Convert(1);

            Assert.That(artifacts.Rig.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(artifacts.Clips, Is.Empty);
            Assert.That(_log.Warnings, Has.Some.Contains("rig 0001 has no action set"));
        }

        [Test]
        public void A_skeleton_file_that_is_not_named_after_a_model_still_converts()
        {
            var path = _client.AddSkeleton("lab/0001.lab", "hero.lab");

            var artifacts = Converter().Convert(path);

            Assert.That(artifacts.Rig.Name, Is.EqualTo("hero"));
            Assert.That(_log.Warnings, Has.Some.Contains("'hero' is not a skeleton model id"));
        }

        [Test]
        public void A_missing_skeleton_fails()
        {
            Assert.That(Converter().Convert(1), Is.Null);
            Assert.That(_log.Errors, Has.Some.Contains("0001.lab"));
        }

        [Test]
        public void Output_already_in_place_is_skipped()
        {
            _client.AddSkeleton("lab/0001.lab");
            Converter(true, Character(10, actionId: 7)).Convert(1);

            var artifacts = Converter(false, Character(10, actionId: 7)).Convert(1);

            Assert.That(artifacts.Rig.Outcome, Is.EqualTo(ConversionOutcome.Skipped));
            Assert.That(artifacts.Clips.Select(clip => clip.Outcome), Is.EqualTo(new[]
            {
                ConversionOutcome.Skipped, ConversionOutcome.Skipped,
            }));
        }

        [Test]
        public void A_skeleton_named_twice_in_one_run_converts_once()
        {
            _client.AddSkeleton("lab/0001.lab");
            var converter = Converter();

            converter.Convert(1);

            Assert.That(converter.Convert(1).Rig.Outcome, Is.EqualTo(ConversionOutcome.Skipped));
        }

        [Test]
        public void Every_bone_of_a_body_type_moves_in_its_clips()
        {
            var clip = Read(ConvertBodyType0().Clips.Single().ModelPath).Document;
            var moved = clip.Animations.Single().Channels.Select(channel => channel.Target.Node.Value).ToHashSet();

            Assert.That(clip.Skins.Single().Joints.Where(joint => !moved.Contains(joint))
                .Select(joint => clip.Nodes[joint].Name), Is.Empty);
        }

        [Test]
        public void A_body_type_rests_at_its_bind_pose()
        {
            var skin = new ConvertedSkin(Read(ConvertBodyType0().Rig.ModelPath)).At(null);

            Assert.That(skin.Select(matrix => Largest(matrix - Matrix4x4.Identity)).Max(), Is.LessThan(RestTolerance),
                "each joint at rest, times its inverse bind, leaves every vertex where it is");
        }

        [Test]
        public void A_body_type_plays_each_key_within_the_recorded_tolerance_of_the_original_matrix_math()
        {
            var skeleton = BodyType0();
            var clip = new ConvertedSkin(Read(ConvertBodyType0().Clips.Single().ModelPath));
            var largest = 0f;

            for (var frame = 0; frame < clip.FrameCount; frame++)
            {
                var original = OriginalSkin(skeleton, WaitingStart + frame);
                var converted = clip.At(frame);

                for (var bone = 0; bone < original.Length; bone++)
                {
                    largest = MathF.Max(largest, Largest(converted[bone] - original[bone]));
                }
            }

            Assert.That(largest, Is.LessThan(KeyTolerance));
        }

        [Test]
        public void No_key_turns_a_bone_near_half_a_turn_from_its_rest_as_godot_reads_it()
        {
            var file = Read(ConvertBodyType0().Clips.Single().ModelPath);
            var data = new GltfData(file);
            var animation = file.Document.Animations.Single();
            var turns = new Dictionary<string, float>();

            foreach (var channel in animation.Channels.Where(channel => channel.Target.Path == "rotation"))
            {
                var node = file.Document.Nodes[channel.Target.Node.Value];
                var rest = EngineRestRotation(node);
                var keys = data.ReadFloats(animation.Samplers[channel.Sampler].Output);

                turns[node.Name] = Enumerable.Range(0, keys.Length / 4)
                    .Select(key => AngleBetween(rest, Rotation(keys, key * 4)))
                    .Max();
            }

            Assert.That(turns.Where(turn => turn.Value >= BlendSafeTurn).Select(turn => turn.Key), Is.Empty);
        }

        private static Quaternion EngineRestRotation(GltfNode node)
        {
            var scale = node.Scale ?? [1f, 1f, 1f];
            var rest = Matrix4x4.CreateScale(scale[0], scale[1], scale[2])
                       * Matrix4x4.CreateFromQuaternion(Rotation(node.Rotation ?? [0f, 0f, 0f, 1f], 0));

            if (rest.GetDeterminant() < 0f)
            {
                rest = Matrix4x4.CreateScale(-1f) * rest;
            }

            Matrix4x4.Decompose(rest, out _, out var rotation, out _);

            return rotation;
        }

        private static Quaternion Rotation(float[] values, int offset)
        {
            return new Quaternion(values[offset], values[offset + 1], values[offset + 2], values[offset + 3]);
        }

        private static float AngleBetween(Quaternion from, Quaternion to)
        {
            var dot = MathF.Min(MathF.Abs(Quaternion.Dot(from, to)), 1f);

            return float.RadiansToDegrees(2f * MathF.Acos(dot));
        }

        [Test]
        public void Converting_a_body_type_with_sheared_bones_again_writes_the_same_bytes()
        {
            var first = ConvertBodyType0();
            var rig = File.ReadAllBytes(first.Rig.ModelPath);
            var clip = File.ReadAllBytes(first.Clips.Single().ModelPath);

            var second = ConvertBodyType0();

            Assert.That(File.ReadAllBytes(second.Rig.ModelPath), Is.EqualTo(rig));
            Assert.That(File.ReadAllBytes(second.Clips.Single().ModelPath), Is.EqualTo(clip));
        }
    }
}
