using NUnit.Framework;
using Top.Client.Game.World;
using Top.Contracts.Assets.Maps;
using UnityEngine;

namespace Top.Client.Game.Tests.World
{
    public class MapDataTests
    {
        private static MapData Load(MapFile map)
        {
            return new MapData(map);
        }

        [Test]
        public void Dimensions_come_back_as_written()
        {
            var map = new MapFile(8, 6, 2);

            var data = Load(map);

            Assert.That(data.Width, Is.EqualTo(8));
            Assert.That(data.Height, Is.EqualTo(6));
            Assert.That(data.ChunkSize, Is.EqualTo(2));
            Assert.That(data.ChunkCountX, Is.EqualTo(4));
            Assert.That(data.ChunkCountY, Is.EqualTo(3));
        }

        private static MapFile CreateMap(int width, int height, int chunkSize)
        {
            return new MapFile(width, height, chunkSize);
        }

        private static MapChunk AddChunk(MapFile map, int chunkX, int chunkY)
        {
            var chunk = new MapChunk(map.ChunkSize);

            map.Chunks[chunkX, chunkY] = chunk;

            return chunk;
        }

        private static void SetTile(MapFile map, int x, int y, MapTile tile)
        {
            var chunk = map.Chunks[x / map.ChunkSize, y / map.ChunkSize];

            chunk.Tiles[((y % map.ChunkSize) * map.ChunkSize) + (x % map.ChunkSize)] = tile;
        }

        private static void SetHeight(MapFile map, int x, int y, float height)
        {
            SetTile(map, x, y, new MapTile { Height = height });
        }

        private static MapFile CreateQuadMap()
        {
            var map = CreateMap(2, 2, 2);

            AddChunk(map, 0, 0);
            SetHeight(map, 0, 0, 1f);
            SetHeight(map, 1, 0, 3f);
            SetHeight(map, 0, 1, 5f);
            SetHeight(map, 1, 1, 10f);

            return map;
        }

        [Test]
        public void Height_at_a_vertex_is_that_vertex_height()
        {
            var data = Load(CreateQuadMap());

            Assert.That(data.HeightAt(0f, 0f), Is.EqualTo(1f));
            Assert.That(data.HeightAt(1f, 0f), Is.EqualTo(3f));
            Assert.That(data.HeightAt(0f, 1f), Is.EqualTo(5f));
            Assert.That(data.HeightAt(1f, 1f), Is.EqualTo(10f));
        }

        [Test]
        public void Height_between_vertices_interpolates_over_the_quad_triangles()
        {
            var data = Load(CreateQuadMap());

            Assert.That(data.HeightAt(0.25f, 0.25f), Is.EqualTo(2.5f).Within(0.0001f),
                "below the diagonal: 1 + 0.25*(3-1) + 0.25*(5-1)");
            Assert.That(data.HeightAt(0.75f, 0.75f), Is.EqualTo(7f).Within(0.0001f),
                "above the diagonal: 10 + 0.25*(5-10) + 0.25*(3-10)");
            Assert.That(data.HeightAt(0.5f, 0.5f), Is.EqualTo(4f).Within(0.0001f),
                "on the diagonal both triangles meet at (3+5)/2");
        }

