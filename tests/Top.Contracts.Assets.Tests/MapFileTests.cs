using System;
using System.IO;
using NUnit.Framework;
using Top.Contracts.Assets.Maps;

namespace Top.Contracts.Assets.Tests
{
    public class MapFileTests
    {
        private static MapFile RoundTrip(MapFile map)
        {
            using var stream = new MemoryStream();
            map.Write(stream);
            stream.Position = 0;
            return MapFile.Read(stream);
        }

        private static MapChunk FullyPopulatedChunk(int chunkSize)
        {
            var chunk = new MapChunk(chunkSize);

            for (var i = 0; i < chunk.Tiles.Length; i++)
            {
                chunk.Tiles[i] = new MapTile
                {
                    Height = i * 0.1f - 12.8f,
                    ColorR = (byte)i,
                    ColorG = (byte)(i + 1),
                    ColorB = (byte)(i + 2),
                    Layer0 = new MapTileLayer { TerrainId = 0, MaskIndex = 15 },
                    Layer1 = new MapTileLayer { TerrainId = 1, MaskIndex = (byte)(i % 16) },
                    Layer2 = new MapTileLayer { TerrainId = (byte)(i % 3), MaskIndex = 7 },
                    Layer3 = new MapTileLayer { TerrainId = 2, MaskIndex = 3 },
                    Region = (ushort)(1 << i % 15),
                    Island = (byte)(i % 201),
                    Cell00 = (byte)(0x80 | i % 64),
                    Cell10 = (byte)(0x40 | i % 64),
                    Cell01 = (byte)(i % 64),
                    Cell11 = (byte)(0xC0 | i % 64),
                };
            }

            chunk.Placements.Add(new MapPlacement
            {
                Kind = PlacementKind.Model,
                Id = 501,
                X = 100.5f,
                Y = 7f,
                HeightOffset = 0.2f,
                Yaw = 180f,
            });
            chunk.Placements.Add(new MapPlacement
            {
                Kind = PlacementKind.Effect,
                Id = 12,
                X = 64f,
                Y = 65f,
                HeightOffset = -1.5f,
                Yaw = 22.5f,
            });

            return chunk;
        }

        [Test]
        public void A_map_with_every_layer_populated_round_trips()
        {
            var map = new MapFile(128, 128, 64);
            map.Chunks[1, 0] = FullyPopulatedChunk(64);
            map.Chunks[0, 1] = FullyPopulatedChunk(64);

            var read = RoundTrip(map);

            Assert.That(read.Chunks[1, 0].Tiles, Is.EqualTo(map.Chunks[1, 0].Tiles));
            Assert.That(read.Chunks[0, 1].Tiles, Is.EqualTo(map.Chunks[0, 1].Tiles));
            Assert.That(read.Chunks[1, 0].Placements, Is.EqualTo(map.Chunks[1, 0].Placements));
            Assert.That(read.Chunks[0, 1].Placements, Is.EqualTo(map.Chunks[0, 1].Placements));
        }

        [Test]
        public void A_chunk_absent_from_the_offset_table_reads_back_as_absent()
        {
            var map = new MapFile(128, 128, 64);
            map.Chunks[1, 0] = FullyPopulatedChunk(64);

            var read = RoundTrip(map);

            Assert.That(read.Chunks[1, 0], Is.Not.Null);
            Assert.That(read.Chunks[0, 0], Is.Null);
            Assert.That(read.Chunks[0, 1], Is.Null);
            Assert.That(read.Chunks[1, 1], Is.Null);
        }

        [Test]
        public void A_placement_carries_kind_catalog_id_ground_position_height_offset_and_yaw()
        {
            var map = new MapFile(64, 64, 64);
            var chunk = new MapChunk(64);
            chunk.Placements.Add(new MapPlacement
            {
                Kind = PlacementKind.Model,
                Id = 1234,
                X = 12.5f,
                Y = 33.25f,
                HeightOffset = -0.75f,
                Yaw = 270f,
            });
            chunk.Placements.Add(new MapPlacement
            {
                Kind = PlacementKind.Effect,
                Id = 87,
                X = 1f,
                Y = 2f,
                HeightOffset = 0.1f,
                Yaw = 45.5f,
            });
            map.Chunks[0, 0] = chunk;

            var read = RoundTrip(map);

            Assert.That(read.Chunks[0, 0].Placements, Is.EqualTo(chunk.Placements));
        }

        [Test]
        public void Placement_lists_are_variable_length_beyond_the_original_slot_limit()
        {
            var map = new MapFile(128, 64, 64);
            map.Chunks[0, 0] = new MapChunk(64);
            var crowded = new MapChunk(64);
            for (var i = 0; i < 40; i++)
            {
                crowded.Placements.Add(new MapPlacement { Kind = PlacementKind.Model, Id = i, X = i, Y = i });
            }

            map.Chunks[1, 0] = crowded;

            var read = RoundTrip(map);

            Assert.That(read.Chunks[0, 0].Placements, Is.Empty);
            Assert.That(read.Chunks[1, 0].Placements, Is.EqualTo(crowded.Placements));
        }

        [Test]
        public void The_header_round_trips()
        {
            var map = new MapFile(896, 896, 64);

            var read = RoundTrip(map);

            Assert.That(read.Width, Is.EqualTo(896));
            Assert.That(read.Height, Is.EqualTo(896));
            Assert.That(read.ChunkSize, Is.EqualTo(64));
            Assert.That(read.ChunkCountX, Is.EqualTo(14));
            Assert.That(read.ChunkCountY, Is.EqualTo(14));
        }

        private static long WrittenLength(Func<MapChunk> chunk)
        {
            var map = new MapFile(128, 128, 64);

            for (var y = 0; y < map.ChunkCountY; y++)
            {
                for (var x = 0; x < map.ChunkCountX; x++)
                {
                    map.Chunks[x, y] = chunk();
                }
            }

            using var stream = new MemoryStream();
            map.Write(stream);

            return stream.Length;
        }

        [Test]
        public void Ground_that_repeats_costs_almost_nothing()
        {
            var distinct = WrittenLength(() => FullyPopulatedChunk(64));
            var alike = WrittenLength(() => new MapChunk(64));

            Assert.That(alike * 20, Is.LessThan(distinct),
                "the body is deflated, so the ground a chunk pads itself with where nothing was " +
                "authored costs next to nothing");
        }

        [Test]
        public void A_file_of_an_unknown_version_is_rejected()
        {
            using var stream = new MemoryStream();
            new MapFile(64, 64, 64).Write(stream);
            stream.Position = 0;
            stream.WriteByte(99);
            stream.Position = 0;

            Assert.That(() => MapFile.Read(stream), Throws.TypeOf<InvalidDataException>());
        }
    }
}
