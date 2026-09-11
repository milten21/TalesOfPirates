using NUnit.Framework;
using Top.Client.Game.World.Terrain;
using UnityEngine;

namespace Top.Client.Game.Tests.World.Terrain
{
    public class TerrainUvTests
    {
        private static void AssertUv(Vector2 actual, float u, float v, string message = null)
        {
            Assert.That(actual.x, Is.EqualTo(u).Within(0.0001f), message);
            Assert.That(actual.y, Is.EqualTo(v).Within(0.0001f), message);
        }

        [Test]
        public void One_texture_spans_four_by_four_tiles_with_v_counting_down_from_the_top()
        {
            AssertUv(TerrainUv.BaseUvAt(0, 0, 0, 0), 0f, 1f, "the map's south-west tile corner is the image's top-left");
            AssertUv(TerrainUv.BaseUvAt(0, 0, 1, 1), 0.25f, 0.75f);
            AssertUv(TerrainUv.BaseUvAt(3, 0, 1, 0), 1f, 1f, "the fourth tile reaches the far edge");
            AssertUv(TerrainUv.BaseUvAt(5, 6, 0, 0), 0.25f, 0.5f, "tiles repeat every four");
        }

        [Test]
        public void A_quad_covering_many_tiles_repeats_the_texture_once_per_four_of_them()
        {
            AssertUv(TerrainUv.BaseUvAtSpanCorner(64, 0, 0), 0f, 1f);
            AssertUv(TerrainUv.BaseUvAtSpanCorner(64, 1, 1), 16f, -15f, "sixty-four tiles are sixteen repeats");
            AssertUv(TerrainUv.BaseUvAtSpanCorner(4, 1, 0), 1f, 1f, "four tiles are one repeat");
        }

        [Test]
        public void A_mask_index_names_a_mask_of_the_four_by_four_atlas_read_left_to_right_top_down()
        {
            AssertUv(TerrainUv.MaskUvAt(1, 0, 0), 0.01f, 0.99f, "index one is the top-left mask, inset");
            AssertUv(TerrainUv.MaskUvAt(1, 1, 1), 0.24f, 0.76f);
            AssertUv(TerrainUv.MaskUvAt(6, 0, 0), 0.26f, 0.74f, "index six is the second mask of the second row");
            AssertUv(TerrainUv.MaskUvAt(15, 0, 0), 0.51f, 0.24f, "index fifteen is the third mask of the bottom row");
        }
    }
}
