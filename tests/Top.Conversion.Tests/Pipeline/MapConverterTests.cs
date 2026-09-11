using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Conversion.Pipeline;
using Top.Legacy.Tables;
using Top.Legacy.Tables.Records;
using Converted = Top.Contracts.Assets.Maps;
using Original = Top.Legacy.MindPower.World;

namespace Top.Conversion.Tests.Pipeline
{
    public class MapConverterTests
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

        private const int UnderwaterTerrain = 22;

        private static TerrainInfoRecord Terrain(int id, string fileName)
        {
            return new TerrainInfoRecord { Id = id, Name = fileName };
        }

        private MapConverter Converter(bool overwrite = true, params TerrainInfoRecord[] terrain)
        {
            var tables = new ClientTables(null, null, null, null, new Table<TerrainInfoRecord>([..terrain]));

            return new MapConverter(_client.Settings(overwrite), tables);
        }

        private Converted.MapFile Read(string name)
        {
            using var stream = File.OpenRead(_client.ConvertedMap(name));

            return Converted.MapFile.Read(stream);
        }

        private static Converted.MapTile TileAt(Converted.MapFile map, int x, int y)
        {
            var chunk = map.Chunks[x / map.ChunkSize, y / map.ChunkSize];

            return chunk.Tiles[((y % map.ChunkSize) * map.ChunkSize) + (x % map.ChunkSize)];
        }

        private static Original.MapSection PaintedSection(bool oldFormat)
        {
            return LegacyMaps.Section(index =>
            {
                var tile = LegacyMaps.Tile();

                if (index != 0)
                {
                    return tile;
                }

                tile.Texture0 = 1;
                tile.Alpha0 = (byte)(oldFormat ? 3 : 15);
                tile.Texture1 = 2;
                tile.Alpha1 = 7;
                tile.Texture2 = 3;
                tile.Alpha2 = 4;
                tile.Region = 5;
                tile.Island = 9;
                tile.Block = new byte[] { 0x80, 0x41, 0x02, 0xC3 };

                if (oldFormat)
                {
                    tile.HeightCm = 250;
                    tile.Color888 = 0xFFA04410;
                }
                else
                {
                    tile.HeightStep = 25;
                    tile.Color565 = 0x1234;
                }

                return tile;
            });
        }

        private static readonly TerrainInfoRecord[] Textures =
        [
            Terrain(1, "texture/terrain/Grass05.bmp"),
            Terrain(2, "texture/terrain/Sand01.bmp"),
            Terrain(3, "texture/terrain/Rock02.bmp"),
            Terrain(UnderwaterTerrain, "texture/terrain/Brick06.bmp"),
        ];

