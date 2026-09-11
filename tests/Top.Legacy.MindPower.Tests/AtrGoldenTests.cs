using System.IO;
using NUnit.Framework;
using Top.Legacy.MindPower.World;

namespace Top.Legacy.MindPower.Tests
{
    public class AtrGoldenTests
    {
        [Test]
        public void Parses_known_fixture()
        {
            // teampk.atr: SAttribFileHeader width=96 height=96 (TerrainAttrib.h:55), then 96*96
            // STILE_ATTRIB tiless row-major (TerrainAttrib.cpp:_seekTile:131). Every tiles carries
            // attrib bit 1 set (mask 0x0001) and island index 95 -- hand-decoded from the fixture
            // bytes (tile[0] at offset 8 = 01 00 5f).
            using var stream = File.OpenRead(Fixtures.Path("atr/teampk.atr"));
            var file = AtrFile.Read(stream);

            Assert.That(file.Grid.Width, Is.EqualTo(96));
            Assert.That(file.Grid.Height, Is.EqualTo(96));
            Assert.That(file.Grid.Tiles.Length, Is.EqualTo(96 * 96));

            // tile (0,0): attrib mask 0x0001 (Bit1), island 95.
            Assert.That(file.Grid[0, 0].Attrib, Is.EqualTo(TerrainAttribute.Bit1));
            Assert.That(file.Grid[0, 0].Island, Is.EqualTo((byte)95));

            // a few interior tiles confirm the uniform fill and row-major addressing.
            Assert.That(file.Grid[1, 0].Attrib, Is.EqualTo(TerrainAttribute.Bit1));
            Assert.That(file.Grid[0, 1].Island, Is.EqualTo((byte)95));
            Assert.That(file.Grid[50, 50].Attrib, Is.EqualTo(TerrainAttribute.Bit1));
            Assert.That(file.Grid[95, 95].Island, Is.EqualTo((byte)95));
        }

        [Test]
        public void Tile_packs_attrib_bits_and_island()
        {
            // The fixture is uniform, so drive a synthetic tile through Write/Read to pin the
            // ushort attribute-mask + byte island layout (STILE_ATTRIB, TerrainAttrib.h:63) and
            // multi-bit attribute flags (set via 1 << (bit-1), TerrainAttrib.cpp:339).
            var file = new AtrFile
            {
                Grid = new TerrainGrid(2, 1, new TerrainTile[2]),
            };
            file.Grid[0, 0] = new TerrainTile
            {
                Attrib = TerrainAttribute.Bit1 | TerrainAttribute.Bit4 | TerrainAttribute.Bit16,
                Island = 200,
            };
            file.Grid[1, 0] = new TerrainTile
            {
                Attrib = TerrainAttribute.None,
                Island = 0,
            };

            using var mem = new MemoryStream();
            file.Write(mem);
            mem.Position = 0;
            var read = AtrFile.Read(mem);

            // 0x8009 = bit1 | bit4 | bit16.
            Assert.That((ushort)read.Grid[0, 0].Attrib, Is.EqualTo((ushort)0x8009));
            Assert.That(read.Grid[0, 0].Attrib,
                Is.EqualTo(TerrainAttribute.Bit1 | TerrainAttribute.Bit4 | TerrainAttribute.Bit16));
            Assert.That(read.Grid[0, 0].Island, Is.EqualTo((byte)200));
            Assert.That(read.Grid[1, 0].Attrib, Is.EqualTo(TerrainAttribute.None));
            Assert.That(read.Grid[1, 0].Island, Is.EqualTo((byte)0));
        }

        [Test]
        public void Round_trips_fixture()
        {
            GoldenAssert.RoundTrips("atr/teampk.atr", AtrFile.Read, (s, f) => f.Write(s));
        }

        [Test]
        [Category("Fidelity")]
        public void Round_trips_all_fixtures()
        {
            GoldenAssert.RoundTripsAll("atr", "*.atr",
                AtrFile.Read, (s, f) => f.Write(s));
        }
    }
}
