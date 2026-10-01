using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Top.Contracts;
using Top.Contracts.Tables;
using Top.Contracts.Tables.Items;
using Top.Contracts.Tables.Rigs;
using Top.Contracts.Tables.Ships;
using Top.Contracts.Tables.World;
using Top.Conversion.Pipeline;
using Top.Legacy.Tables.Custom;
using Top.Legacy.Tables.Records;
using Characters = Top.Contracts.Tables.Characters;

namespace Top.Conversion.Tests.Pipeline
{
    public class TableConverterTests
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

        private static SceneObjectInfoRecord Row(int id, string fileName)
        {
            return new SceneObjectInfoRecord { Id = id, Name = fileName };
        }

        private static readonly TerrainInfoRecord[] TerrainRows =
        [
            new TerrainInfoRecord
            {
                Id = 3, Name = "texture/terrain/Sand01.bmp", Type = 2,
            },
        ];

        private static readonly MapInfoRecord[] MapRows =
        [
            new MapInfoRecord
            {
                Id = 1, Name = "garner", DisplayName = "Ascaron", ShowSwitch = true,
                InitX = 2202, InitY = 2782,
            },
        ];

        private static readonly AreaRecord[] AreaRows =
        [
            new AreaRecord { Id = 1, Name = "Argent City", IsCity = true },
            new AreaRecord { Id = 2, Name = "Andes Forest Haven" },
            new AreaRecord { Id = 3, Name = "" },
        ];

        private static readonly TipsList Tips = new TipsList
        {
            Lines = ["At every new level you gain stat points.", "", "You can reset your Stats using Stat Reset."],
        };

        private static readonly CharacterInfoRecord[] CharacterRows =
        [
            new CharacterInfoRecord
            {
                Id = 1, Name = "Long Haired Guy", ModalType = CharacterModalType.MainCharacter, Model = 3,
                Height = 5.5f, Radius = 12, Territory = 1, SeaHeight = 30,
                Parts = [2246, 0, 464, 640, 816, 7, 7, 7], ActionId = 4,
            },
            new CharacterInfoRecord
            {
                Id = 6, Name = "Weird Monster", ModalType = CharacterModalType.Other, Model = 55,
                Height = 9f, Radius = 20, Parts = [1, 1, 0, 0, 0, 0, 0, 0], ActionId = 9,
                EffectActionIds = [1, 312, 2],
            },
            new CharacterInfoRecord
            {
                Id = 9, Name = "Trade Ship", ModalType = CharacterModalType.Boat, Model = 70, ActionId = 401,
            },
            new CharacterInfoRecord
            {
                Id = 879, Name = "Ami Newbie Phantom", ModalType = CharacterModalType.MainCharacter, Model = 3,
                ControlType = CharacterControlType.Npc, Parts = [2246, 0, 464, 640, 816, 7, 7, 7], ActionId = 4,
            },
        ];

        /// <summary>
        /// Action set 4 is a main character's, on the stance grid: an action is
        /// its pose in the fists column and 54 more in each column after it.
        /// Action 1 is the same in fists and sword, action 4 is the same in fists
        /// and sword but not in two-hand sword, action 5 keeps its range in sword
        /// but not its key frame, and action 40 exists in dual swords only. Action
        /// set 9 is a monster's, with key frames, and set 401 a boat's.
        /// </summary>
        private const string ActionRows = """
            4
            \t1\t10\t20
            \t55\t10\t20
            \t4\t30\t40
            \t58\t30\t40
            \t112\t50\t60
            \t5\t90\t99\t92
            \t59\t90\t99\t95
            \t202\t70\t80
            9
            \t1\t0\t10\t0\t9
            \t2\t11\t20\t15
            401
            \t1\t0\t5
            """;

        private static readonly ItemInfoRecord[] ItemRows =
        [
            new ItemInfoRecord
            {
                Id = 464, Name = "Novice Tunic", Icon = "e0020", Type = ItemType.Armor, EquipSlots = [2],
                Modules = ["10110002", "0000610002", "0001610002", "0002610002", "0"],
            },
            new ItemInfoRecord
            {
                Id = 2000, Name = "Short Hair", Icon = "0", Type = ItemType.Hairdo, EquipSlots = [0],
                Modules = ["10130005", "0000000001", "0", "0", "0"],
            },
            new ItemInfoRecord
            {
                Id = 1, Name = "Short Sword", Icon = "W0001", Type = ItemType.Sword, EquipSlots = [9, 6],
                Modules = ["0", "01010001", "02010001", "03010070", "0"],
            },
        ];

        private TableConverter Converter(bool overwrite = true, params SceneObjectInfoRecord[] rows)
        {
            return Converter(ItemRows, overwrite, rows);
        }

        private TableConverter Converter(ItemInfoRecord[] items, bool overwrite = true,
            params SceneObjectInfoRecord[] rows)
        {
            var tables = new ClientTables(
                new Top.Legacy.Tables.Table<CharacterInfoRecord>([..CharacterRows]),
                new Top.Legacy.Tables.Table<ItemInfoRecord>([..items]),
                new Top.Legacy.Tables.Table<SceneObjectInfoRecord>([..rows]), ReadActions(ActionRows),
                new Top.Legacy.Tables.Table<TerrainInfoRecord>([..TerrainRows]),
                new Top.Legacy.Tables.Table<MapInfoRecord>([..MapRows]),
                new Top.Legacy.Tables.Table<ShipInfoRecord>([new ShipInfoRecord { Id = 1, Name = "Guppy", HullId = 43 }]),
                new Top.Legacy.Tables.Table<ShipItemInfoRecord>([ShipPart(43, 2004000000)]),
                new Top.Legacy.Tables.Table<AreaRecord>([..AreaRows]),
                Tips,
                ReadClientAttributes(),
                new Top.Legacy.Tables.Table<StoneInfoRecord>([..StoneRows]),
                new Top.Legacy.Tables.Table<ItemPreRecord>([..PrefixRows]),
                ReadGemHints());

            return new TableConverter(_client.Settings(overwrite), tables);
        }

        private static readonly ItemPreRecord[] PrefixRows =
        [
            new ItemPreRecord { Id = 0, Name = "0" },
            new ItemPreRecord { Id = 1, Name = "Mammoth " },
            new ItemPreRecord { Id = 36, Name = "Uncommon " },
        ];

        private static readonly StoneInfoRecord[] StoneRows =
        [
            new StoneInfoRecord
            {
                Id = 1, Name = "Fiery Gem", ItemId = 878, EquipPositions = [1], Type = 1,
                HintFunction = "ItemHint_LieYanS", ItemRgb = 0xF6D243,
            },
            new StoneInfoRecord
            {
                Id = 10, Name = "Gem of Striking", ItemId = 861, EquipPositions = [23], Type = 4,
                HintFunction = "ItemHint_YingYanS", ItemRgb = 0xEB4E68,
            },
            new StoneInfoRecord
            {
                Id = 17, Name = "Yellow Jade", ItemId = 6832, EquipPositions = [14, 0, 0], Type = 2,
                HintFunction = "ItemHint_HuangYu", ItemRgb = 0xFED330,
            },
            new StoneInfoRecord
            {
                Id = 40, Name = "Key of Apollo", ItemId = 2911, EquipPositions = [0], Type = 1,
                HintFunction = "ItemHint_ABOLUO", ItemRgb = 0xFFA500,
            },
        ];

        private static GemHintScript ReadGemHints()
        {
            using var stream = File.OpenRead(Fixtures.Path("lua/scripts.lua"));

            return GemHintScript.Read(stream);
        }

        private static ClientAttributeScript ReadClientAttributes()
        {
            using var stream = File.OpenRead(Fixtures.Path("lua/CharacterConf.clu"));

            return ClientAttributeScript.Read(stream);
        }

        private static ItemInfoRecord MachinePart(int id, ItemType type, params string[] modules)
        {
            var columns = new[] { "0", "0", "0", "0", "0" };
            modules.CopyTo(columns, 0);

            return new ItemInfoRecord { Id = id, Name = $"item {id}", Type = type, Modules = columns };
        }

