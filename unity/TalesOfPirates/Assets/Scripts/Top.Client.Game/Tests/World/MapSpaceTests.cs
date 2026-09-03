using NUnit.Framework;
using Top.Client.Game.World;
using UnityEngine;

namespace Top.Client.Game.Tests.World
{
    public class MapSpaceTests
    {
        [Test]
        public void Map_ground_maps_to_unity_with_x_flipped()
        {
            Assert.That(MapSpace.ToWorld(3f, 5f, 2f), Is.EqualTo(new Vector3(-3f, 2f, 5f)));
        }

        [Test]
        public void World_position_maps_back_to_map_ground()
        {
            Assert.That(MapSpace.ToMap(new Vector3(-3f, 2f, 5f)), Is.EqualTo(new Vector2(3f, 5f)));
        }
    }
}
