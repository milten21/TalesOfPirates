using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Top.Client.Core;
using Top.Content;
using Top.Contracts.Tables.Characters;
using Top.Contracts.Tables.Items;
using Top.Contracts.Tables.Rigs;
using Top.Contracts.Tables.Ships;
using Top.Contracts.Tables.World;

namespace Top.Client.Tests.Core
{
    public class TableReaderTests
    {
        private CountedContent _content;
        private TableReader _reader;

        [SetUp]
        public void SetUp()
        {
            _content = new CountedContent();

            _content.Add(MapTable.Path, """[{ "id": 1, "name": "garner", "mapPath": "maps/garner.map" }]""");
            _content.Add(AreaTable.Path, """[{ "id": 1, "name": "Argent City", "isCity": true }]""");
            _content.Add(TipTable.Path, """[{ "id": 1, "text": "You can reset your Stats using Stat Reset." }]""");
            _content.Add(JobTable.Path, """[{ "id": 0, "name": "Newbie" }, { "id": 1, "name": "Swordsman" }]""");
            _content.Add(PortraitCameraTable.Path,
                """
                [
                  { "id": 0, "angle": 30, "distance": 3.0, "height": 0.6 },
                  { "id": 1, "angle": 30, "distance": 1.5, "height": 0.5 }
                ]
                """);
            _content.Add(TerrainTable.Path, """[{ "id": 3, "texturePath": "textures/terrain/sand01.png" }]""");
            _content.Add(SceneObjectTable.Path, """[{ "id": 42, "kind": "Model" }]""");
            _content.Add(CharacterTable.Path,
                """[{ "id": 6, "name": "Weird Monster", "modalType": "Other", "height": 9.0, "radius": 20 }]""");
            _content.Add(ItemTable.Path,
                """
                [
                  { "id": 464, "name": "Novice Tunic", "iconPath": "textures/icons/e0020.png", "equipLinks": [2],
                    "modelPaths": ["models/character/0000610002.glb", null, null, null] },
                  { "id": 231, "name": "Fairy of Luck", "iconPath": "textures/icons/n1651.png", "equipLinks": [16],
                    "modelPaths": [] }
                ]
                """);
            _content.Add(RigTable.Path,
                """
                [{
                  "id": 0,
                  "path": "rigs/0000.glb",
                  "clips": [
                    { "name": "0000_04_waiting2", "file": "rigs/0000.glb", "animation": "0000_04_waiting2" },
                    { "name": "0000_112", "file": "rigs/0000.glb", "animation": "0000_112" }
                  ],
                  "actions": [
                    { "id": 4, "clip": "0000_04_waiting2" },
                    { "id": 4, "stance": "two_hand_sword", "clip": "0000_112" }
                  ]
                }]
                """);
            _content.Add(ActionSetTable.Path,
                """
                [{
                  "id": 500,
                  "path": null,
                  "clips": [ { "name": "01_waiting", "file": null, "animation": "01_waiting" } ],
                  "actions": [ { "id": 1, "clip": "01_waiting" }, { "id": 17, "clip": "01_waiting" } ]
                }]
                """);
            _content.Add(ShipTable.Path, """[{ "id": 12, "hullId": 2 }]""");
            _content.Add(ShipPartTable.Path,
                """[{ "id": 143, "modelPath": "models/machine/2000010002.glb", "propellerIds": [23, 24, 25, 26] }]""");

            _reader = new TableReader(_content);
        }

        [Test]
        public async Task A_table_is_read_the_first_time_it_is_asked_for()
        {
            var characters = await _reader.ReadCharacters();

            Assert.That(characters.TryGetById(6, out var entry), Is.True);
            Assert.That(entry.Name, Is.EqualTo("Weird Monster"));
            Assert.That(entry.ModalType, Is.EqualTo(CharacterModalType.Other));
            Assert.That(entry.Height, Is.EqualTo(9f));
            Assert.That(entry.Radius, Is.EqualTo(20));
        }