        [Test]
        public void Height_interpolates_across_a_chunk_boundary()
        {
            var map = CreateMap(4, 4, 2);

            AddChunk(map, 0, 0);
            AddChunk(map, 1, 0);
            SetHeight(map, 1, 0, 2f);
            SetHeight(map, 2, 0, 6f);

            var data = Load(map);

            Assert.That(data.HeightAt(1.5f, 0f), Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void Height_off_the_written_ground_is_open_water()
        {
            var map = CreateMap(4, 4, 2);

            AddChunk(map, 0, 0);
            SetHeight(map, 0, 0, 7f);

            var data = Load(map);

            Assert.That(data.HeightAt(3.2f, 3.2f), Is.EqualTo(-2f), "an absent chunk is open water");
            Assert.That(data.HeightAt(-2f, 1f), Is.EqualTo(-2f), "past the west edge");
            Assert.That(data.HeightAt(1f, 9f), Is.EqualTo(-2f), "past the north edge");
        }

        [Test]
        public void Blocked_bits_read_back_per_cell()
        {
            var map = CreateMap(2, 2, 2);

            AddChunk(map, 0, 0);
            SetTile(map, 1, 0, new MapTile { Cell10 = 0x80, Cell01 = 0x80 });

            var data = Load(map);

            Assert.That(data.IsBlocked(3, 0), Is.True, "the tile's east-south cell");
            Assert.That(data.IsBlocked(2, 1), Is.True, "the tile's west-north cell");
            Assert.That(data.IsBlocked(2, 0), Is.False);
            Assert.That(data.IsBlocked(3, 1), Is.False);
            Assert.That(data.IsBlocked(0, 0), Is.False, "the neighbor tile stays open");
        }

        [Test]
        public void Off_the_written_ground_is_blocked()
        {
            var map = CreateMap(4, 4, 2);

            AddChunk(map, 0, 0);

            var data = Load(map);

            Assert.That(data.IsBlocked(6, 6), Is.True, "an absent chunk");
            Assert.That(data.IsBlocked(-1, 0), Is.True, "past the west edge");
            Assert.That(data.IsBlocked(0, 8), Is.True, "past the north edge");
        }

        [Test]
        public void Cell_height_decodes_sign_and_five_centimeter_steps()
        {
            var map = CreateMap(2, 2, 2);

            AddChunk(map, 0, 0);
            SetTile(map, 0, 0, new MapTile { Cell00 = 10, Cell10 = 0x40 | 10, Cell01 = 0x80 | 10 });

            var data = Load(map);

            Assert.That(data.CellHeightAt(0, 0), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(data.CellHeightAt(1, 0), Is.EqualTo(-0.5f).Within(0.0001f), "bit six is the sign");
            Assert.That(data.CellHeightAt(0, 1), Is.EqualTo(0.5f).Within(0.0001f),
                "the blocked bit leaves the height alone");
            Assert.That(data.CellHeightAt(6, 6), Is.EqualTo(0f), "off the map there is no relief");
        }

        [Test]
        public void Vertex_color_reads_back_per_tile_and_open_water_is_white()
        {
            var map = CreateMap(4, 4, 2);

            AddChunk(map, 0, 0);
            SetTile(map, 1, 0, new MapTile { ColorR = 10, ColorG = 20, ColorB = 30 });

            var data = Load(map);

            Assert.That(data.ColorAt(1, 0), Is.EqualTo(new Color32(10, 20, 30, 255)));
            Assert.That(data.ColorAt(3, 3), Is.EqualTo(new Color32(255, 255, 255, 255)),
                "an absent chunk carries no authored shading");
            Assert.That(data.ColorAt(-1, 5), Is.EqualTo(new Color32(255, 255, 255, 255)),
                "past the edge carries no authored shading");
        }

        [Test]
        public void Regions_and_islands_read_back_per_tile()
        {
            var map = CreateMap(4, 4, 2);

            AddChunk(map, 0, 0);
            SetTile(map, 1, 1, new MapTile { Region = 0b101, Island = 7 });

            var data = Load(map);

            Assert.That(data.RegionAt(1, 1), Is.EqualTo(0b101));
            Assert.That(data.IslandAt(1, 1), Is.EqualTo(7));
            Assert.That(data.RegionAt(0, 0), Is.EqualTo(0));
            Assert.That(data.IslandAt(0, 0), Is.EqualTo(0));
            Assert.That(data.RegionAt(3, 3), Is.EqualTo(0), "an absent chunk is open water, no region");
            Assert.That(data.IslandAt(3, 3), Is.EqualTo(0), "an absent chunk belongs to no island");
            Assert.That(data.RegionAt(-1, 9), Is.EqualTo(0));
            Assert.That(data.IslandAt(-1, 9), Is.EqualTo(0));
        }

        [Test]
        public void Placements_come_back_with_their_chunk()
        {
            var map = CreateMap(4, 4, 2);

            var chunk = AddChunk(map, 0, 0);

            chunk.Placements.Add(new MapPlacement
            {
                Kind = PlacementKind.Model,
                Id = 42,
                X = 1.5f,
                Y = 0.25f,
                HeightOffset = 0.6f,
                Yaw = 90f,
            });
            chunk.Placements.Add(new MapPlacement { Kind = PlacementKind.Effect, Id = 7 });
            AddChunk(map, 1, 0);

            var data = Load(map);

            var placements = data.PlacementsAt(0, 0);

            Assert.That(placements, Has.Count.EqualTo(2));
            Assert.That(placements[0].Kind, Is.EqualTo(PlacementKind.Model));
            Assert.That(placements[0].Id, Is.EqualTo(42));
            Assert.That(placements[0].X, Is.EqualTo(1.5f));
            Assert.That(placements[0].Y, Is.EqualTo(0.25f));
            Assert.That(placements[0].HeightOffset, Is.EqualTo(0.6f));
            Assert.That(placements[0].Yaw, Is.EqualTo(90f));
            Assert.That(placements[1].Kind, Is.EqualTo(PlacementKind.Effect));
            Assert.That(data.PlacementsAt(1, 0), Is.Empty, "a written chunk can place nothing");
            Assert.That(data.PlacementsAt(1, 1), Is.Empty, "an absent chunk places nothing");
        }

        [Test]
        public void The_tile_layers_read_back_as_written()
        {
            var map = CreateMap(4, 4, 2);

            AddChunk(map, 0, 0);
            SetTile(map, 1, 0, new MapTile
            {
                Layer0 = new MapTileLayer { TerrainId = 5, MaskIndex = 15 },
                Layer1 = new MapTileLayer { TerrainId = 22, MaskIndex = 6 },
            });

            var data = Load(map);

            Assert.That(data.LayerAt(1, 0, 0), Is.EqualTo(new MapTileLayer { TerrainId = 5, MaskIndex = 15 }));
            Assert.That(data.LayerAt(1, 0, 1), Is.EqualTo(new MapTileLayer { TerrainId = 22, MaskIndex = 6 }));
            Assert.That(data.LayerAt(1, 0, 2).TerrainId, Is.EqualTo(0), "an unpainted layer names no terrain");
        }

        [Test]
        public void Ground_off_the_written_chunks_reads_as_underwater()
        {
            var map = CreateMap(4, 4, 2);

            AddChunk(map, 0, 0);

            var data = Load(map);

            Assert.That(data.LayerAt(3, 3, 0).TerrainId, Is.EqualTo(MapTile.UnderwaterTerrain),
                "an absent chunk");
            Assert.That(data.LayerAt(-1, 0, 0).TerrainId, Is.EqualTo(MapTile.UnderwaterTerrain),
                "past the edge");
            Assert.That(data.LayerAt(3, 3, 0).MaskIndex, Is.EqualTo(15), "the underwater tile covers fully");
            Assert.That(data.LayerAt(3, 3, 1).TerrainId, Is.EqualTo(0), "it carries no detail layers");
        }

        private static MapData LoadFlatGround(float height)
        {
            var map = CreateMap(2, 2, 2);

            AddChunk(map, 0, 0);
            SetHeight(map, 0, 0, height);
            SetHeight(map, 1, 0, height);
            SetHeight(map, 0, 1, height);
            SetHeight(map, 1, 1, height);

            return Load(map);
        }

        [Test]
        public void A_placement_over_sunken_ground_rests_at_the_water_line()
        {
            var sunk = new MapPlacement { X = 0.5f, Y = 0.5f, HeightOffset = -1.5f };

            Assert.That(LoadFlatGround(-2f).GetPlacementHeight(sunk), Is.EqualTo(-1.5f),
                "the original clamps ground to the water line before the offset");
        }

        [Test]
        public void A_placement_on_dry_ground_rests_at_terrain_height_plus_its_offset()
        {
            var standing = new MapPlacement { X = 0.5f, Y = 0.5f, HeightOffset = 0.25f };

            Assert.That(LoadFlatGround(3f).GetPlacementHeight(standing), Is.EqualTo(3.25f));
        }

        [Test]
        public void A_chunk_absent_from_the_file_reads_as_absent()
        {
            var map = CreateMap(4, 4, 2);

            AddChunk(map, 0, 0);

            var data = Load(map);

            Assert.That(data.HasChunk(0, 0), Is.True);
            Assert.That(data.HasChunk(1, 1), Is.False);
            Assert.That(data.HasChunk(-1, 0), Is.False);
            Assert.That(data.HasChunk(0, 2), Is.False);
        }
    }
}
