using NUnit.Framework;
using Top.Client.Assets.Maps;
using Top.Contracts.Assets.Maps;

namespace Top.Client.Presentation.Tests
{
    public class ScenerySpawnerTests
    {
        private static MapData Ground(float height)
        {
            var map = new MapFile(8, 8, 2);
            var chunk = new MapChunk(map.ChunkSize);

            map.Chunks[0, 0] = chunk;

            for (var tile = 0; tile < chunk.Tiles.Length; tile++)
            {
                chunk.Tiles[tile] = new MapTile { Height = height };
            }

            return new MapData(map);
        }

        [Test]
        public void A_placement_over_sunken_ground_seats_at_the_water_line()
        {
            var sunk = new MapPlacement { X = 0.5f, Y = 0.5f, HeightOffset = -1.5f };

            Assert.That(ScenerySpawner.SeatHeight(Ground(-2f), sunk), Is.EqualTo(-1.5f),
                "the original clamps ground to the water line before the offset");
        }

        [Test]
        public void A_placement_on_dry_ground_seats_at_terrain_height_plus_its_offset()
        {
            var standing = new MapPlacement { X = 0.5f, Y = 0.5f, HeightOffset = 0.25f };

            Assert.That(ScenerySpawner.SeatHeight(Ground(3f), standing), Is.EqualTo(3.25f));
        }
    }
}