        [Test]
        public async Task A_table_asked_for_again_is_the_one_already_read()
        {
            var first = await _reader.ReadMaps();
            var second = await _reader.ReadMaps();

            Assert.That(second, Is.SameAs(first));
            Assert.That(_content.Reads, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task A_table_asked_for_twice_while_it_is_read_is_read_once()
        {
            _content.Gate = new TaskCompletionSource();

            var first = _reader.ReadMaps();
            var second = _reader.ReadMaps();

            _content.Gate.SetResult();

            Assert.That(await second, Is.SameAs(await first));
            Assert.That(_content.Reads, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task Asking_for_one_table_leaves_the_rest_unread()
        {
            await _reader.ReadTerrains();

            Assert.That(_content.Reads, Is.EqualTo(new[] { TerrainTable.Path }));
        }

        [Test]
        public async Task Every_kind_of_table_is_reached_through_the_same_reader()
        {
            Assert.That((await _reader.ReadMaps())[1].Name, Is.EqualTo("garner"));
            Assert.That((await _reader.ReadAreas())[1].Name, Is.EqualTo("Argent City"));
            Assert.That((await _reader.ReadAreas())[1].IsCity, Is.True);
            Assert.That((await _reader.ReadTips())[1].Text, Is.EqualTo("You can reset your Stats using Stat Reset."));
            Assert.That((await _reader.ReadTerrains())[3].TexturePath, Is.EqualTo("textures/terrain/sand01.png"));
            Assert.That((await _reader.ReadSceneObjects())[42].Kind, Is.EqualTo(SceneObjectKind.Model));
            Assert.That((await _reader.ReadCharacters())[6].Name, Is.EqualTo("Weird Monster"));
            Assert.That((await _reader.ReadItems())[464].EquipLinks, Is.EqualTo(new[] { 2 }));
            Assert.That((await _reader.ReadItems())[231].Name, Is.EqualTo("Fairy of Luck"));
            Assert.That((await _reader.ReadItems())[231].IconPath, Is.EqualTo("textures/icons/n1651.png"));
            Assert.That((await _reader.ReadJobs())[1].Name, Is.EqualTo("Swordsman"));
            Assert.That((await _reader.ReadPortraitCameras())[1].Distance, Is.EqualTo(1.5f));
            Assert.That((await _reader.ReadShips())[12].HullId, Is.EqualTo(2));
            Assert.That((await _reader.ReadShipParts())[143].PropellerIds, Is.EqualTo(new[] { 23, 24, 25, 26 }));
        }

        [Test]
        public async Task A_job_index_with_no_entry_reads_as_the_name_of_job_zero()
        {
            var jobs = await _reader.ReadJobs();

            Assert.That(jobs.NameOf(1), Is.EqualTo("Swordsman"));
            Assert.That(jobs.NameOf(19), Is.EqualTo("Newbie"));
            Assert.That(jobs.NameOf(-1), Is.EqualTo("Newbie"));
        }

        [Test]
        public async Task A_character_type_with_no_portrait_camera_reads_as_the_camera_of_entry_zero()
        {
            var cameras = await _reader.ReadPortraitCameras();

            Assert.That(cameras.FindCamera(1).Height, Is.EqualTo(0.5f));
            Assert.That(cameras.FindCamera(789).Id, Is.EqualTo(0));
            Assert.That(cameras.FindCamera(789).Distance, Is.EqualTo(3f));
        }

        [Test]
        public async Task A_content_pack_with_no_tip_table_reads_as_no_tips()
        {
            var reader = new TableReader(new CountedContent());

            Assert.That(await reader.ReadTips(), Is.Empty);
        }

        [Test]
        public async Task An_action_set_resolves_an_action_to_a_clip_in_the_part_that_plays_it()
        {
            var tower = (await _reader.ReadActionSets())[500];

            Assert.That(tower.FindClip(17, Stance.Fists).Animation, Is.EqualTo("01_waiting"));
            Assert.That(tower.FindClip(17, Stance.Fists).File, Is.Null);
        }

        [Test]
        public async Task A_worn_item_carries_a_null_where_a_body_type_has_no_mesh()
        {
            var item = (await _reader.ReadItems())[464];

            Assert.That(item.ModelPaths, Is.EqualTo(new[] { "models/character/0000610002.glb", null, null, null }));
        }

        [Test]
        public async Task A_rig_resolves_an_action_through_its_table_rows()
        {
            var rig = (await _reader.ReadRigs())[0];

            Assert.That(rig.Path, Is.EqualTo("rigs/0000.glb"));
            Assert.That(rig.FindClip(4, Stance.TwoHandSword).Animation, Is.EqualTo("0000_112"));
            Assert.That(rig.FindClip(4, Stance.Bow).Animation, Is.EqualTo("0000_04_waiting2"),
                "a row with no stance plays for any stance");
        }

        private class CountedContent : IContentSource
        {
            private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>();

            public List<string> Reads { get; } = new List<string>();

            public TaskCompletionSource Gate { get; set; }

            public void Add(string path, string json)
            {
                _files[path] = Encoding.UTF8.GetBytes(json);
            }

            public async Task<byte[]> Read(string path)
            {
                Reads.Add(path);

                if (Gate != null)
                {
                    await Gate.Task;
                }

                return _files[path];
            }

            public bool Exists(string path)
            {
                return _files.ContainsKey(path);
            }

            public IEnumerable<string> List(string prefix)
            {
                return _files.Keys.Where(path => path.StartsWith(prefix));
            }
        }
    }
}
