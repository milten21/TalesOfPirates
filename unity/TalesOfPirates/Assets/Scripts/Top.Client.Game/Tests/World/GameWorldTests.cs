using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Top.Client.Core;
using Top.Client.Game.World;
using Top.Client.Game.World.SceneObjects;
using Top.Client.Models;
using Top.Contracts.Assets.Maps;
using Top.Contracts.Tables.World;
using UnityEngine;

namespace Top.Client.Game.Tests.World
{
    public class GameWorldTests
    {
        private class FakeMapReader : IMapReader
        {
            private readonly MapData _mapData;

            public FakeMapReader(MapData mapData)
            {
                _mapData = mapData;
            }

            public Task<MapData> Read(string path, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_mapData);
            }
        }

        private class EmptySceneObjectFactory : ISceneObjectFactory
        {
            public Task<ModelInstance> Instantiate(int id, Transform parent,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<ModelInstance>(null);
            }
        }

        private Transform _root;

        [SetUp]
        public void CreateRoot()
        {
            _root = new GameObject("Root").transform;
        }

        [TearDown]
        public void DestroyRoot()
        {
            UnityObjects.Destroy(_root.gameObject);
        }

        [Test]
        public async Task The_map_of_the_asked_id_goes_under_the_root()
        {
            using var world = CreateWorld();

            await world.SetMap(1);

            Assert.That(world.Map.DisplayName, Is.EqualTo("First"));
            Assert.That(_root.childCount, Is.EqualTo(1));
        }

        [Test]
        public async Task A_new_map_replaces_the_map_that_is_open()
        {
            using var world = CreateWorld();

            await world.SetMap(1);
            await world.SetMap(2);

            Assert.That(world.Map.DisplayName, Is.EqualTo("Second"));
            Assert.That(_root.childCount, Is.EqualTo(1), "the map that was open went away");
        }

        [Test]
        public async Task Dispose_takes_the_map_out_of_the_scene()
        {
            var world = CreateWorld();

            await world.SetMap(1);

            world.Dispose();

            Assert.That(_root.childCount, Is.Zero);
            Assert.That(world.Map, Is.Null);
        }

        [Test]
        public void A_map_id_that_the_table_does_not_name_fails()
        {
            using var world = CreateWorld();

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => world.SetMap(3));
            Assert.That(_root.childCount, Is.Zero);
        }

        private GameWorld CreateWorld()
        {
            var mapTable = new MapTable(new[]
            {
                new MapEntry { Id = 1, MapPath = "maps/first.map", DisplayName = "First" },
                new MapEntry { Id = 2, MapPath = "maps/second.map", DisplayName = "Second" },
            });
            var mapFactory = new MapFactory(mapTable, new FakeMapReader(new MapData(new MapFile(8, 8, 2))),
                new EmptySceneObjectFactory(), terrainMaterial: null, waterMaterial: null, streamingRadius: 16f);

            return new GameWorld(mapFactory, _root, sun: null);
        }
    }
}
