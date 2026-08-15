using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Top.Contracts.Tables.World;

namespace Top.Contracts.Tables.Tests
{
    public class SceneObjectTableTests
    {
        private static List<SceneObjectEntry> Read(string json)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            return new TableFormat().Read<SceneObjectEntry>(stream);
        }

        private static string Write(params SceneObjectEntry[] entries)
        {
            using var stream = new MemoryStream();

            new TableFormat().Write(stream, entries);

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        [Test]
        public void Json_reads_as_typed_entries()
        {
            var entries = Read("""
                [
                  { "id": 42, "modelPath": "models/scene/stone01.glb", "displayName": "stone",
                    "isReallyBig": true },
                  { "id": 7, "type": 3, "color": [255, 128, 0], "range": 500 }
                ]
                """);

            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries[0].Id, Is.EqualTo(42));
            Assert.That(entries[0].ModelPath, Is.EqualTo("models/scene/stone01.glb"));
            Assert.That(entries[0].DisplayName, Is.EqualTo("stone"));
            Assert.That(entries[0].IsReallyBig, Is.True);

            var light = (PointLightEntry)entries[1];

            Assert.That(light.Type, Is.EqualTo(3));
            Assert.That(light.Color, Is.EqualTo(new[] { 255, 128, 0 }));
            Assert.That(light.Range, Is.EqualTo(500));
        }

        [Test]
        public void The_type_names_the_variation_a_row_reads_as()
        {
            var entries = Read("""
                [
                  { "id": 1, "sequence": [1, 2], "coefficient": 0.5 },
                  { "id": 2, "type": 4, "color": [10, 20, 30] },
                  { "id": 3, "type": 5, "color": [1, 2, 3] },
                  { "id": 4, "type": 6, "sound": "wave.wav", "distance": 900 },
                  { "id": 5, "type": 2 },
                  { "id": 6 }
                ]
                """);

            Assert.That(entries[0], Is.InstanceOf<FadeEntry>());
            Assert.That(entries[1], Is.InstanceOf<AmbientLightEntry>());
            Assert.That(entries[2], Is.InstanceOf<FogEntry>());
            Assert.That(entries[3], Is.InstanceOf<SoundEntry>());
            Assert.That(entries[4].GetType(), Is.EqualTo(typeof(SceneObjectEntry)),
                "a type without parameters reads as the base");
            Assert.That(entries[5].GetType(), Is.EqualTo(typeof(SceneObjectEntry)),
                "an ordinary object without fade data reads as the base");
        }

        [Test]
        public void Absent_fields_read_as_defaults()
        {
            var entry = Read("""[ { "id": 7 } ]""").Single();

            Assert.That(entry.ModelPath, Is.Null);
            Assert.That(entry.Type, Is.Zero);
            Assert.That(entry.EnableEnvLight, Is.False);
        }

        [Test]
        public void A_field_from_a_later_format_is_ignored()
        {
            var entries = Read("""[ { "id": 7, "someLaterField": { "x": 0.5 } } ]""");

            Assert.That(entries.Single().Id, Is.EqualTo(7));
        }

        [Test]
        public void An_id_of_zero_still_writes_its_id_member()
        {
            var json = Write(new SceneObjectEntry { Id = 0 });

            Assert.That(json, Does.Contain("\"id\": 0"), "every entry carries the catalog id");
        }

        [Test]
        public void Entries_round_trip()
        {
            var json = Write(
                new FadeEntry
                {
                    Id = 42,
                    ModelPath = "models/scene/stone01.glb",
                    DisplayName = "stone",
                    AttachEffectId = 5,
                    EnableEnvLight = true,
                    EnablePointLight = true,
                    Style = 1,
                    Flag = 2,
                    SizeFlag = 3,
                    ShadeFlag = true,
                    IsReallyBig = true,
                    Sequence = new[] { 1, 2 },
                    Coefficient = 0.5f,
                },
                new PointLightEntry
                {
                    Id = 7,
                    Type = 3,
                    Color = new[] { 255, 128, 0 },
                    Range = 500,
                    Attenuation = 0.7f,
                    AnimationId = 9,
                });

            var entries = Read(json);

            var faded = (FadeEntry)entries[0];

            Assert.That(faded.AttachEffectId, Is.EqualTo(5));
            Assert.That(faded.EnableEnvLight, Is.True);
            Assert.That(faded.EnablePointLight, Is.True);
            Assert.That(faded.Style, Is.EqualTo(1));
            Assert.That(faded.Flag, Is.EqualTo(2));
            Assert.That(faded.SizeFlag, Is.EqualTo(3));
            Assert.That(faded.ShadeFlag, Is.True);
            Assert.That(faded.Sequence, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(faded.Coefficient, Is.EqualTo(0.5f));

            var light = (PointLightEntry)entries[1];

            Assert.That(light.Color, Is.EqualTo(new[] { 255, 128, 0 }));
            Assert.That(light.Range, Is.EqualTo(500));
            Assert.That(light.Attenuation, Is.EqualTo(0.7f));
            Assert.That(light.AnimationId, Is.EqualTo(9));
        }

        [Test]
        public void The_table_lives_at_one_tree_path()
        {
            Assert.That(SceneObjectTable.Path, Is.EqualTo("tables/sceneobjects.json"));
        }

        [Test]
        public void The_table_looks_up_by_id_and_keeps_file_order()
        {
            var table = new SceneObjectTable(new[]
            {
                new SceneObjectEntry { Id = 42 },
                new SceneObjectEntry { Id = 7 },
            });

            Assert.That(table.Select(entry => entry.Id), Is.EqualTo(new[] { 42, 7 }));
            Assert.That(table.TryGetById(7, out var entry), Is.True);
            Assert.That(entry.Id, Is.EqualTo(7));
            Assert.That(table.TryGetById(9000, out _), Is.False);
        }

        [Test]
        public void Duplicate_ids_keep_the_last_entry()
        {
            var table = new SceneObjectTable(new[]
            {
                new SceneObjectEntry { Id = 42, DisplayName = "first" },
                new SceneObjectEntry { Id = 42, DisplayName = "last" },
            });

            Assert.That(table[42].DisplayName, Is.EqualTo("last"));
            Assert.That(table.Count, Is.EqualTo(2), "enumeration keeps every row in file order");
        }
    }
}