        [Test]
        public void Every_layer_survives_the_round_trip_through_the_format()
        {
            var terrain = LegacyMaps.Terrain(LegacyMaps.NewFormat, 64, 64);
            terrain.Sections[0] = PaintedSection(oldFormat: false);

            _client.AddMap("DreamIsland", terrain);

            var result = Converter(terrain: Textures).ConvertAll().Single();

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Converted));

            var map = Read("dreamisland");

            Assert.That(map.Width, Is.EqualTo(64));
            Assert.That(map.Height, Is.EqualTo(64));
            Assert.That(map.ChunkSize, Is.EqualTo(64));

            var tile = TileAt(map, 0, 0);

            Assert.That(tile.Height, Is.EqualTo(2.5f), "height steps of 10 cm become meters");
            Assert.That(tile.ColorR, Is.EqualTo(160));
            Assert.That(tile.ColorG, Is.EqualTo(68));
            Assert.That(tile.ColorB, Is.EqualTo(16));
            Assert.That(tile.Layer0.TerrainId, Is.EqualTo(1), "the original terrain id is kept");
            Assert.That(tile.Layer0.MaskIndex, Is.EqualTo(15), "the base layer always covers fully");
            Assert.That(tile.Layer1.TerrainId, Is.EqualTo(2));
            Assert.That(tile.Layer1.MaskIndex, Is.EqualTo(7));
            Assert.That(tile.Layer2.TerrainId, Is.EqualTo(3));
            Assert.That(tile.Layer2.MaskIndex, Is.EqualTo(4));
            Assert.That(tile.Layer3.TerrainId, Is.EqualTo(0), "an unpainted layer names no texture");
            Assert.That(tile.Region, Is.EqualTo(5));
            Assert.That(tile.Island, Is.EqualTo(9));
            Assert.That(tile.Cell00, Is.EqualTo(0x80));
            Assert.That(tile.Cell10, Is.EqualTo(0x41));
            Assert.That(tile.Cell01, Is.EqualTo(0x02));
            Assert.That(tile.Cell11, Is.EqualTo(0xC3));
        }

        [Test]
        public void Both_original_tile_record_versions_convert_to_the_same_values()
        {
            var current = LegacyMaps.Terrain(LegacyMaps.NewFormat, 64, 64);
            current.Sections[0] = PaintedSection(oldFormat: false);

            var earlier = LegacyMaps.Terrain(LegacyMaps.OldFormat, 64, 64);
            earlier.Sections[0] = PaintedSection(oldFormat: true);

            _client.AddMap("current", current);
            _client.AddMap("earlier", earlier);

            Converter(terrain: Textures).ConvertAll().ToList();

            var fromCurrent = TileAt(Read("current"), 0, 0);
            var fromEarlier = TileAt(Read("earlier"), 0, 0);

            Assert.That(fromEarlier.Height, Is.EqualTo(2.5f), "centimeters become meters");
            Assert.That(fromEarlier.ColorR, Is.EqualTo(160));
            Assert.That(fromEarlier.ColorG, Is.EqualTo(68));
            Assert.That(fromEarlier.ColorB, Is.EqualTo(16));
            Assert.That(fromEarlier.Layer0.MaskIndex, Is.EqualTo(15),
                "the earlier record's own base mask goes the way the original drops it");
            Assert.That(fromEarlier, Is.EqualTo(fromCurrent));
        }

        [Test]
        public void Both_placement_kinds_land_in_one_record_shape_with_one_yaw_convention()
        {
            var terrain = LegacyMaps.FilledTerrain(LegacyMaps.NewFormat, 128, 64);

            var objects = LegacyMaps.Objects(128, 64);
            objects.Sections[0] = new Original.ObjSection { Objects = [LegacyMaps.Model(501, 250, 700, 20, 90)] };
            objects.Sections[8] = new Original.ObjSection { Objects = [LegacyMaps.Effect(12, 600, 500, -150, 100)] };

            _client.AddMap("shore", terrain, objects);

            Converter().ConvertAll().ToList();

            var map = Read("shore");
            var model = map.Chunks[0, 0].Placements.Single();
            var effect = map.Chunks[1, 0].Placements.Single();

            Assert.That(model.Kind, Is.EqualTo(Converted.PlacementKind.Model));
            Assert.That(model.Id, Is.EqualTo(501), "catalog ids convert untouched");
            Assert.That(model.X, Is.EqualTo(2.5f), "centimeters become meters");
            Assert.That(model.Y, Is.EqualTo(7f));
            Assert.That(model.HeightOffset, Is.EqualTo(0.2f));
            Assert.That(model.Yaw, Is.EqualTo(90f), "model yaw was already whole degrees");

            Assert.That(effect.Kind, Is.EqualTo(Converted.PlacementKind.Effect));
            Assert.That(effect.Id, Is.EqualTo(12), "the kind bits never reach the catalog id");
            Assert.That(effect.X, Is.EqualTo(70f));
            Assert.That(effect.Y, Is.EqualTo(5f));
            Assert.That(effect.HeightOffset, Is.EqualTo(-1.5f));
            Assert.That(effect.Yaw, Is.EqualTo(57.29578f).Within(0.001f),
                "effect yaw was hundredths of a radian");
        }

        [Test]
        public void A_chunk_gathers_the_placements_of_every_section_it_covers()
        {
            var terrain = LegacyMaps.FilledTerrain(LegacyMaps.NewFormat, 64, 64);

            var objects = LegacyMaps.Objects(64, 64);
            objects.Sections[0] = new Original.ObjSection { Objects = [LegacyMaps.Model(1, 100, 100, 0, 0)] };
            objects.Sections[1] = new Original.ObjSection { Objects = [LegacyMaps.Model(2, 100, 100, 0, 0)] };
            objects.Sections[9] = new Original.ObjSection { Objects = [LegacyMaps.Model(3, 100, 100, 0, 0)] };

            _client.AddMap("town", terrain, objects);

            Converter().ConvertAll().ToList();

            var placements = Read("town").Chunks[0, 0].Placements;

            Assert.That(placements.Select(placement => placement.Id), Is.EqualTo(new[] { 1, 2, 3 }),
                "one chunk covers 64 original sections, and its list has no slot limit");
        }

        [Test]
        public void A_placement_position_counts_from_the_map_corner_not_its_section_corner()
        {
            var terrain = LegacyMaps.FilledTerrain(LegacyMaps.NewFormat, 128, 128);

            var objects = LegacyMaps.Objects(128, 128);
            var section = (9 * objects.SectionCountX) + 9;

            objects.Sections[section] = new Original.ObjSection
            {
                Objects = [LegacyMaps.Model(77, 250, 250, 0, 0)],
            };

            _client.AddMap("drift", terrain, objects);

            Converter().ConvertAll().ToList();

            var map = Read("drift");
            var placement = map.Chunks[1, 1].Placements.Single();

            Assert.That(map.Chunks[0, 0].Placements, Is.Empty);
            Assert.That(placement.Id, Is.EqualTo(77));
            Assert.That(placement.X, Is.EqualTo(74.5f));
            Assert.That(placement.Y, Is.EqualTo(74.5f));
        }

        [Test]
        public void A_placement_where_no_section_wrote_terrain_is_dropped_and_reported()
        {
            var terrain = LegacyMaps.Terrain(LegacyMaps.NewFormat, 128, 64);
            terrain.Sections[0] = LegacyMaps.Section();

            var objects = LegacyMaps.Objects(128, 64);
            objects.Sections[9] = new Original.ObjSection { Objects = [LegacyMaps.Model(77, 100, 100, 0, 0)] };

            _client.AddMap("edge", terrain, objects);

            Converter().ConvertAll().ToList();

            var map = Read("edge");

            Assert.That(map.Chunks[0, 0].Placements, Is.Empty);
            Assert.That(map.Chunks[1, 0], Is.Null);
            Assert.That(_log.Warnings, Has.Some.Contains("dropped 1"));
        }

        [Test]
        public void A_placement_the_object_file_puts_past_the_map_edge_is_dropped_and_reported()
        {
            var terrain = LegacyMaps.Terrain(LegacyMaps.NewFormat, 64, 64);
            terrain.Sections[0] = LegacyMaps.Section();

            var objects = LegacyMaps.Objects(128, 128);
            objects.Sections[153] = new Original.ObjSection { Objects = [LegacyMaps.Model(77, 100, 100, 0, 0)] };

            _client.AddMap("wide", terrain, objects);

            Converter().ConvertAll().ToList();

            Assert.That(Read("wide").Chunks[0, 0].Placements, Is.Empty);
            Assert.That(_log.Warnings, Has.Some.Contains("past the map"));
        }

        [Test]
        public void A_tile_keeps_the_terrain_id_the_original_painted_it_with()
        {
            var terrain = LegacyMaps.Terrain(LegacyMaps.NewFormat, 64, 64);
            terrain.Sections[0] = LegacyMaps.Section(index =>
            {
                var tile = LegacyMaps.Tile();

                tile.Texture0 = (byte)(index == 0 ? 3 : 0);
                tile.Texture1 = (byte)(index == 1 ? 1 : 0);
                tile.Alpha1 = (byte)(index == 1 ? 6 : 0);

                return tile;
            });

            _client.AddMap("palette", terrain);

            Converter(terrain: Textures).ConvertAll().ToList();

            var map = Read("palette");

            Assert.That(TileAt(map, 0, 0).Layer0.TerrainId, Is.EqualTo(3), "rock02 is terrain three");
            Assert.That(TileAt(map, 1, 0).Layer1.TerrainId, Is.EqualTo(1), "grass05 is terrain one");
        }

        private void AddTerrainTextures()
        {
            _client.AddTexture("terrain", "bmp/1.BMP", "Grass05.bmp");
            _client.AddTexture("terrain", "tga/03040032.tga", "Sand01.bmp");
            _client.AddTexture("terrain", "bmp/1.BMP", "Rock02.bmp");
            _client.AddTexture("terrain", "bmp/1.BMP", "Brick06.bmp");
            _client.AddTexture("terrain/alpha", "tga/9011001903.tga", "total.tga");
        }

        private static Original.MapFile TwoTextureTerrain()
        {
            var terrain = LegacyMaps.Terrain(LegacyMaps.NewFormat, 64, 64);

            terrain.Sections[0] = LegacyMaps.Section(index =>
            {
                var tile = LegacyMaps.Tile();

                tile.Texture0 = 1;
                tile.Texture1 = (byte)(index == 0 ? 2 : 0);
                tile.Alpha1 = (byte)(index == 0 ? 6 : 0);

                return tile;
            });

            return terrain;
        }

        [Test]
        public void Every_terrain_texture_the_table_names_lands_in_the_tree_with_the_mask_atlas()
        {
            AddTerrainTextures();
            _client.AddWaterLoop();
            _client.AddMap("painted", TwoTextureTerrain(), LegacyMaps.Objects(64, 64));

            Converter(terrain: Textures).ConvertAll().ToList();

            Assert.That(File.Exists(_client.ConvertedTexture("terrain", "grass05.png")));
            Assert.That(File.Exists(_client.ConvertedTexture("terrain", "sand01.png")));
            Assert.That(File.Exists(_client.ConvertedTexture("terrain", "brick06.png")));
            Assert.That(File.Exists(_client.ConvertedTexture("terrain", "rock02.png")),
                "the whole catalogue is written, not only what this map paints with");
            Assert.That(File.Exists(_client.ConvertedTexture("terrain/alpha", "total.png")));
            Assert.That(_log.Warnings, Is.Empty);
        }

        [Test]
        public void The_thirty_water_loop_textures_land_in_the_tree()
        {
            var terrain = LegacyMaps.Terrain(LegacyMaps.NewFormat, 64, 64);
            terrain.Sections[0] = LegacyMaps.Section();

            AddTerrainTextures();
            _client.AddWaterLoop();
            _client.AddMap("bare", terrain, LegacyMaps.Objects(64, 64));

            Converter(terrain: Textures).ConvertAll().ToList();

            var written = Directory.GetFiles(Path.GetDirectoryName(
                _client.ConvertedTexture("terrain/water", "ocean_h.01.png")));

            Assert.That(written, Has.Length.EqualTo(30));
            Assert.That(File.Exists(_client.ConvertedTexture("terrain/water", "ocean_h.01.png")));
            Assert.That(File.Exists(_client.ConvertedTexture("terrain/water", "ocean_h.30.png")));
            Assert.That(_log.Warnings, Is.Empty);
        }

        [Test]
        public void A_terrain_texture_missing_from_the_client_is_reported_and_the_map_still_converts()
        {
            _client.AddTexture("terrain", "bmp/1.BMP", "Grass05.bmp");
            _client.AddMap("painted", TwoTextureTerrain(), LegacyMaps.Objects(64, 64));

            var result = Converter(terrain: Textures).ConvertAll().Single();

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(File.Exists(_client.ConvertedTexture("terrain", "grass05.png")));
            Assert.That(File.Exists(_client.ConvertedTexture("terrain", "sand01.png")), Is.False);
            Assert.That(_log.Warnings, Has.Some.Contains("Sand01.bmp"));
            Assert.That(_log.Warnings, Has.Some.Contains("total.tga"));
            Assert.That(_log.Warnings, Has.Some.Contains("ocean_h.01.bmp"));
        }

        [Test]
        public void A_texture_already_in_the_tree_is_left_alone()
        {
            AddTerrainTextures();
            _client.AddWaterLoop();
            _client.AddMap("painted", TwoTextureTerrain(), LegacyMaps.Objects(64, 64));

            var stale = _client.ConvertedTexture("terrain", "sand01.png");

            Directory.CreateDirectory(Path.GetDirectoryName(stale));
            File.WriteAllBytes(stale, new byte[] { 1 });

            Converter(terrain: Textures).ConvertAll().ToList();

            Assert.That(File.ReadAllBytes(stale), Is.EqualTo(new byte[] { 1 }));
            Assert.That(_log.Warnings, Is.Empty);
        }

        [Test]
        public void A_texture_the_terrain_table_does_not_name_is_reported_and_left_unpainted()
        {
            var terrain = LegacyMaps.Terrain(LegacyMaps.NewFormat, 64, 64);
            terrain.Sections[0] = LegacyMaps.Section(index =>
            {
                var tile = LegacyMaps.Tile();

                tile.Texture0 = (byte)(index == 0 ? 62 : 0);

                return tile;
            });

            _client.AddMap("stray", terrain);

            Converter(terrain: Textures).ConvertAll().ToList();

            Assert.That(TileAt(Read("stray"), 0, 0).Layer0.TerrainId, Is.EqualTo(0));
            Assert.That(_log.Warnings, Has.Some.Contains("62"));
        }

        [Test]
        public void A_chunk_no_section_wrote_is_absent()
        {
            var terrain = LegacyMaps.Terrain(LegacyMaps.NewFormat, 128, 128);
            terrain.Sections[0] = LegacyMaps.Section();

            _client.AddMap("sparse", terrain);

            Converter().ConvertAll().ToList();

            var map = Read("sparse");

            Assert.That(map.Chunks[0, 0], Is.Not.Null);
            Assert.That(map.Chunks[1, 0], Is.Null);
            Assert.That(map.Chunks[0, 1], Is.Null);
            Assert.That(map.Chunks[1, 1], Is.Null);
        }

        [Test]
        public void A_tile_no_section_wrote_reads_as_underwater()
        {
            var terrain = LegacyMaps.Terrain(LegacyMaps.NewFormat, 64, 64);
            terrain.Sections[0] = PaintedSection(oldFormat: false);

            _client.AddMap("gap", terrain);

            Converter(terrain: Textures).ConvertAll().ToList();

            var tile = TileAt(Read("gap"), 63, 63);

            Assert.That(tile.Height, Is.EqualTo(-2f),
                "a height of zero would leave the neighboring quads coplanar with the water");
            Assert.That(tile.Layer0.TerrainId, Is.EqualTo(Converted.MapTile.UnderwaterTerrain));
            Assert.That(tile.Layer0.MaskIndex, Is.EqualTo(15), "the underwater tile covers fully");
            Assert.That(tile.ColorR, Is.EqualTo(255), "the engine's default tile is white");
            Assert.That(tile.ColorG, Is.EqualTo(255));
            Assert.That(tile.ColorB, Is.EqualTo(255));
        }

        [Test]
        public void A_map_without_an_object_file_converts_with_no_placements()
        {
            var terrain = LegacyMaps.Terrain(LegacyMaps.NewFormat, 64, 64);
            terrain.Sections[0] = LegacyMaps.Section();

            _client.AddMap("bare", terrain);

            var result = Converter().ConvertAll().Single();

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(Read("bare").Chunks[0, 0].Placements, Is.Empty);
            Assert.That(_log.Warnings, Has.Some.Contains("bare.obj"));
        }

        [Test]
        public void A_map_the_readers_reject_is_logged_and_counted_while_the_batch_carries_on()
        {
            var terrain = LegacyMaps.Terrain(LegacyMaps.NewFormat, 64, 64);
            terrain.Sections[0] = LegacyMaps.Section();

            _client.AddUnreadableMap("broken");
            _client.AddMap("sound", terrain);

            var results = Converter().ConvertAll().ToList();

            Assert.That(results.Select(result => result.Name), Is.EqualTo(new[] { "broken", "sound" }));
            Assert.That(results[0].Outcome, Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(results[1].Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(File.Exists(_client.ConvertedMap("broken")), Is.False);
            Assert.That(File.Exists(_client.ConvertedMap("sound")), Is.True);
            Assert.That(_log.Errors, Has.Some.Contains("broken"));
        }

        [Test]
        public void A_map_already_in_the_tree_is_skipped_without_overwrite()
        {
            var terrain = LegacyMaps.Terrain(LegacyMaps.NewFormat, 64, 64);
            terrain.Sections[0] = LegacyMaps.Section();

            _client.AddMap("once", terrain);

            Converter().ConvertAll().ToList();

            var result = Converter(overwrite: false).ConvertAll().Single();

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Skipped));
        }

        [Test]
        public void A_batch_reports_the_unit_it_is_about_to_convert()
        {
            var terrain = LegacyMaps.Terrain(LegacyMaps.NewFormat, 64, 64);
            terrain.Sections[0] = LegacyMaps.Section();

            _client.AddMap("first", terrain);
            _client.AddMap("second", terrain);

            var progress = new RecordedProgress();

            Converter().ConvertAll(progress).ToList();

            Assert.That(progress.Steps.Select(step => step.Unit),
                Is.EqualTo(new[] { "map first", "map second" }));
        }
    }
}
