using System.Threading.Tasks;
using NUnit.Framework;
using Top.Client.Game.World;
using Top.Content;
using Top.Contracts.Assets.Maps;

namespace Top.Client.Game.Tests.World
{
    public class MapReaderTests
    {
        [Test]
        public void Map_content_the_tree_does_not_hold_fails_the_read()
        {
            var reader = new MapReader(new MemoryContentSource());

            Assert.ThrowsAsync<System.IO.FileNotFoundException>(() => reader.Read("maps/missing.map"));
        }

        [Test]
        public async Task A_map_reads_back_through_the_content_port()
        {
            var map = new MapFile(4, 4, 2);
            var content = new MemoryContentSource();

            map.Chunks[0, 0] = new MapChunk(map.ChunkSize);

            using var stream = new System.IO.MemoryStream();

            map.Write(stream);
            content.Add("maps/test.map", stream.ToArray());

            var reader = new MapReader(content);

            var data = await reader.Read("maps/test.map");

            Assert.That(data.Width, Is.EqualTo(4));
            Assert.That(data.HasChunk(0, 0), Is.True);
            Assert.That(data.HasChunk(1, 1), Is.False);
        }
    }
}