        /// <summary>
        /// A player, the boat Ship 01 on framework number 3 and the cannon
        /// tower on framework number 3000, their part items, two ships built
        /// on the boat and the parts those ships show. The action table is
        /// the shipped one.
        /// </summary>
        private TableConverter MachineConverter()
        {
            var boat = new CharacterInfoRecord
            {
                Id = 301, Name = "Ship 01", ModalType = CharacterModalType.Boat, Model = 3, ActionId = 401,
                Parts = [907, 989, 1071, 0, 0, 0, 0, 0],
            };
            var tower = new CharacterInfoRecord
            {
                Id = 350, Name = "Defense Turret A", ModalType = CharacterModalType.Employee, Model = 3000,
                ActionId = 500, Parts = [1853, 1863, 1873, 0, 0, 0, 0, 0],
            };
            var hull = MachinePart(907, ItemType.Hull, "2000000000");

            hull.ShipFlag = 2004;

            var items = new[]
            {
                hull,
                MachinePart(989, ItemType.Embolon, "2000000001"),
                MachinePart(1071, ItemType.Mobility, "2000000002", "1153"),
                MachinePart(1153, ItemType.Airscrew, "2000000004"),
                MachinePart(2004, ItemType.BoatSign, "2000000003"),
                MachinePart(1853, (ItemType)61, "3000000000"),
                MachinePart(1863, (ItemType)62, "3000000001"),
                MachinePart(1873, (ItemType)63, "3000000002"),
            };
            var ships = new[]
            {
                new ShipInfoRecord { Id = 11, Name = "Phantom Galley", CharacterId = 301, PoseId = 401, HullId = 1 },
                new ShipInfoRecord { Id = 12, Name = "Silver Dolphin", CharacterId = 301, PoseId = 401, HullId = 2 },
            };
            var shipParts = new[]
            {
                ShipPart(1, 2000000000), ShipPart(2, 2000010000), ShipPart(8, 0), ShipPart(21, 2000000002, 22),
                ShipPart(22, 2000000004), ShipPart(53, 0), ShipPart(146, 0, 29), ShipPart(29, 2001000004),
            };

            using var actions = File.OpenRead(Fixtures.Path("tables/CharacterAction.tx"));

            var tables = new ClientTables(
                new Top.Legacy.Tables.Table<CharacterInfoRecord>([CharacterRows[0], boat, tower]),
                new Top.Legacy.Tables.Table<ItemInfoRecord>([..items]), null, CharacterActionTable.Read(actions),
                ships: new Top.Legacy.Tables.Table<ShipInfoRecord>([..ships]),
                shipParts: new Top.Legacy.Tables.Table<ShipItemInfoRecord>([..shipParts]));

            return new TableConverter(_client.Settings(), tables);
        }

        private static ShipItemInfoRecord ShipPart(int id, int model, params int[] propellers)
        {
            var motors = new int[4];
            propellers.CopyTo(motors, 0);

            return new ShipItemInfoRecord { Id = id, Name = $"ship part {id}", ModelId = model, Motors = motors };
        }

