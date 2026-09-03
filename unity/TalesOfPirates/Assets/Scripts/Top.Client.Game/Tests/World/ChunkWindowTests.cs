using System.Collections.Generic;
using NUnit.Framework;
using Top.Client.Game.World;
using Top.Contracts.Assets.Maps;
using UnityEngine;

namespace Top.Client.Game.Tests.World
{
    public class ChunkWindowTests
    {
        private static MapData Load(MapFile map)
        {
            return new MapData(map);
        }

        private static MapFile Map(params (int X, int Y)[] chunks)
        {
            var map = new MapFile(8, 8, 2);

            foreach ((int x, int y) in chunks)
            {
                map.Chunks[x, y] = new MapChunk(map.ChunkSize);
            }

            return map;
        }

        [Test]
        public void Chunks_within_the_radius_of_the_center_are_in_the_window()
        {
            var data = Load(Map((0, 0), (1, 0), (3, 3)));
            var window = new ChunkWindow(data, 0.5f);

            Assert.That(window.Chunks, Is.Empty, "the window is empty before a center is set");

            window.SetCenter(new Vector2(1f, 1f));

            Assert.That(window.Chunks, Is.EquivalentTo(new[] { new Vector2Int(0, 0) }));

            window.SetCenter(new Vector2(1.9f, 1f));

            Assert.That(window.Chunks,
                Is.EquivalentTo(new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }),
                "the neighbor chunk's edge came within the radius");
        }

        [Test]
        public void An_unpopulated_chunk_never_enters_the_window()
        {
            var data = Load(Map((0, 0), (1, 0)));
            var window = new ChunkWindow(data, 0.5f);

            window.SetCenter(new Vector2(3f, 3f));

            Assert.That(window.Chunks, Is.Empty, "the center stands on a chunk the map never wrote");
        }

        [Test]
        public void A_window_covering_open_water_counts_unpopulated_chunks()
        {
            var data = Load(Map((0, 0)));
            var window = new ChunkWindow(data, 0.5f, populatedOnly: false);

            window.SetCenter(new Vector2(3f, 3f));

            Assert.That(window.Chunks, Is.EquivalentTo(new[] { new Vector2Int(1, 1) }),
                "the chunk the map never wrote is open water, not a hole");
        }

        [Test]
        public void The_window_follows_the_center_as_it_moves()
        {
            var data = Load(Map((0, 0), (3, 0)));
            var window = new ChunkWindow(data, 0.5f);

            var added = new List<Vector2Int>();
            var removed = new List<Vector2Int>();

            window.Added += added.Add;
            window.Removed += removed.Add;

            window.SetCenter(new Vector2(1f, 1f));

            Assert.That(added, Is.EquivalentTo(new[] { new Vector2Int(0, 0) }));
            Assert.That(removed, Is.Empty);

            window.SetCenter(new Vector2(7f, 1f));

            Assert.That(window.Chunks, Is.EquivalentTo(new[] { new Vector2Int(3, 0) }));
            Assert.That(added, Is.EquivalentTo(new[] { new Vector2Int(0, 0), new Vector2Int(3, 0) }));
            Assert.That(removed, Is.EquivalentTo(new[] { new Vector2Int(0, 0) }),
                "the chunk left behind released");
        }

        [Test]
        public void A_center_move_within_the_same_chunk_changes_nothing()
        {
            var data = Load(Map((0, 0)));
            var window = new ChunkWindow(data, 0.5f);

            window.SetCenter(new Vector2(0.5f, 0.5f));

            var added = new List<Vector2Int>();
            var removed = new List<Vector2Int>();

            window.Added += added.Add;
            window.Removed += removed.Add;

            window.SetCenter(new Vector2(1f, 1f));

            Assert.That(added, Is.Empty);
            Assert.That(removed, Is.Empty);
            Assert.That(window.Chunks, Is.EquivalentTo(new[] { new Vector2Int(0, 0) }));
        }
    }
}
