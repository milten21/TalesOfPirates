using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Top.Conversion.Gltf;
using Top.Conversion.Pipeline;
using Top.Legacy.Tables;
using Top.Legacy.Tables.Custom;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Tests.Pipeline
{
    public class CharacterConverterTests
    {
        private const int PlayerModel = 1;
        private const int MonsterModel = 724;

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

        private static CharacterInfoRecord Character(int id, CharacterModalType modal, int model,
            params int[] parts)
        {
            // characterinfo carries eight part columns; only the first five
            // are equipment slots.
            var slots = new int[8];
            parts.CopyTo(slots, 0);

            return new CharacterInfoRecord
            {
                Id = id,
                Name = $"character {id}",
                ModalType = modal,
                Model = model,
                SuitId = 0,
                ActionId = 7,
                Parts = slots,
            };
        }

        private static ItemInfoRecord Wearable(int id, int framework, string module)
        {
            var columns = new[] { "0", "0", "0", "0", "0" };
            columns[framework + 1] = module;

            return new ItemInfoRecord
            {
                Id = id,
                Name = $"item {id}",
                Type = ItemType.Armor,
                Modules = columns,
                Body = [-1],
            };
        }

        private static ItemInfoRecord[] WornByTwoBodyTypes(int id, string module)
        {
            return [Wearable(id, PlayerModel, module), Wearable(id + 1, PlayerModel - 1, module)];
        }

        private static ItemInfoRecord MachinePart(int id, ItemType type, params string[] modules)
        {
            var columns = new[] { "0", "0", "0", "0", "0" };
            modules.CopyTo(columns, 0);

            return new ItemInfoRecord { Id = id, Name = $"item {id}", Type = type, Modules = columns };
        }

        private static ItemInfoRecord[] BoatItems =>
        [
            MachinePart(907, ItemType.Hull, "2000000000"),
            MachinePart(989, ItemType.Embolon, "2000000001"),
            MachinePart(1071, ItemType.Mobility, "2000000002", "1153"),
            MachinePart(1153, ItemType.Airscrew, "2000000004"),
        ];

        private void AddBoatParts()
        {
            foreach (var part in new[] { "2000000000", "2000000001", "2000000002", "2000000004" })
            {
                _client.AddModel("character", $"lgo/{part}.lgo");
            }
        }

        private void AddTowerParts()
        {
            foreach (var part in new[] { "3000000000", "3000000001", "3000000002" })
            {
                _client.AddModel("character", $"lgo/{part}.lgo");
            }
        }

        private CharacterResult ConvertTower()
        {
            var row = Character(350, CharacterModalType.Employee, model: 3000, 1853, 1863, 1873);
            var items = new[]
            {
                MachinePart(1853, (ItemType)61, "3000000000"),
                MachinePart(1863, (ItemType)62, "3000000001"),
                MachinePart(1873, (ItemType)63, "3000000002"),
            };

            row.ActionId = 500;

            using var actions = File.OpenRead(Fixtures.Path("tables/CharacterAction.tx"));

            return Converter([row], items, actions: CharacterActionTable.Read(actions)).Convert(350);
        }

        private GltfFile ReadMachinePart(string part)
        {
            using var stream = File.OpenRead(_client.Converted("machine", part));

            return GltfReader.Read(stream);
        }

        private Dictionary<string, float[]> KeyTimesOf(string part)
        {
            var file = ReadMachinePart(part);
            var data = new GltfData(file);

            return (file.Document.Animations ?? []).ToDictionary(animation => animation.Name,
                animation => data.ReadFloats(animation.Samplers[0].Input));
        }

        private float[] TranslationsOf(string part, string animationName)
        {
            var file = ReadMachinePart(part);
            var animation = file.Document.Animations.Single(candidate => candidate.Name == animationName);
            var channel = animation.Channels.Single(candidate => candidate.Target.Path == "translation");

            return new GltfData(file).ReadFloats(animation.Samplers[channel.Sampler].Output);
        }

        private static CharacterActionTable ReadActions(string rows)
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(rows));

            return CharacterActionTable.Read(stream);
        }

        private static ShipItemInfoRecord ShipPart(int id, string name, int model, params int[] propellers)
        {
            var motors = new int[4];
            propellers.CopyTo(motors, 0);

            return new ShipItemInfoRecord { Id = id, Name = name, ModelId = model, Motors = motors };
        }

        private CharacterConverter Converter(IEnumerable<CharacterInfoRecord> characters,
            IEnumerable<ItemInfoRecord> items = null, bool overwrite = true, CharacterActionTable actions = null,
            IEnumerable<ShipInfoRecord> ships = null, IEnumerable<ShipItemInfoRecord> shipParts = null)
        {
            var settings = _client.Settings(overwrite);
            var tables = new ClientTables(
                new Table<CharacterInfoRecord>(characters.ToList()),
                new Table<ItemInfoRecord>((items ?? []).ToList()),
                null,
                actions ?? FakeClient.ActionSet(7),
                ships: new Table<ShipInfoRecord>((ships ?? []).ToList()),
                shipParts: new Table<ShipItemInfoRecord>((shipParts ?? []).ToList()));

            var models = new ModelConverter(settings, tables);

            return new CharacterConverter(settings, tables,
                new RigConverter(settings, tables),
                new ItemConverter(settings, tables, models),
                models);
        }

        [Test]
        public void A_player_character_keeps_its_rig_and_its_parts_apart()
        {
            _client.AddSkeleton("lab/0001.lab");
            _client.AddModel("character", "lgo/0724000000.lgo", "0001000000.lgo");

            var row = Character(10, CharacterModalType.MainCharacter, PlayerModel, 500);
            var result = Converter([row], [Wearable(500, PlayerModel, "0001000000")])
                .Convert(10);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(result.Name, Is.EqualTo("0010"));
            Assert.That(result.Model, Is.Null, "a player is composed, not merged");
            Assert.That(result.Rig.ModelPath, Is.EqualTo(_client.ConvertedRig("0001")));
            Assert.That(result.Clips.Select(clip => clip.ModelPath),
                Is.EqualTo(new[] { _client.ConvertedClip("0001", "01_waiting") }));
            Assert.That(result.Artifacts, Is.SupersetOf(result.Clips));
            Assert.That(result.Parts, Has.Count.EqualTo(1));
            Assert.That(result.Parts[0].ModuleArtifacts[PlayerModel].ModelPath,
                Is.EqualTo(_client.Converted("character", "0001000000")));
        }

        [Test]
        public void A_player_character_wears_only_its_first_five_part_slots()
        {
            _client.AddSkeleton("lab/0001.lab");
            _client.AddModel("character", "lgo/0724000000.lgo", "0001000000.lgo");

            var row = Character(10, CharacterModalType.MainCharacter, PlayerModel,
                500, 0, 0, 0, 0, 501, 501, 501);
            var items = new[]
            {
                Wearable(500, PlayerModel, "0001000000"),
                Wearable(501, PlayerModel, "0001000000"),
            };

            var result = Converter([row], items).Convert(10);

            Assert.That(result.Parts, Has.Count.EqualTo(1),
                "slots past the fifth are monster meshes, not equipment");
        }

        [Test]
        public void A_player_part_naming_no_item_is_reported_and_left_out()
        {
            _client.AddSkeleton("lab/0001.lab");
            _client.AddModel("character", "lgo/0724000000.lgo", "0001000000.lgo");

            var row = Character(10, CharacterModalType.MainCharacter, PlayerModel, 500, 999);
            var result = Converter([row], [Wearable(500, PlayerModel, "0001000000")])
                .Convert(10);

            Assert.That(result.Parts, Has.Count.EqualTo(1));
            Assert.That(_log.Warnings, Has.Some.Contains("unknown item 999"));
        }

        [Test]
        public void A_player_part_skinned_to_another_skeleton_is_reported()
        {
            _client.AddSkeleton("lab/0001.lab");
            _client.AddSkeleton("lab/0724.lab");
            _client.AddModel("character", "lgo/0724000000.lgo");

            var row = Character(10, CharacterModalType.MainCharacter, PlayerModel, 500);

            Converter([row], WornByTwoBodyTypes(500, "0724000000")).Convert(10);

            Assert.That(_log.Warnings, Has.Some.Contains("part '0724000000' binds bones rig '0001' does not have"));
        }

        [Test]
        public void A_player_part_skinned_to_its_rig_is_not_reported()
        {
            _client.AddSkeleton("lab/0001.lab");
            _client.AddModel("character", "lgo/0724000000.lgo", "0001000000.lgo");

            var row = Character(10, CharacterModalType.MainCharacter, PlayerModel, 500);

            Converter([row], [Wearable(500, PlayerModel, "0001000000")]).Convert(10);

            Assert.That(_log.Warnings, Has.None.Contains("does not have"));
        }

        [Test]
        public void A_player_part_already_converted_is_still_checked_against_its_rig()
        {
            _client.AddSkeleton("lab/0001.lab");
            _client.AddSkeleton("lab/0724.lab");
            _client.AddModel("character", "lgo/0724000000.lgo");

            var row = Character(10, CharacterModalType.MainCharacter, PlayerModel, 500);
            var items = WornByTwoBodyTypes(500, "0724000000");

            Converter([row], items).Convert(10);
            _log.Warnings.Clear();
            Converter([row], items, overwrite: false).Convert(10);

            Assert.That(_log.Warnings, Has.Some.Contains("part '0724000000' binds bones rig '0001' does not have"));
        }

        [Test]
        public void A_player_character_whose_parts_all_fail_fails()
        {
            _client.AddSkeleton("lab/0001.lab");

            var row = Character(10, CharacterModalType.MainCharacter, PlayerModel, 500);
            var result = Converter([row], [Wearable(500, PlayerModel, "missing")])
                .Convert(10);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(_log.Errors, Has.Some.Contains("no part of character 10 loaded"));
        }

        [Test]
        public void A_monster_is_merged_into_one_model_named_after_its_skeleton()
        {
            _client.AddSkeleton("lab/0724.lab");
            _client.AddModel("character", "lgo/0724000000.lgo");

            var row = Character(20, CharacterModalType.Other, MonsterModel, 1);
            var result = Converter([row]).Convert(20);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(result.Name, Is.EqualTo("0724"));
            Assert.That(result.Rig, Is.Null, "the skeleton rides inside the merged model");
            Assert.That(result.Model.ModelPath, Is.EqualTo(_client.Converted("character", "0724")));
        }

        [Test]
        public void Monsters_sharing_a_skeleton_merge_once()
        {
            _client.AddSkeleton("lab/0724.lab");
            _client.AddModel("character", "lgo/0724000000.lgo");

            var rows = new[]
            {
                Character(20, CharacterModalType.Other, MonsterModel, 1),
                Character(21, CharacterModalType.Other, MonsterModel, 1),
            };

            var results = Converter(rows).ConvertAll().ToList();

            Assert.That(results.Select(result => result.Outcome),
                Is.EqualTo(new[] { ConversionOutcome.Converted, ConversionOutcome.Skipped }));
            Assert.That(results[1].Model.ModelPath, Is.EqualTo(results[0].Model.ModelPath));
        }

        [Test]
        public void A_monster_with_no_meshes_fails()
        {
            _client.AddSkeleton("lab/0724.lab");

            var row = Character(20, CharacterModalType.Other, MonsterModel, 1);
            var result = Converter([row]).Convert(20);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(_log.Errors, Has.Some.Contains("no part of model 724 loaded"));
        }

        [Test]
        public void A_monster_with_no_skeleton_fails()
        {
            _client.AddModel("character", "lgo/0724000000.lgo");

            var row = Character(20, CharacterModalType.Other, MonsterModel, 1);
            var result = Converter([row]).Convert(20);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(_log.Errors, Has.Some.Contains("0724.lab"));
        }

        [Test]
        public void A_boat_converts_each_of_its_parts_to_a_model_of_its_own_under_the_machine_kind()
        {
            AddBoatParts();

            var row = Character(301, CharacterModalType.Boat, PlayerModel, 907, 989, 1071);
            var result = Converter([row], BoatItems).Convert(301);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(result.Artifacts.Select(artifact => artifact.ModelPath), Is.EquivalentTo(new[]
            {
                _client.Converted("machine", "2000000000"),
                _client.Converted("machine", "2000000001"),
                _client.Converted("machine", "2000000002"),
                _client.Converted("machine", "2000000004"),
            }), "the hull, the bow, the engine and the propeller the engine names");
            Assert.That(result.MachineParts.Select(part => ReadMachinePart(part.Name).Document.Skins), Is.All.Null,
                "a boat part is rigid");
        }

        [Test]
        public void A_boat_converts_the_parts_of_every_ship_built_on_it()
        {
            AddBoatParts();

            var row = Character(301, CharacterModalType.Boat, PlayerModel);
            var ship = new ShipInfoRecord
            {
                Id = 11, Name = "Phantom Galley", CharacterId = 301, PoseId = 7, HullId = 1,
                EngineOptions = [21], HeadOptions = [115],
            };
            var shipParts = new[]
            {
                ShipPart(1, "Titanic Hull", 2000000000),
                ShipPart(21, "Turbo Propeller", 2000000002, 22),
                ShipPart(22, "Demon Propeller 01", 2000000004),
                ShipPart(115, "Colliding Bow", 2000000001),
            };

            var result = Converter([row], ships: [ship], shipParts: shipParts).Convert(301);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(result.Artifacts.Select(artifact => artifact.Name), Is.EquivalentTo(new[]
            {
                "2000000000", "2000000001", "2000000002", "2000000004",
            }), "the ship's hull, every engine it can take with their propellers, and every bow it can take");
            Assert.That(KeyTimesOf("2000000000").Keys, Is.EqualTo(new[] { "01_waiting" }),
                "the hull plays the boat's actions");
        }

        [Test]
        public void A_tower_head_plays_one_clip_for_its_seventeen_identical_action_ranges()
        {
            AddTowerParts();

            ConvertTower();

            Assert.That(KeyTimesOf("3000000001").Select(clip => (clip.Key, clip.Value.Length)),
                Is.EqualTo(new[] { ("01_waiting", 46) }), "actions 1 to 17 all play frames 0 to 45");
            Assert.That(KeyTimesOf("3000000002").Keys, Is.EqualTo(new[] { "geom_2" }),
                "the tube turns on its own track");
            Assert.That(KeyTimesOf("3000000000"), Is.Empty, "the base does not move");
        }

        [Test]
        public void A_boat_resolves_no_file_from_its_framework_number()
        {
            AddBoatParts();
            _client.AddSkeleton("lab/0001.lab");

            var result = Converter([Character(301, CharacterModalType.Boat, PlayerModel, 907, 989, 1071)], BoatItems)
                .Convert(301);

            Assert.That(result.Rig, Is.Null, "framework number 1 is a body type, and a boat does not animate on it");
            Assert.That(result.Artifacts.Select(artifact => artifact.Kind), Is.All.EqualTo(ContentKind.Machine));
        }

        [Test]
        public void A_boat_flies_the_flag_its_hull_names()
        {
            AddBoatParts();
            _client.AddModel("character", "lgo/2000000001.lgo", "2000000003.lgo");

            var items = BoatItems;
            items[0].ShipFlag = 2004;

            var result = Converter([Character(301, CharacterModalType.Boat, PlayerModel, 907, 989, 1071)],
                [..items, MachinePart(2004, ItemType.BoatSign, "2000000003")]).Convert(301);

            Assert.That(result.Artifacts.Select(artifact => artifact.ModelPath),
                Has.Member(_client.Converted("machine", "2000000003")));
        }

        [Test]
        public void A_boat_hull_plays_one_clip_for_each_frame_range_of_its_action_set()
        {
            AddBoatParts();

            var actions = ReadActions("7\n\t1\t0\t60\n\t2\t0\t60\n\t7\t40\t45\t43\n\t17\t61\t180\n");

            Converter([Character(301, CharacterModalType.Boat, PlayerModel, 907, 989, 1071)], BoatItems,
                actions: actions).Convert(301);

            var clips = KeyTimesOf("2000000000");

            Assert.That(clips.Keys, Is.EquivalentTo(new[] { "01_waiting", "07_attack", "17_die" }),
                "a range several actions play is one clip, named after the lowest of them");
            Assert.That(clips.Values.Select(times => times.Length), Is.EquivalentTo(new[] { 61, 6, 120 }));
            Assert.That(clips["07_attack"][0], Is.Zero, "a clip starts at its first frame");
            Assert.That(TranslationsOf("2000000000", "07_attack").Take(3),
                Is.EqualTo(TranslationsOf("2000000000", "01_waiting").Skip(40 * 3).Take(3)),
                "the attack starts on frame 40 of the hull's track");
        }

        [Test]
        public void A_boat_part_other_than_the_hull_plays_its_own_whole_track()
        {
            AddBoatParts();

            var actions = ReadActions("7\n\t1\t0\t60\n\t7\t40\t45\t43\n");

            Converter([Character(301, CharacterModalType.Boat, PlayerModel, 907, 989, 1071)], BoatItems,
                actions: actions).Convert(301);

            var propeller = KeyTimesOf("2000000004");

            Assert.That(propeller.Values.Select(times => times.Length), Is.EqualTo(new[] { 122 }),
                "the propeller turns on its own track, not in the hull's clips");
            Assert.That(KeyTimesOf("2000000001"), Is.Empty, "the bow does not move");
        }

        [Test]
        public void An_unknown_id_fails()
        {
            Assert.That(Converter([]).Convert(10).Outcome,
                Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(_log.Errors, Has.Some.Contains("no characterinfo row 10"));
        }

        [Test]
        public void Converting_a_skeleton_covers_every_character_on_it()
        {
            _client.AddSkeleton("lab/0724.lab");
            _client.AddModel("character", "lgo/0724000000.lgo");

            var rows = new[]
            {
                Character(20, CharacterModalType.Other, MonsterModel, 1),
                Character(21, CharacterModalType.Other, MonsterModel, 1),
                Character(22, CharacterModalType.Other, model: 99, parts: 1),
            };

            var results = Converter(rows).ConvertModel(MonsterModel).ToList();

            Assert.That(results.Select(result => result.Id), Is.EqualTo(new[] { 20, 21 }));
        }

        [Test]
        public void A_skeleton_no_character_uses_converts_nothing()
        {
            var results = Converter([])
                .ConvertModel(MonsterModel)
                .ToList();

            Assert.That(results, Is.Empty);
            Assert.That(_log.Warnings, Has.Some.Contains("no characterinfo row uses model 724"));
        }
    }
}