        private static CharacterActionTable ReadActions(string rows)
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(rows.Replace("\\t", "\t")));

            return CharacterActionTable.Read(stream);
        }

        private static TableResult Run(TableConverter converter, string unit = "sceneobjects")
        {
            return converter.ConvertAll().Single(result => result.Name == unit);
        }

        private string TablePath => Path.Combine(_client.OutputRoot, "tables", "sceneobjects.json");

        private List<SceneObjectEntry> Emitted()
        {
            using var stream = File.OpenRead(TablePath);

            return new TableFormat().Read<SceneObjectEntry>(stream);
        }

        private List<TerrainEntry> EmittedTerrains()
        {
            using var stream = File.OpenRead(Path.Combine(_client.OutputRoot, "tables", "terrains.json"));

            return new TableFormat().Read<TerrainEntry>(stream);
        }

        private List<MapEntry> EmittedMaps()
        {
            using var stream = File.OpenRead(Path.Combine(_client.OutputRoot, "tables", "maps.json"));

            return new TableFormat().Read<MapEntry>(stream);
        }

        private List<AreaEntry> EmittedAreas()
        {
            using var stream = File.OpenRead(Path.Combine(_client.OutputRoot, "tables", "areas.json"));

            return new TableFormat().Read<AreaEntry>(stream);
        }

        private List<TipEntry> EmittedTips()
        {
            using var stream = File.OpenRead(Path.Combine(_client.OutputRoot, "tables", "tips.json"));

            return new TableFormat().Read<TipEntry>(stream);
        }

        private List<Characters.JobEntry> EmittedJobs()
        {
            using var stream = File.OpenRead(Path.Combine(_client.OutputRoot, "tables", "jobs.json"));

            return new TableFormat().Read<Characters.JobEntry>(stream);
        }

        private List<Characters.PortraitCameraEntry> EmittedPortraitCameras()
        {
            using var stream = File.OpenRead(Path.Combine(_client.OutputRoot, "tables", "portraitcameras.json"));

            return new TableFormat().Read<Characters.PortraitCameraEntry>(stream);
        }

        private List<ItemEntry> EmittedItems()
        {
            using var stream = File.OpenRead(Path.Combine(_client.OutputRoot, "tables", "items.json"));

            return new TableFormat().Read<ItemEntry>(stream);
        }

        private List<T> EmittedTable<T>(string fileName) where T : TableEntry
        {
            using var stream = File.OpenRead(Path.Combine(_client.OutputRoot, "tables", fileName));

            return new TableFormat().Read<T>(stream);
        }

        private List<RigEntry> EmittedRigs()
        {
            using var stream = File.OpenRead(Path.Combine(_client.OutputRoot, "tables", "rigs.json"));

            return new TableFormat().Read<RigEntry>(stream);
        }

        private List<RigEntry> EmittedActionSets()
        {
            using var stream = File.OpenRead(Path.Combine(_client.OutputRoot, "tables", "actionsets.json"));

            return new TableFormat().Read<RigEntry>(stream);
        }

        private List<ShipEntry> EmittedShips()
        {
            using var stream = File.OpenRead(Path.Combine(_client.OutputRoot, "tables", "ships.json"));

            return new TableFormat().Read<ShipEntry>(stream);
        }

        private List<ShipPartEntry> EmittedShipParts()
        {
            using var stream = File.OpenRead(Path.Combine(_client.OutputRoot, "tables", "shipparts.json"));

            return new TableFormat().Read<ShipPartEntry>(stream);
        }

        private List<Characters.CharacterEntry> EmittedCharacters()
        {
            using var stream = File.OpenRead(Path.Combine(_client.OutputRoot, "tables", "characters.json"));

            return new TableFormat().Read<Characters.CharacterEntry>(stream);
        }

        [Test]
        public void Every_row_emits_mapped_to_its_id_and_converted_model_path()
        {
            var result = Run(Converter(rows:
            [
                Row(42, "Stone01.lgo"),
                Row(7, string.Empty),
            ]));

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Converted));

            var entries = Emitted();

            Assert.That(entries.Select(entry => entry.Id), Is.EqualTo(new[] { 42, 7 }),
                "every row emits, model file or not");
            Assert.That(entries[0].ModelPath, Is.EqualTo("models/scene/stone01.glb"),
                "resolved by the output layout convention, no model converted first");
            Assert.That(entries[1].ModelPath, Is.Null, "lights, sounds and fog carry no path");
        }

        [Test]
        public void The_whole_row_emits()
        {
            var row = Row(42, "Stone01.lgo");

            row.DisplayName = "stone";
            row.AttachEffectId = 5;
            row.EnableEnvLight = true;
            row.EnablePointLight = true;
            row.Style = 1;
            row.Flag = 2;
            row.SizeFlag = 3;
            row.ShadeFlag = true;
            row.IsReallyBig = true;

            Run(Converter(rows: row));

            var entry = Emitted().Single();

            Assert.That(entry.DisplayName, Is.EqualTo("stone"));
            Assert.That(entry.AttachEffectId, Is.EqualTo(5));
            Assert.That(entry.EnableEnvLight, Is.True);
            Assert.That(entry.EnablePointLight, Is.True);
            Assert.That(entry.Style, Is.EqualTo(1));
            Assert.That(entry.Flag, Is.EqualTo(2));
            Assert.That(entry.SizeFlag, Is.EqualTo(3));
            Assert.That(entry.ShadeFlag, Is.True);
            Assert.That(entry.IsReallyBig, Is.True);
        }

        [Test]
        public void The_kind_selects_the_variation_the_row_emits_as()
        {
            var fading = Row(1, "Tree01.lgo");
            fading.FadeObjSeq = new[] { 1, 2 };
            fading.FadeCoefficient = 0.5f;

            var light = new SceneObjectInfoRecord
            {
                Id = 2, Name = string.Empty, Type = 3, PointColor = new[] { 255, 128, 0 },
                PointLightRange = 500, PointLightAttenuation = 0.7f, PointLightAnimCtrlId = 9,
            };
            var ambient = new SceneObjectInfoRecord
            {
                Id = 3, Name = string.Empty, Type = 4, EnvColor = new[] { 10, 20, 30 },
            };
            var fog = new SceneObjectInfoRecord
            {
                Id = 4, Name = string.Empty, Type = 5, FogColor = new[] { 1, 2, 3 },
            };
            var sound = new SceneObjectInfoRecord
            {
                Id = 5, Name = string.Empty, Type = 6, EnvSound = "wave.wav", EnvSoundDistance = 900,
            };

            Run(Converter(rows: [fading, light, ambient, fog, sound]));

            var entries = Emitted();

            var faded = (FadeEntry)entries[0];

            Assert.That(faded.Sequence, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(faded.Coefficient, Is.EqualTo(0.5f));

            var pointLight = (PointLightEntry)entries[1];

            Assert.That(pointLight.Kind, Is.EqualTo(SceneObjectKind.PointLight));
            Assert.That(pointLight.Color, Is.EqualTo(new Rgb(255, 128, 0)));
            Assert.That(pointLight.Range, Is.EqualTo(500));
            Assert.That(pointLight.Attenuation, Is.EqualTo(0.7f));
            Assert.That(pointLight.AnimationId, Is.EqualTo(9));
            Assert.That(((AmbientLightEntry)entries[2]).Color, Is.EqualTo(new Rgb(10, 20, 30)));
            Assert.That(((FogEntry)entries[3]).Color, Is.EqualTo(new Rgb(1, 2, 3)));

            var emitted = (SoundEntry)entries[4];

            Assert.That(emitted.Sound, Is.EqualTo("wave.wav"));
            Assert.That(emitted.Distance, Is.EqualTo(900));
        }

        [Test]
        public void An_ordinary_row_without_fade_data_emits_as_the_base()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")));

            Assert.That(Emitted().Single().GetType(), Is.EqualTo(typeof(SceneObjectEntry)));
        }

        [Test]
        public void A_missing_catalog_fails()
        {
            var settings = _client.Settings();
            var tables = new ClientTables(null, null, null, null);

            var result = Run(new TableConverter(settings, tables));

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(File.Exists(TablePath), Is.False);
            Assert.That(_log.Errors, Has.Some.Contains("sceneobjects"));
        }

        [Test]
        public void A_table_already_in_the_tree_is_skipped_without_overwrite()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")));

            var result = Run(Converter(overwrite: false, Row(42, "Stone01.lgo"), Row(7, string.Empty)));

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Skipped));
            Assert.That(Emitted(), Has.Count.EqualTo(1));
        }

        [Test]
        public void Every_terrain_row_emits_with_its_converted_texture_path()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "terrains");

            var entry = EmittedTerrains().Single();

            Assert.That(entry.Id, Is.EqualTo(3), "the id tiles carry");
            Assert.That(entry.TexturePath, Is.EqualTo("textures/terrain/sand01.png"));
            Assert.That(entry.Type, Is.EqualTo(2), "the original type number is kept");
            Assert.That(entry.LeavesFootprints, Is.False);
        }

        [Test]
        public void Every_map_row_emits_with_its_converted_map_path()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "maps");

            var entry = EmittedMaps().Single();

            Assert.That(entry.Id, Is.EqualTo(1));
            Assert.That(entry.Name, Is.EqualTo("garner"), "the name the server addresses the map by");
            Assert.That(entry.MapPath, Is.EqualTo("maps/garner.map"));
            Assert.That(entry.DisplayName, Is.EqualTo("Ascaron"));
            Assert.That(entry.ShowsAreaNames, Is.True);
            Assert.That(entry.StartX, Is.EqualTo(2202));
            Assert.That(entry.StartY, Is.EqualTo(2782));
            Assert.That(entry.LightDirection, Is.EqualTo(new Float3(1f, 1f, -1f)));
            Assert.That(entry.LightColor, Is.EqualTo(new Rgb(255, 255, 255)));
        }

        [Test]
        public void Every_area_row_emits_its_name_and_if_it_is_a_city()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "areas");

            var entries = EmittedAreas();

            Assert.That(entries.Select(entry => (entry.Id, entry.Name, entry.IsCity)), Is.EqualTo(new[]
            {
                (1, "Argent City", true), (2, "Andes Forest Haven", false), (3, (string)null, false),
            }));
        }

        [Test]
        public void Every_tip_line_emits_in_file_order_with_its_line_number()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "tips");

            var entries = EmittedTips();

            Assert.That(entries.Select(entry => (entry.Id, entry.Text)), Is.EqualTo(new[]
            {
                (1, "At every new level you gain stat points."), (2, ""), (3, "You can reset your Stats using Stat Reset."),
            }));
        }

        [Test]
        public void The_job_table_emits_the_nineteen_names_by_job_index()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "jobs");

            Assert.That(EmittedJobs().Select(entry => (entry.Id, entry.Name)), Is.EqualTo(new[]
            {
                (0, "Newbie"), (1, "Swordsman"), (2, "Hunter"), (3, "Sailor"), (4, "Explorer"), (5, "Herbalist"),
                (6, "Engineer"), (7, "Merchant"), (8, "Champion"), (9, "Crusader"), (10, "White Knight"),
                (11, "Animal Tamer"), (12, "Sharpshooter"), (13, "Cleric"), (14, "Seal master"), (15, "Captain"),
                (16, "Voyager"), (17, "Upstart"), (18, "Engineer"),
            }));
        }

        [Test]
        public void The_portrait_camera_table_emits_the_angle_distance_and_height_that_the_script_sets_for_each_character_type()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "portraitcameras");

            var entries = EmittedPortraitCameras()
                .ToDictionary(entry => entry.Id, entry => (entry.Angle, entry.Distance, entry.Height));

            Assert.That(entries, Has.Count.EqualTo(62));
            Assert.That(entries[0], Is.EqualTo((30, 3f, 0.6f)), "entry 0 is the fallback");
            Assert.That(entries[789], Is.EqualTo((357, 15f, 3f)), "a type the script sets through a group");
            Assert.That(entries[851], Is.EqualTo((19, 3f, 0.6f)), "the last type of a group with a trailing comma");
            Assert.That(entries[289], Is.EqualTo((14, 7.5f, 2.4f)), "a later call overwrites an earlier one");
        }

        [Test]
        public void The_player_types_emit_a_head_and_shoulders_framing_in_place_of_the_head_the_script_frames()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "portraitcameras");

            var entries = EmittedPortraitCameras()
                .ToDictionary(entry => entry.Id, entry => (entry.Angle, entry.Distance, entry.Height));

            Assert.That(entries[1], Is.EqualTo((30, 2.25f, 0.65f)), "the script sets 30, 1.5, 0.5");
            Assert.That(entries[2], Is.EqualTo((30, 2.7f, 0.55f)), "the script sets 30, 1.8, 0.4");
            Assert.That(entries[3], Is.EqualTo((30, 2.25f, 0.45f)), "the script sets 30, 1.5, 0.3");
            Assert.That(entries[4], Is.EqualTo((30, 2.1f, 0.65f)), "the script sets 30, 1.4, 0.5");
        }

        [Test]
        public void Every_character_row_emits_the_nine_columns()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "characters");

            var entries = EmittedCharacters();

            Assert.That(entries.Select(entry => entry.Id), Is.EqualTo(new[] { 1, 6, 9, 879 }));

            var player = entries[0];

            Assert.That(player.Name, Is.EqualTo("Long Haired Guy"));
            Assert.That(player.ModalType, Is.EqualTo(Characters.CharacterModalType.MainCharacter));
            Assert.That(player.RigId, Is.EqualTo(3), "a player animates on the rig entry of its body type");
            Assert.That(player.ModelPath, Is.Null, "a player is composed out of its equipment");
            Assert.That(player.Height, Is.EqualTo(5.5f));
            Assert.That(player.Radius, Is.EqualTo(12));
            Assert.That(player.Territory, Is.EqualTo(1));
            Assert.That(player.SeaHeight, Is.EqualTo(30));

            var monster = entries[1];

            Assert.That(monster.ModalType, Is.EqualTo(Characters.CharacterModalType.Other));
            Assert.That(monster.ModelPath, Is.EqualTo("models/character/0055.glb"),
                "a monster is merged into one model named after its skeleton");
            Assert.That(monster.RigId, Is.EqualTo(55), "a monster plays the clips of the rig entry named after its skeleton");

            var boat = entries[2];

            Assert.That(boat.ModelPath, Is.Null, "a boat is built of parts");
            Assert.That(boat.RigId, Is.Null, "a boat has no skeleton, so no rig");
        }

        [Test]
        public void A_boat_emits_the_model_of_each_part_slot_and_the_action_set_it_plays()
        {
            Run(MachineConverter(), "characters");

            var boat = EmittedCharacters().Single(entry => entry.Id == 301);

            Assert.That(boat.PartPaths, Is.EqualTo(new[]
            {
                "models/machine/2000000000.glb", "models/machine/2000000001.glb", "models/machine/2000000002.glb",
                "models/machine/2000000003.glb", "models/machine/2000000004.glb", null, null, null,
            }), "hull, bow, engine, the flag the hull names and the propellers the engine names");
            Assert.That(boat.ActionSetId, Is.EqualTo(401));
        }

        [Test]
        public void A_tower_emits_the_model_of_its_base_head_and_tube_and_the_action_set_it_plays()
        {
            Run(MachineConverter(), "characters");

            var tower = EmittedCharacters().Single(entry => entry.Id == 350);

            Assert.That(tower.PartPaths, Is.EqualTo(new[]
            {
                "models/machine/3000000000.glb", "models/machine/3000000001.glb", "models/machine/3000000002.glb",
            }));
            Assert.That(tower.ActionSetId, Is.EqualTo(500));
        }

        [Test]
        public void A_boat_or_a_tower_resolves_nothing_from_its_framework_number()
        {
            Run(MachineConverter(), "characters");

            foreach (var entry in EmittedCharacters().Where(entry => entry.Id is 301 or 350))
            {
                Assert.That((entry.RigId, entry.BodyType, entry.ModelPath), Is.EqualTo(((int?)null, (int?)null, (string)null)),
                    "the boat's framework number is a body type and the tower's is 3000, and neither names a file");
                Assert.That(entry.DefaultItems, Is.Empty);
            }
        }

        [Test]
        public void Every_action_set_a_boat_or_a_tower_plays_emits_one_entry_with_no_rig()
        {
            Run(MachineConverter(), "actionsets");

            var entries = EmittedActionSets();

            Assert.That(entries.Select(entry => entry.Id), Is.EqualTo(new[] { 401, 500 }),
                "the player's action set is a rig's");
            Assert.That(entries.Select(entry => entry.Path), Is.All.Null);
        }

        [Test]
        public void The_tower_action_set_plays_its_seventeen_identical_ranges_as_one_clip()
        {
            Run(MachineConverter(), "actionsets");

            var tower = EmittedActionSets().Single(entry => entry.Id == 500);

            Assert.That(tower.Clips.Select(clip => (clip.Name, clip.File, clip.Animation)),
                Is.EqualTo(new[] { ("01_waiting", (string)null, "01_waiting") }),
                "the clip is in the part that plays it, not in a file of its own");
            Assert.That(tower.Actions.Select(action => (action.Id, action.Stance, action.Clip)),
                Is.EqualTo(Enumerable.Range(1, 17).Select(action => (action, (Stance?)null, "01_waiting"))));
        }

        [Test]
        public void The_boat_action_set_names_each_clip_after_the_lowest_action_that_plays_it()
        {
            Run(MachineConverter(), "actionsets");

            var boat = EmittedActionSets().Single(entry => entry.Id == 401);

            Assert.That(boat.Clips.Select(clip => clip.Name),
                Is.EqualTo(new[] { "01_waiting", "07_attack", "17_die", "39_falldown" }));
            Assert.That(boat.Actions.Where(action => action.Id is 13 or 8).Select(action => action.Clip),
                Is.EqualTo(new[] { "07_attack", "01_waiting" }));
            Assert.That(boat.Actions.Single(action => action.Id == 7).Events.Select(e => (e.Name, e.Time)),
                Is.EqualTo(new[] { ("hit", 0.1f) }), "key frame 43 of the range 40-45");
        }

        [Test]
        public void Every_ship_emits_its_hull()
        {
            Run(MachineConverter(), "ships");

            Assert.That(EmittedShips().Select(entry => (entry.Id, entry.HullId)), Is.EqualTo(new[] { (11, 1), (12, 2) }));
        }

        [Test]
        public void A_ship_part_emits_the_model_it_shows_and_the_propellers_it_turns()
        {
            Run(MachineConverter(), "shipparts");

            Assert.That(EmittedShipParts().Select(entry => (entry.Id, entry.ModelPath, entry.PropellerIds)), Is.EqualTo(new[]
            {
                (1, "models/machine/2000000000.glb", new[] { 0, 0, 0, 0 }),
                (2, "models/machine/2000010000.glb", new[] { 0, 0, 0, 0 }),
                (21, "models/machine/2000000002.glb", new[] { 22, 0, 0, 0 }),
                (22, "models/machine/2000000004.glb", new[] { 0, 0, 0, 0 }),
                (146, null, new[] { 29, 0, 0, 0 }),
                (29, "models/machine/2001000004.glb", new[] { 0, 0, 0, 0 }),
            }), "a part with no model and no propeller shows nothing, as cannons and components do");
        }

        [Test]
        public void A_character_that_is_not_built_of_parts_emits_none()
        {
            Run(MachineConverter(), "characters");

            var player = EmittedCharacters().Single(entry => entry.Id == 1);

            Assert.That(player.PartPaths, Is.Empty);
            Assert.That(player.ActionSetId, Is.Null);
        }

        [Test]
        public void A_character_emits_the_special_effect_its_action_shows_at_a_key_frame()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "characters");

            var entries = EmittedCharacters();
            var monster = entries.Single(entry => entry.Id == 6);

            Assert.That((monster.EffectAction.KeyIndex, monster.EffectAction.Effect, monster.EffectAction.Dummy),
                Is.EqualTo((1, 312, 2)));
            Assert.That(entries.Single(entry => entry.Id == 1).EffectAction, Is.Null, "a row with no effect shows none");
        }

        [Test]
        public void A_humanoid_npc_references_the_rig_entry_of_its_body_type()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "characters");

            var npc = EmittedCharacters().Single(entry => entry.Id == 879);

            Assert.That(npc.RigId, Is.EqualTo(3));
            Assert.That(npc.ModelPath, Is.Null);
        }

        [Test]
        public void A_player_body_emits_its_body_type_and_its_default_items()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "characters");

            var entries = EmittedCharacters();

            Assert.That(entries[0].BodyType, Is.EqualTo(3), "the model column gives the body type");
            Assert.That(entries[0].DefaultItems, Is.EqualTo(new[] { 2246, 0, 464, 640, 816 }),
                "the index is the equipment link, an empty link is 0 and the columns past the fifth are not body parts");
        }

        [Test]
        public void A_body_that_is_not_composed_emits_no_body_type_and_no_default_items()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "characters");

            var entries = EmittedCharacters();

            Assert.That(entries[1].BodyType, Is.Null);
            Assert.That(entries[1].DefaultItems, Is.Empty, "a monster's part columns are mesh flags, not items");
            Assert.That(entries[2].BodyType, Is.Null);
            Assert.That(entries[2].DefaultItems, Is.Empty);
        }

        [Test]
        public void Every_worn_item_emits_its_equip_links_and_a_model_per_body_type()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "items");

            var entries = EmittedItems().ToDictionary(entry => entry.Id);

            Assert.That(entries[464].EquipLinks, Is.EqualTo(new[] { 2 }));
            Assert.That(entries[464].ModelPaths, Is.EqualTo(new[]
            {
                "models/character/0000610002.glb",
                "models/character/0001610002.glb",
                "models/character/0002610002.glb",
                null,
            }), "the last body type has no mesh for this item");

            Assert.That(entries[2000].EquipLinks, Is.EqualTo(new[] { 0 }));
            Assert.That(entries[2000].ModelPaths, Is.EqualTo(new[] { "models/character/0000000001.glb", null, null, null }));
        }

        [Test]
        public void A_held_item_emits_its_equip_links_and_a_model_per_body_type_from_the_item_models()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "items");

            var sword = EmittedItems().Single(entry => entry.Id == 1);

            Assert.That(sword.EquipLinks, Is.EqualTo(new[] { 9, 6 }), "the links name the dummies the item attaches to");
            Assert.That(sword.ModelPaths, Is.EqualTo(new[]
            {
                "models/item/01010001.glb",
                "models/item/02010001.glb",
                "models/item/03010070.glb",
                null,
            }), "each body type holds its own copy of the model");
        }

        [Test]
        public void An_item_emits_its_item_type()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "items");

            var entries = EmittedItems().ToDictionary(entry => entry.Id);

            Assert.That(entries[1].Type, Is.EqualTo((int)ItemType.Sword), "the held item type selects the stance");
            Assert.That(entries[464].Type, Is.EqualTo((int)ItemType.Armor));
        }

        [Test]
        public void An_apparel_item_emits_its_apparel_link()
        {
            var robe = new ItemInfoRecord
            {
                Id = 5341, Name = "Robe of the Chilling Heart", Type = ItemType.Armor, EquipSlots = [21],
                Modules = ["0", "0000920002", "0001920002", "0", "0"],
            };

            Run(Converter([robe], rows: Row(42, "Stone01.lgo")), "items");

            var entry = EmittedItems().Single();

            Assert.That(entry.EquipLinks, Is.EqualTo(new[] { 21 }), "link 21 shows over the body part at link 2");
            Assert.That(entry.ModelPaths[0], Is.EqualTo("models/character/0000920002.glb"));
        }

        [Test]
        public void Every_item_emits_its_name_and_its_icon_path()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "items");

            var entries = EmittedItems().ToDictionary(entry => entry.Id);

            Assert.That(entries[464].Name, Is.EqualTo("Novice Tunic"));
            Assert.That(entries[464].IconPath, Is.EqualTo("textures/icons/e0020.png"));
            Assert.That(entries[1].Name, Is.EqualTo("Short Sword"));
            Assert.That(entries[1].IconPath, Is.EqualTo("textures/icons/w0001.png"), "every segment is lowercase");
        }

        [Test]
        public void An_item_with_no_icon_emits_no_icon_path()
        {
            var blank = new ItemInfoRecord
            {
                Id = 4100, Name = "Blank", Icon = string.Empty, Type = ItemType.General,
                Modules = ["0", "0", "0", "0", "0"],
            };

            Run(Converter([blank, ItemRows[1]], rows: Row(42, "Stone01.lgo")), "items");

            var entries = EmittedItems().ToDictionary(entry => entry.Id);

            Assert.That(entries[4100].IconPath, Is.Null);
            Assert.That(entries[2000].IconPath, Is.Null, "icon 0 names no icon");
        }

        [Test]
        public void An_item_that_is_neither_worn_nor_held_in_a_hand_emits_no_model_paths()
        {
            var fairy = new ItemInfoRecord
            {
                Id = 231, Name = "Fairy of Luck", Icon = "n1651", Type = ItemType.Pet, EquipSlots = [16],
                Modules = ["10130005", "0", "0", "0", "0"],
            };
            var arrow = new ItemInfoRecord
            {
                Id = 4300, Name = "Arrow", Icon = "n0010", Type = ItemType.Arrow, EquipSlots = [-1],
                Modules = ["10120001", "01120001", "0", "0", "0"],
            };

            Run(Converter([fairy, arrow, ItemRows[0]], rows: Row(42, "Stone01.lgo")), "items");

            var entries = EmittedItems().ToDictionary(entry => entry.Id);

            Assert.That(entries.Keys, Is.EquivalentTo(new[] { 231, 4300, 464 }), "every item is in the table");
            Assert.That(entries[231].Name, Is.EqualTo("Fairy of Luck"));
            Assert.That(entries[231].IconPath, Is.EqualTo("textures/icons/n1651.png"));
            Assert.That(entries[231].EquipLinks, Is.EqualTo(new[] { 16 }));
            Assert.That(entries[231].ModelPaths, Is.Empty);
            Assert.That(entries[4300].ModelPaths, Is.Empty);
        }

        [Test]
        public void An_item_with_no_model_for_any_body_type_emits_no_model_paths()
        {
            var bare = new ItemInfoRecord
            {
                Id = 9427, Name = "Bare Hair", Type = ItemType.Hair, EquipSlots = [0],
                Modules = ["0", "0", "0", "0", "0"],
            };

            Run(Converter([bare, ItemRows[0]], rows: Row(42, "Stone01.lgo")), "items");

            var entries = EmittedItems().ToDictionary(entry => entry.Id);

            Assert.That(entries[9427].Name, Is.EqualTo("Bare Hair"));
            Assert.That(entries[9427].ModelPaths, Is.Empty);
        }

        [Test]
        public void A_worn_item_with_models_and_no_body_link_is_reported_and_emits_no_model_paths()
        {
            var odd = new ItemInfoRecord
            {
                Id = 9428, Name = "Odd Hair", Type = ItemType.Hair, EquipSlots = [-1],
                Modules = ["0", "0", "0", "0000255080", "0"],
            };

            Run(Converter([odd, ItemRows[0]], rows: Row(42, "Stone01.lgo")), "items");

            var entries = EmittedItems().ToDictionary(entry => entry.Id);

            Assert.That(entries[9428].ModelPaths, Is.Empty);
            Assert.That(_log.Warnings, Has.Some.Contains("item 9428"));
        }

        private ItemEntry EmittedItem(ItemInfoRecord item)
        {
            Run(Converter([item], rows: Row(42, "Stone01.lgo")), "items");

            return EmittedItems().Single();
        }

        private static Dictionary<CharacterStat, (int Coefficient, int Minimum, int Maximum)> Bonuses(ItemEntry entry)
        {
            return entry.Stats.ToDictionary(pair => pair.Key,
                pair => (pair.Value.Coefficient, pair.Value.Minimum, pair.Value.Maximum));
        }

        private static (int Minimum, int Maximum) Bounds(ItemRange range)
        {
            return (range.Minimum, range.Maximum);
        }

        [Test]
        public void A_weapon_emits_its_economy_its_requirements_its_stats_its_wear_and_its_sockets()
        {
            var sword = new ItemInfoRecord
            {
                Id = 1, Name = "Short Sword", Icon = "w0001", Type = ItemType.Sword,
                Modules = ["0", "01010001", "02010001", "03010070", "0"],
                ForgeLevel = 0, ForgeSteady = 7, Tradeable = true, Pickable = true, Discardable = true,
                Deletable = true, PileMax = 1, Price = 1015, NeedLevel = 10, Jobs = [1, 8, 9, 10],
                EquipSlots = [9, 6], NeedSlots = [-1], AttackSpeedCoef = 50,
                StrengthValue = [0, 10], AgilityValue = [0, 10], AccuracyValue = [0, 10],
                ConstitutionValue = [0, 10], SpiritValue = [0, 10], LuckValue = [0, 0],
                MinAttackValue = [30, 58], MaxAttackValue = [38, 70], PhysicalResist = [0, 0],
                Energy = [0, 1000], Endure = [10000, 10000], Holes = 3, AttrEffect = "0",
                Description = "Sword that is slightly shorter", Cooldown = 1f,
            };

            var entry = EmittedItem(sword);

            Assert.That(entry.Tradeable, Is.True);
            Assert.That(entry.Pickable, Is.True);
            Assert.That(entry.Discardable, Is.True);
            Assert.That(entry.Deletable, Is.True);
            Assert.That(entry.StackLimit, Is.EqualTo(1));
            Assert.That(entry.Price, Is.EqualTo(1015));
            Assert.That(entry.RequiredLevel, Is.EqualTo(10));
            Assert.That(entry.Jobs, Is.EqualTo(new[] { 1, 8, 9, 10 }));
            Assert.That(entry.EquipLinks, Is.EqualTo(new[] { 9, 6 }));
            Assert.That(entry.NeedLinks, Is.EqualTo(new[] { -1 }));
            Assert.That(Bonuses(entry), Is.EquivalentTo(new Dictionary<CharacterStat, (int, int, int)>
            {
                [CharacterStat.Strength] = (0, 0, 10),
                [CharacterStat.Agility] = (0, 0, 10),
                [CharacterStat.Accuracy] = (0, 0, 10),
                [CharacterStat.Constitution] = (0, 0, 10),
                [CharacterStat.Spirit] = (0, 0, 10),
                [CharacterStat.AttackSpeed] = (50, 0, 0),
                [CharacterStat.MinAttack] = (0, 30, 58),
                [CharacterStat.MaxAttack] = (0, 38, 70),
            }), "a stat the item does not raise emits no bonus");
            Assert.That(Bounds(entry.Durability), Is.EqualTo((10000, 10000)));
            Assert.That(Bounds(entry.Energy), Is.EqualTo((0, 1000)));
            Assert.That(entry.Sockets, Is.EqualTo(3));
            Assert.That(entry.ForgeLevel, Is.EqualTo(0));
            Assert.That(entry.ForgeCap, Is.EqualTo(7));
            Assert.That(entry.ScriptEffect, Is.Null, "script effect 0 names no script");
            Assert.That(entry.Description, Is.EqualTo("Sword that is slightly shorter"));
            Assert.That(entry.Cooldown, Is.EqualTo(1f));
        }

        [Test]
        public void An_armor_emits_its_defense_and_its_physical_resist()
        {
            var tattoo = new ItemInfoRecord
            {
                Id = 228, Name = "Raging Bull Tattoo", Icon = "h0123", Type = ItemType.Armor,
                Modules = ["0", "0", "0", "0", "0"],
                Tradeable = false, Pickable = true, Discardable = true, Deletable = true, PileMax = 1,
                Price = 31450, NeedLevel = 55, Jobs = [8], EquipSlots = [2], NeedSlots = [-1],
                StrengthValue = [0, 14], AgilityValue = [0, 14], AccuracyValue = [0, 14],
                ConstitutionValue = [0, 14], SpiritValue = [0, 14], DefenseValue = [28, 38],
                PhysicalResist = [22, 24], Energy = [0, 1000], Endure = [10000, 10000], Holes = 3,
                AttrEffect = "0", Description = string.Empty, Cooldown = 1f,
            };

            var entry = EmittedItem(tattoo);

            Assert.That(entry.Tradeable, Is.False);
            Assert.That(entry.Price, Is.EqualTo(31450));
            Assert.That(entry.RequiredLevel, Is.EqualTo(55));
            Assert.That(entry.Jobs, Is.EqualTo(new[] { 8 }));
            Assert.That(entry.EquipLinks, Is.EqualTo(new[] { 2 }));
            Assert.That(Bonuses(entry), Is.EquivalentTo(new Dictionary<CharacterStat, (int, int, int)>
            {
                [CharacterStat.Strength] = (0, 0, 14),
                [CharacterStat.Agility] = (0, 0, 14),
                [CharacterStat.Accuracy] = (0, 0, 14),
                [CharacterStat.Constitution] = (0, 0, 14),
                [CharacterStat.Spirit] = (0, 0, 14),
                [CharacterStat.Defense] = (0, 28, 38),
                [CharacterStat.PhysicalResist] = (0, 22, 24),
            }));
            Assert.That(entry.Sockets, Is.EqualTo(3));
            Assert.That(entry.Description, Is.Empty);
        }

        [Test]
        public void An_accessory_emits_both_ring_links_and_its_fixed_stats()
        {
            var ring = new ItemInfoRecord
            {
                Id = 281, Name = "Final Perseverance", Icon = "r0015", Type = ItemType.Ring,
                Modules = ["0", "0", "0", "0", "0"],
                Pickable = true, Discardable = true, Deletable = true, PileMax = 1, Price = 15216,
                NeedLevel = 20, Jobs = [-1], EquipSlots = [7, 8], NeedSlots = [-1],
                StrengthValue = [2, 2], ConstitutionValue = [1, 1], Energy = [0, 1000],
                Endure = [10000, 10000], AttrEffect = "0", Description = string.Empty, Cooldown = 1f,
            };

            var entry = EmittedItem(ring);

            Assert.That(entry.RequiredLevel, Is.EqualTo(20));
            Assert.That(entry.Jobs, Is.EqualTo(new[] { -1 }), "job -1 lets every job equip the item");
            Assert.That(entry.EquipLinks, Is.EqualTo(new[] { 7, 8 }));
            Assert.That(Bonuses(entry), Is.EquivalentTo(new Dictionary<CharacterStat, (int, int, int)>
            {
                [CharacterStat.Strength] = (0, 2, 2),
                [CharacterStat.Constitution] = (0, 1, 1),
            }));
            Assert.That(entry.Sockets, Is.Zero);
        }

        [Test]
        public void A_consumable_emits_its_stack_limit_its_script_effect_and_its_cooldown()
        {
            var crab = new ItemInfoRecord
            {
                Id = 48, Name = "Hairy Crab", Icon = "n1897", Type = ItemType.Medicine,
                Modules = ["0", "0", "0", "0", "0"],
                Tradeable = false, Pickable = false, Discardable = false, Deletable = true, PileMax = 99,
                Price = 99, Jobs = [-1], EquipSlots = [-1], NeedSlots = [-1], Energy = [0], Endure = [0, 0],
                AttrEffect = "AmplifierItem",
                Description = "Delicious Hairy Crab. Your EXP will increase to 5 times within 2 minutes.",
                Cooldown = 1f,
            };

            var entry = EmittedItem(crab);

            Assert.That(entry.Pickable, Is.False);
            Assert.That(entry.Discardable, Is.False);
            Assert.That(entry.Deletable, Is.True);
            Assert.That(entry.StackLimit, Is.EqualTo(99));
            Assert.That(entry.Price, Is.EqualTo(99));
            Assert.That(entry.Stats, Is.Empty);
            Assert.That(Bounds(entry.Energy), Is.EqualTo((0, 0)), "a missing maximum is zero");
            Assert.That(entry.ScriptEffect, Is.EqualTo("AmplifierItem"));
            Assert.That(entry.Description, Does.StartWith("Delicious Hairy Crab."));
            Assert.That(entry.Cooldown, Is.EqualTo(1f));
        }

        [Test]
        public void A_material_emits_its_price_and_its_description()
        {
            var rope = new ItemInfoRecord
            {
                Id = 57, Name = "Rope for Crab", Icon = "n1898", Type = ItemType.General,
                Modules = ["0", "0", "0", "0", "0"],
                Deletable = true, PileMax = 1, Price = 99, Jobs = [-1], EquipSlots = [-1], NeedSlots = [-1],
                AttrEffect = "0", Description = "The slope is used to strap up crabs by the Crab Farmer.",
                Cooldown = 1f,
            };

            var entry = EmittedItem(rope);

            Assert.That(entry.Tradeable, Is.False);
            Assert.That(entry.StackLimit, Is.EqualTo(1));
            Assert.That(entry.Price, Is.EqualTo(99));
            Assert.That(entry.Stats, Is.Empty);
            Assert.That(Bounds(entry.Durability), Is.EqualTo((0, 0)), "a missing range is zero");
            Assert.That(entry.Description, Is.EqualTo("The slope is used to strap up crabs by the Crab Farmer."));
        }

        [Test]
        public void A_quest_item_emits_that_it_can_be_picked_up_and_can_not_be_traded_or_thrown()
        {
            var pouch = new ItemInfoRecord
            {
                Id = 949, Name = "Herbal Pouch", Icon = "n0278", Type = ItemType.QuestItem,
                Modules = ["0", "0", "0", "0", "0"],
                Tradeable = false, Pickable = true, Discardable = false, Deletable = true, PileMax = 99,
                Price = 0, Jobs = [-1], EquipSlots = [-1], NeedSlots = [-1], AttrEffect = "0",
                Description = "Herbal Pouch", Cooldown = 1f,
            };

            var entry = EmittedItem(pouch);

            Assert.That(entry.Tradeable, Is.False);
            Assert.That(entry.Pickable, Is.True);
            Assert.That(entry.Discardable, Is.False);
            Assert.That(entry.Deletable, Is.True);
            Assert.That(entry.StackLimit, Is.EqualTo(99));
            Assert.That(entry.Price, Is.Zero);
            Assert.That(entry.Description, Is.EqualTo("Herbal Pouch"));
        }

        [Test]
        public void A_pet_item_emits_the_script_that_feeds_the_pet()
        {
            var fruit = new ItemInfoRecord
            {
                Id = 222, Name = "Snow Dragon Fruit", Icon = "n1346", Type = ItemType.PetSock,
                Modules = ["0", "0", "0", "0", "0"],
                Tradeable = true, Pickable = true, Discardable = true, Deletable = true, PileMax = 99,
                Price = 360, Jobs = [-1], EquipSlots = [-1], NeedSlots = [-1], AttrEffect = "FairyFruit",
                Description = "Snow Dragon Fruit can increases the Strength of pet", Cooldown = 1f,
            };

            var entry = EmittedItem(fruit);

            Assert.That(entry.Tradeable, Is.True);
            Assert.That(entry.StackLimit, Is.EqualTo(99));
            Assert.That(entry.Price, Is.EqualTo(360));
            Assert.That(entry.ScriptEffect, Is.EqualTo("FairyFruit"));
            Assert.That(entry.Description, Is.EqualTo("Snow Dragon Fruit can increases the Strength of pet"));
        }

        [Test]
        public void An_item_emits_its_gathering_speed_bonus()
        {
            var pickaxe = new ItemInfoRecord
            {
                Id = 9500, Name = "Gathering Pickaxe", Type = ItemType.MetalPickaxe,
                Modules = ["0", "0", "0", "0", "0"], CollectCoef = 10, CollectValue = [2, 4],
            };

            var entry = EmittedItem(pickaxe);

            Assert.That(Bonuses(entry), Is.EquivalentTo(new Dictionary<CharacterStat, (int, int, int)>
            {
                [CharacterStat.GatheringSpeed] = (10, 2, 4),
            }));
        }

        [TestCase(ItemType.Boat)]
        [TestCase(ItemType.Hull)]
        [TestCase(ItemType.Embolon)]
        [TestCase(ItemType.Mobility)]
        [TestCase(ItemType.Artillery)]
        [TestCase(ItemType.Airscrew)]
        [TestCase(ItemType.BoatSign)]
        public void A_boat_part_emits_its_identity_and_nothing_more(ItemType type)
        {
            var deed = new ItemInfoRecord
            {
                Id = 3988, Name = "Vessel Deed", Icon = "n0185", Type = type,
                Modules = ["0", "0", "0", "0", "0"],
                Pickable = true, Discardable = true, Deletable = true, PileMax = 1, Jobs = [-1],
                EquipSlots = [-1], NeedSlots = [-1], MinAttackValue = [10, 20], MaxAttackValue = [20, 30],
                AttrEffect = "0", Description = "Right click to view attributes of ship", Cooldown = 1f,
            };

            var entry = EmittedItem(deed);

            Assert.That(entry.Id, Is.EqualTo(3988));
            Assert.That(entry.Name, Is.EqualTo("Vessel Deed"));
            Assert.That(entry.IconPath, Is.EqualTo("textures/icons/n0185.png"));
            Assert.That(entry.Type, Is.EqualTo((int)type));
            Assert.That(entry.EquipLinks, Is.EqualTo(new[] { -1 }));
            Assert.That(entry.Pickable, Is.False);
            Assert.That(entry.StackLimit, Is.Zero);
            Assert.That(entry.Jobs, Is.Empty);
            Assert.That(entry.NeedLinks, Is.Empty);
            Assert.That(entry.Stats, Is.Empty);
            Assert.That(Bounds(entry.Durability), Is.EqualTo((0, 0)));
            Assert.That(entry.Description, Is.Null);
            Assert.That(entry.Cooldown, Is.Zero);
        }

        [Test]
        public void Every_main_character_and_every_monster_model_emits_one_rig_entry()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "rigs");

            var entries = EmittedRigs();

            Assert.That(entries.Select(entry => entry.Id), Is.EqualTo(new[] { 3, 55 }),
                "one entry per skeleton model, shared by every row on it, and none for a boat");
            Assert.That(entries[0].Path, Is.EqualTo("rigs/0003.glb"), "a main character animates on its rig");
            Assert.That(entries[1].Path, Is.Null, "a monster has no rig of its own");
        }

        [Test]
        public void The_shipped_main_character_action_sets_decode_onto_the_stance_grid()
        {
            ClientTables tables;

            using (var characters = File.OpenRead(Fixtures.Path("tables/characterinfo.txt")))
            using (var actions = File.OpenRead(Fixtures.Path("tables/CharacterAction.tx")))
            {
                tables = new ClientTables(Top.Legacy.Tables.TableFile.Read<CharacterInfoRecord>(characters),
                    null, null, CharacterActionTable.Read(actions));
            }

            Run(new TableConverter(_client.Settings(), tables), "rigs");

            var rigs = EmittedRigs().ToDictionary(entry => entry.Id);
            var lance = rigs[0];

            Assert.That(lance.Actions, Has.Length.EqualTo(89), "245 poses of 38 actions");
            Assert.That(lance.Actions.Count(action => action.Stance == null), Is.EqualTo(35),
                "one for each action with a fists pose");
            Assert.That(lance.Actions.Where(action => action.Id == 4).Select(action => (action.Stance, action.Clip)),
                Is.EqualTo(new[]
                {
                    ((Stance?)null, "04_waiting2"),
                    (Stance.TwoHandSword, "04_waiting2_two_hand_sword"),
                    (Stance.DualSwords, "04_waiting2_dual_swords"),
                    (Stance.Firegun, "04_waiting2_firegun"),
                    (Stance.Bow, "04_waiting2_bow"),
                }), "guard waiting is the same in fists, sword and dagger");
            Assert.That(rigs[1].Actions.Where(action => action.Stance >= Stance.Firegun), Is.Empty,
                "rig 1 has four stances");
            Assert.That(rigs[2].Actions.Where(action => action.Id == 28).Select(action => action.Stance),
                Is.EqualTo(new Stance?[] { null, Stance.Firegun, Stance.Bow, Stance.Dagger }),
                "mining keeps its range in every stance, but only these three carry its key frame");
            Assert.That(lance.Actions.Single(action => action.Id == 5).Events.Select(e => (e.Name, e.Time)),
                Is.EqualTo(new[] { ("footstep", 0f), ("footstep", 0.267f) }), "a run cycle takes two steps");

            foreach (var rig in rigs.Values)
            {
                Assert.That(rig.Actions.Select(action => action.Clip).Distinct(),
                    Is.SubsetOf(rig.Clips.Select(clip => clip.Name)), $"rig {rig.Id} declares every clip its actions play");
            }
        }

        [Test]
        public void The_shipped_main_character_poses_that_play_one_frame_range_share_one_clip()
        {
            ClientTables tables;

            using (var characters = File.OpenRead(Fixtures.Path("tables/characterinfo.txt")))
            using (var actions = File.OpenRead(Fixtures.Path("tables/CharacterAction.tx")))
            {
                tables = new ClientTables(Top.Legacy.Tables.TableFile.Read<CharacterInfoRecord>(characters),
                    null, null, CharacterActionTable.Read(actions));
            }

            Run(new TableConverter(_client.Settings(), tables), "rigs");

            var mainCharacterRigs = EmittedRigs().Where(entry => entry.Path != null).ToDictionary(entry => entry.Id);
            var lance = mainCharacterRigs[0];

            Assert.That(mainCharacterRigs.Keys, Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(mainCharacterRigs.Values.Sum(rig => rig.Clips.Length), Is.EqualTo(236), "725 poses in 236 frame ranges");
            Assert.That(lance.Actions.Where(action => action.Id is 7 or 10 && action.Stance is Stance.Sword or Stance.Dagger)
                    .Select(action => (action.Id, action.Stance, action.Clip)),
                Is.EqualTo(new[]
                {
                    (7, (Stance?)Stance.Sword, "07_attack_sword"),
                    (7, (Stance?)Stance.Dagger, "07_attack_sword"),
                    (10, (Stance?)Stance.Sword, "07_attack_sword"),
                    (10, (Stance?)Stance.Dagger, "07_attack_sword"),
                }), "the clip is named after the lowest action and, in it, the first stance that plays it");
            Assert.That(lance.Actions.Where(action => action.Id is 15 or 40 && action.Stance == Stance.DualSwords)
                    .Select(action => action.Clip),
                Is.EqualTo(new[] { "15_skill5_dual_swords", "15_skill5_dual_swords" }),
                "an action with no fists pose names its clips after its stance");
        }

        [Test]
        public void A_main_character_pose_outside_the_stance_grid_is_reported_and_left_out()
        {
            var tables = new ClientTables(new Top.Legacy.Tables.Table<CharacterInfoRecord>([CharacterRows[0]]),
                null, null, ReadActions("4\n\\t1\\t10\\t20\n\\t379\\t30\\t40\n"));

            Run(new TableConverter(_client.Settings(), tables), "rigs");

            var rig = EmittedRigs().Single();

            Assert.That(rig.Actions.Select(action => action.Clip), Is.EqualTo(new[] { "01_waiting" }),
                "seven stance columns of 54 actions end at pose 378");
            Assert.That(_log.Warnings, Has.Some.Contains("pose 379"));
        }

        [Test]
        public void Main_character_frame_ranges_a_frame_apart_are_reported_and_stay_two_clips()
        {
            var tables = new ClientTables(new Top.Legacy.Tables.Table<CharacterInfoRecord>([CharacterRows[0]]),
                null, null, ReadActions("4\n\\t1\\t10\\t20\n\\t2\\t11\\t21\n\\t3\\t13\\t21\n"));

            Run(new TableConverter(_client.Settings(), tables), "rigs");

            var rig = EmittedRigs().Single();

            Assert.That(rig.Clips.Select(clip => clip.Name), Is.EqualTo(new[] { "01_waiting", "02_show", "03_sleep" }));
            Assert.That(_log.Warnings.Where(warning => warning.Contains("frame apart")), Is.EqualTo(new[]
            {
                "rig 0003: clips 01_waiting (10-20) and 02_show (11-21) are a frame apart, they stay two clips",
            }), "a range two frames off at one end is no near duplicate");
        }

        [Test]
        public void A_rig_table_without_the_action_table_fails()
        {
            var tables = new ClientTables(new Top.Legacy.Tables.Table<CharacterInfoRecord>([..CharacterRows]),
                null, null, null);

            var result = Run(new TableConverter(_client.Settings(), tables), "rigs");

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(File.Exists(Path.Combine(_client.OutputRoot, "tables", "rigs.json")), Is.False);
            Assert.That(_log.Errors, Has.Some.Contains("rigs"));
        }

        [Test]
        public void A_monster_plays_the_clips_baked_into_its_own_model()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "rigs");

            var monster = EmittedRigs().Single(entry => entry.Id == 55);

            Assert.That(monster.Clips.Select(clip => (clip.Name, clip.File, clip.Animation)), Is.EqualTo(new[]
            {
                ("0055_01_waiting", "models/character/0055.glb", "0055_01_waiting"),
                ("0055_02_show", "models/character/0055.glb", "0055_02_show"),
            }));
            Assert.That(monster.Actions.Select(action => (action.Id, action.Stance, action.Clip)), Is.EqualTo(new[]
            {
                (1, (Stance?)null, "0055_01_waiting"),
                (2, (Stance?)null, "0055_02_show"),
            }), "a monster has no stance grid");
        }

        [Test]
        public void A_main_character_action_the_same_in_every_stance_emits_one_stance_less_row()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "rigs");

            var rig = EmittedRigs().Single(entry => entry.Id == 3);

            Assert.That(rig.Actions.Where(action => action.Id == 1).Select(action => (action.Stance, action.Clip)),
                Is.EqualTo(new[] { ((Stance?)null, "01_waiting") }));

            var waiting = rig.Clips.Single(clip => clip.Name == "01_waiting");

            Assert.That(waiting.File, Is.EqualTo("animations/0003/01_waiting.glb"), "a clip lives in a file of its own");
            Assert.That(waiting.Animation, Is.EqualTo("01_waiting"));
        }

        [Test]
        public void A_main_character_action_that_differs_by_stance_emits_a_row_only_for_each_stance_that_differs()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "rigs");

            var rig = EmittedRigs().Single(entry => entry.Id == 3);

            Assert.That(rig.Actions.Where(action => action.Id == 4).Select(action => (action.Stance, action.Clip)),
                Is.EqualTo(new[]
                {
                    ((Stance?)null, "04_waiting2"),
                    (Stance.TwoHandSword, "04_waiting2_two_hand_sword"),
                }), "the sword range is the fists range, so the sword plays the stance-less row");

            var twoHandSword = rig.Clips.Single(clip => clip.Name == "04_waiting2_two_hand_sword");

            Assert.That(twoHandSword.File, Is.EqualTo("animations/0003/04_waiting2_two_hand_sword.glb"));
            Assert.That(twoHandSword.Animation, Is.EqualTo("04_waiting2_two_hand_sword"));
        }

        [Test]
        public void A_main_character_stance_with_the_fists_range_but_other_key_frames_emits_a_row_that_plays_the_fists_clip()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "rigs");

            var rig = EmittedRigs().Single(entry => entry.Id == 3);

            Assert.That(rig.Actions.Where(action => action.Id == 5).Select(action => (action.Stance, action.Clip)),
                Is.EqualTo(new[]
                {
                    ((Stance?)null, "05_run"),
                    (Stance.Sword, "05_run"),
                }), "the events of an action follow its key frames, and its clip its frame range alone");
            Assert.That(rig.Clips.Select(clip => clip.Name), Has.No.Member("05_run_sword"));
        }

        [Test]
        public void A_main_character_action_emits_its_key_frames_as_named_events_in_seconds_from_clip_start()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "rigs");

            var rig = EmittedRigs().Single(entry => entry.Id == 3);

            Assert.That(rig.Actions.Where(action => action.Id == 5)
                    .Select(action => (action.Stance, action.Events.Single().Name, action.Events.Single().Time)),
                Is.EqualTo(new[]
                {
                    ((Stance?)null, "footstep", 0.067f),
                    (Stance.Sword, "footstep", 0.167f),
                }), "key frames 92 and 95 of the range 90-99, at 30 frames a second");
            Assert.That(rig.Actions.Single(action => action.Id == 1).Events, Is.Null, "a pose with no key frame has no events");
        }

        [Test]
        public void A_monster_action_emits_its_key_frames_as_events_and_one_the_names_do_not_cover_as_a_mark()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "rigs");

            var monster = EmittedRigs().Single(entry => entry.Id == 55);

            Assert.That(monster.Actions.Select(action => action.Events.Select(e => (e.Name, e.Time))), Is.EqualTo(new[]
            {
                new[] { ("breath", 0f), ("breath", 0.3f) },
                new[] { ("mark", 0.133f) },
            }), "waiting breathes, and show is not in the name table");
        }

        [Test]
        public void An_event_is_named_after_the_logical_action_that_carries_it()
        {
            int[] actions = [1, 2, 4, 5, 6, 7, 11, 15, 16, 17, 28, 29, 38, 40, 41, 42];
            var rows = string.Concat(actions.Select(action => $"\\t{action}\\t{action * 10}\\t{action * 10 + 5}\\t{action * 10}\n"));
            var tables = new ClientTables(new Top.Legacy.Tables.Table<CharacterInfoRecord>([CharacterRows[1]]),
                null, null, ReadActions($"9\n{rows}"));

            Run(new TableConverter(_client.Settings(), tables), "rigs");

            Assert.That(EmittedRigs().Single().Actions.Select(action => (action.Id, action.Events.Single().Name)),
                Is.EqualTo(new[]
                {
                    (1, "breath"), (2, "mark"), (4, "breath"), (5, "footstep"), (6, "footstep"), (7, "hit"), (11, "hit"),
                    (15, "hit"), (16, "mark"), (17, "death"), (28, "mine"), (29, "gather"), (38, "beat"), (40, "hit"),
                    (41, "hit"), (42, "mark"),
                }));
        }

        [Test]
        public void A_key_frame_outside_its_pose_is_reported_and_left_out()
        {
            var tables = new ClientTables(new Top.Legacy.Tables.Table<CharacterInfoRecord>([CharacterRows[0]]),
                null, null, ReadActions("4\n\\t7\\t10\\t20\\t9\\t20\\t21\n"));

            var result = Run(new TableConverter(_client.Settings(), tables), "rigs");

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(EmittedRigs().Single().Actions.Single().Events.Select(e => (e.Name, e.Time)),
                Is.EqualTo(new[] { ("hit", 0.333f) }), "the end frame is the last frame of the clip");
            Assert.That(_log.Warnings.Where(warning => warning.Contains("key frame")), Is.EqualTo(new[]
            {
                "rig 0003: pose 7 has key frame 9 outside its range 10-20, it is left out",
                "rig 0003: pose 7 has key frame 21 outside its range 10-20, it is left out",
            }));
        }

        [Test]
        public void A_main_character_action_with_no_fists_pose_emits_only_its_stance_rows()
        {
            Run(Converter(rows: Row(42, "Stone01.lgo")), "rigs");

            var rig = EmittedRigs().Single(entry => entry.Id == 3);

            Assert.That(rig.Actions.Where(action => action.Id == 40).Select(action => (action.Stance, action.Clip)),
                Is.EqualTo(new[] { ((Stance?)Stance.DualSwords, "40_skill6_dual_swords") }));
        }

        [Test]
        public void A_batch_is_every_table_unit()
        {
            var results = Converter(rows: Row(42, "Stone01.lgo")).ConvertAll().ToList();

            Assert.That(results.Select(result => result.Name), Is.EqualTo(new[]
            {
                "sceneobjects", "terrains", "maps", "areas", "tips", "characters", "jobs", "portraitcameras", "items", "itemtypes", "itemprefixes", "gems", "rigs", "actionsets", "ships", "shipparts",
            }));
            Assert.That(results.Select(result => result.Outcome),
                Is.All.EqualTo(ConversionOutcome.Converted));
            Assert.That(results[0].Artifacts, Is.Empty);
        }

        [Test]
        public void A_wearable_item_emits_the_characters_that_can_wear_it()
        {
            var sword = new ItemInfoRecord
            {
                Id = 1, Name = "Short Sword", Type = ItemType.Sword, Modules = ["0", "0", "0", "0", "0"],
                Body = [1, 2, 3], EquipSlots = [9, 6],
            };

            Assert.That(EmittedItem(sword).Bodies, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void An_item_any_character_can_wear_emits_no_bodies()
        {
            var ring = new ItemInfoRecord
            {
                Id = 4200, Name = "Final Perseverance", Type = ItemType.Ring, Modules = ["0", "0", "0", "0", "0"],
                Body = [-1], EquipSlots = [7, 8],
            };

            Assert.That(EmittedItem(ring).Bodies, Is.Empty);
        }

        [Test]
        public void A_prefix_row_emits_its_name_and_a_row_named_0_emits_nothing()
        {
            Run(Converter(), "itemprefixes");

            var entries = EmittedTable<ItemPrefixEntry>("itemprefixes.json").ToDictionary(entry => entry.Id);

            Assert.That(entries.Keys, Is.EquivalentTo(new[] { 1, 36 }));
            Assert.That(entries[1].Name, Is.EqualTo("Mammoth"));
            Assert.That(entries[36].Name, Is.EqualTo("Uncommon"));
        }

        [Test]
        public void A_gem_emits_its_name_its_item_its_colour_and_its_bonus_per_level()
        {
            Run(Converter(), "gems");

            var gem = EmittedTable<GemEntry>("gems.json").Single(entry => entry.Id == 1);

            Assert.That(gem.Name, Is.EqualTo("Fiery Gem"));
            Assert.That(gem.ItemId, Is.EqualTo(878));
            Assert.That(gem.Color, Is.EqualTo(0xF6D243));
            Assert.That(gem.Bonus.Text, Is.EqualTo("Attack +"), "the lead every hint repeats is dropped");
            Assert.That(gem.Bonus.PerLevel, Is.EqualTo(4));
            Assert.That(gem.Fits, Is.EqualTo(new[] { 1 }), "the item types the gem can be socketed into");
            Assert.That(gem.MaxLevel, Is.EqualTo(9), "the top gem level of the reference server");
        }

        [Test]
        public void A_gem_fits_no_item_type_0()
        {
            Run(Converter(), "gems");

            var gem = EmittedTable<GemEntry>("gems.json").Single(entry => entry.Id == 17);

            Assert.That(gem.Fits, Is.EqualTo(new[] { 14 }));
        }

        [Test]
        public void A_gem_bonus_keeps_a_hint_text_that_has_no_gem_bonus_lead()
        {
            Run(Converter(), "gems");

            var gem = EmittedTable<GemEntry>("gems.json").Single(entry => entry.Id == 10);

            Assert.That(gem.Bonus.Text, Is.EqualTo("Added Accuracy"));
            Assert.That(gem.Bonus.PerLevel, Is.EqualTo(5));
        }

        [Test]
        public void A_gem_whose_hint_is_not_a_level_formula_is_reported_and_emits_no_bonus()
        {
            Run(Converter(), "gems");

            var gem = EmittedTable<GemEntry>("gems.json").Single(entry => entry.Id == 40);

            Assert.That(gem.Name, Is.EqualTo("Key of Apollo"));
            Assert.That(gem.Bonus, Is.Null);
            Assert.That(_log.Warnings, Has.Some.Contains("gem 40"));
        }

        [Test]
        public void The_item_type_table_names_the_types_a_bag_holds_and_no_boat_part_type()
        {
            Run(Converter(), "itemtypes");

            var entries = EmittedTable<ItemTypeEntry>("itemtypes.json").ToDictionary(entry => entry.Id);

            Assert.That(entries[1].Name, Is.EqualTo("Sword"));
            Assert.That(entries[22].Name, Is.EqualTo("Body armor"));
            Assert.That(entries[42].Name, Is.EqualTo("Quest item"));
            Assert.That(entries[49].Name, Is.EqualTo("Gem"));
            Assert.That(entries[81].Name, Is.EqualTo("Bracelet"));
            Assert.That(entries.Keys, Has.None.AnyOf(43, 51, 52, 53, 54, 55, 56));
        }
    }
}
