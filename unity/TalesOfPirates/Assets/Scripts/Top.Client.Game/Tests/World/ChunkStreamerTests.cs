using System.Collections.Generic;
using NUnit.Framework;
using Top.Client.Game.World;
using Top.Contracts.Assets.Maps;
using UnityEngine;

namespace Top.Client.Game.Tests.World
{
    public class ChunkStreamerTests
    {
        private class RecordingLoader : IChunkLoader
        {
            public readonly List<Vector2Int> Loaded = new List<Vector2Int>();
            public readonly List<Vector2Int> Unloaded = new List<Vector2Int>();

            public void Load(Vector2Int chunk)
            {
                Loaded.Add(chunk);
            }

            public void Unload(Vector2Int chunk)
            {
                Unloaded.Add(chunk);
            }
        }

        private static MapData Load(MapFile map)
        {
            return new MapData(map);
        }

        private static MapFile CreateMap(params (int X, int Y)[] chunks)
        {
            var map = new MapFile(8, 8, 2);

            foreach ((int x, int y) in chunks)
            {
                map.Chunks[x, y] = new MapChunk(map.ChunkSize);
            }

            return map;
        }

        [Test]
        public void Chunks_within_the_radius_of_the_center_are_loaded()
        {
            var data = Load(CreateMap((0, 0), (1, 0), (3, 3)));
            var streamer = new ChunkStreamer(data, 0.5f);

            Assert.That(streamer.Chunks, Is.Empty, "nothing is loaded before a center is set");

            streamer.SetCenter(new Vector2(1f, 1f));

            Assert.That(streamer.Chunks, Is.EquivalentTo(new[] { new Vector2Int(0, 0) }));

            streamer.SetCenter(new Vector2(1.9f, 1f));

            Assert.That(streamer.Chunks,
                Is.EquivalentTo(new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }),
                "the neighbor chunk's edge came within the radius");
        }

        [Test]
        public void A_chunk_the_map_never_wrote_loads_as_open_water()
        {
            var data = Load(CreateMap((0, 0)));
            var streamer = new ChunkStreamer(data, 0.5f);

            streamer.SetCenter(new Vector2(3f, 3f));

            Assert.That(streamer.Chunks, Is.EquivalentTo(new[] { new Vector2Int(1, 1) }),
                "the chunk the map never wrote is open water, not a hole");
        }

        [Test]
        public void Chunks_left_behind_unload_as_the_center_moves()
        {
            var data = Load(CreateMap((0, 0), (3, 0)));
            var loader = new RecordingLoader();
            var streamer = new ChunkStreamer(data, 0.5f, loader);

            streamer.SetCenter(new Vector2(1f, 1f));

            Assert.That(loader.Loaded, Is.EquivalentTo(new[] { new Vector2Int(0, 0) }));
            Assert.That(loader.Unloaded, Is.Empty);

            streamer.SetCenter(new Vector2(7f, 1f));

            Assert.That(streamer.Chunks, Is.EquivalentTo(new[] { new Vector2Int(3, 0) }));
            Assert.That(loader.Loaded, Is.EquivalentTo(new[] { new Vector2Int(0, 0), new Vector2Int(3, 0) }));
            Assert.That(loader.Unloaded, Is.EquivalentTo(new[] { new Vector2Int(0, 0) }),
                "the chunk left behind unloaded");
        }

        [Test]
        public void A_center_move_within_the_same_chunk_changes_nothing()
        {
            var data = Load(CreateMap((0, 0)));
            var loader = new RecordingLoader();
            var streamer = new ChunkStreamer(data, 0.5f, loader);

            streamer.SetCenter(new Vector2(0.5f, 0.5f));

            loader.Loaded.Clear();

            streamer.SetCenter(new Vector2(1f, 1f));

            Assert.That(loader.Loaded, Is.Empty);
            Assert.That(loader.Unloaded, Is.Empty);
            Assert.That(streamer.Chunks, Is.EquivalentTo(new[] { new Vector2Int(0, 0) }));
        }

        [Test]
        public void Setting_the_center_it_already_holds_does_no_work()
        {
            var data = Load(CreateMap((0, 0)));
            var loader = new RecordingLoader();
            var streamer = new ChunkStreamer(data, 0.5f, loader);

            streamer.SetCenter(new Vector2(1f, 1f));

            loader.Loaded.Clear();

            streamer.SetCenter(new Vector2(1f, 1f));

            Assert.That(loader.Loaded, Is.Empty);
            Assert.That(loader.Unloaded, Is.Empty);
        }

        [Test]
        public void Every_loader_hears_about_every_chunk()
        {
            var data = Load(CreateMap((0, 0), (3, 0)));
            var first = new RecordingLoader();
            var second = new RecordingLoader();
            var streamer = new ChunkStreamer(data, 0.5f, first, second);

            streamer.SetCenter(new Vector2(1f, 1f));
            streamer.SetCenter(new Vector2(7f, 1f));

            Assert.That(second.Loaded, Is.EqualTo(first.Loaded));
            Assert.That(second.Unloaded, Is.EqualTo(first.Unloaded));
            Assert.That(first.Loaded, Is.EqualTo(new[] { new Vector2Int(0, 0), new Vector2Int(3, 0) }));
            Assert.That(first.Unloaded, Is.EqualTo(new[] { new Vector2Int(0, 0) }));
        }

        [Test]
        public void Disposing_unloads_everything_it_holds()
        {
            var data = Load(CreateMap((0, 0), (1, 0)));
            var loader = new RecordingLoader();
            var streamer = new ChunkStreamer(data, 0.5f, loader);

            streamer.SetCenter(new Vector2(1.9f, 1f));

            Assert.That(streamer.Chunks, Has.Count.EqualTo(2));

            streamer.Dispose();

            Assert.That(streamer.Chunks, Is.Empty);
            Assert.That(loader.Unloaded,
                Is.EquivalentTo(new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }));

            streamer.Dispose();

            Assert.That(loader.Unloaded, Has.Count.EqualTo(2), "a second dispose unloads nothing again");
        }
    }
}
