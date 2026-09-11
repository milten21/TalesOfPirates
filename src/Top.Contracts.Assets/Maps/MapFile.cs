using System.IO;
using System.IO.Compression;
using System.Text;

namespace Top.Contracts.Assets.Maps
{
    public class MapFile
    {
        public const int Version = 1;

        public readonly int Width;
        public readonly int Height;
        public readonly int ChunkSize;
        public readonly MapChunk[,] Chunks;

        public MapFile(int width, int height, int chunkSize)
        {
            Width = width;
            Height = height;
            ChunkSize = chunkSize;
            Chunks = new MapChunk[ChunkCountX, ChunkCountY];
        }

        public int ChunkCountX => (Width + ChunkSize - 1) / ChunkSize;
        public int ChunkCountY => (Height + ChunkSize - 1) / ChunkSize;

        public void Write(Stream stream)
        {
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(Version);
            }

            using var body = new MemoryStream();

            WriteBody(body);

            body.Position = 0;

            using var deflated = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true);

            body.CopyTo(deflated);
        }

        private void WriteBody(Stream stream)
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            writer.Write(Width);
            writer.Write(Height);
            writer.Write(ChunkSize);

            var tablePosition = stream.Position;

            for (var i = 0; i < ChunkCountX * ChunkCountY; i++)
            {
                writer.Write(0L);
            }

            var offsets = new long[ChunkCountX * ChunkCountY];

            for (var chunkY = 0; chunkY < ChunkCountY; chunkY++)
            {
                for (var chunkX = 0; chunkX < ChunkCountX; chunkX++)
                {
                    var chunk = Chunks[chunkX, chunkY];

                    if (chunk == null)
                    {
                        continue;
                    }

                    offsets[chunkY * ChunkCountX + chunkX] = stream.Position;
                    WriteChunk(writer, chunk);
                }
            }

            var end = stream.Position;
            stream.Position = tablePosition;

            foreach (var offset in offsets)
            {
                writer.Write(offset);
            }

            stream.Position = end;
        }

        private static void WriteChunk(BinaryWriter writer, MapChunk chunk)
        {
            foreach (var tile in chunk.Tiles)
            {
                writer.Write(tile.Height);
                writer.Write(tile.ColorR);
                writer.Write(tile.ColorG);
                writer.Write(tile.ColorB);
                WriteLayer(writer, tile.Layer0);
                WriteLayer(writer, tile.Layer1);
                WriteLayer(writer, tile.Layer2);
                WriteLayer(writer, tile.Layer3);
                writer.Write(tile.Region);
                writer.Write(tile.Island);
                writer.Write(tile.Cell00);
                writer.Write(tile.Cell10);
                writer.Write(tile.Cell01);
                writer.Write(tile.Cell11);
            }

            writer.Write(chunk.Placements.Count);

            foreach (var placement in chunk.Placements)
            {
                writer.Write((byte)placement.Kind);
                writer.Write(placement.Id);
                writer.Write(placement.X);
                writer.Write(placement.Y);
                writer.Write(placement.HeightOffset);
                writer.Write(placement.Yaw);
            }
        }

        private static void WriteLayer(BinaryWriter writer, MapTileLayer layer)
        {
            writer.Write(layer.TerrainId);
            writer.Write(layer.MaskIndex);
        }

        public static MapFile Read(Stream stream)
        {
            int version;

            using (var header = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                version = header.ReadInt32();
            }

            if (version != Version)
            {
                throw new InvalidDataException($"Unknown map format version {version}, expected {Version}.");
            }

            using var body = new MemoryStream();

            using (var deflated = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true))
            {
                deflated.CopyTo(body);
            }

            body.Position = 0;

            return ReadBody(body);
        }

        private static MapFile ReadBody(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var chunkSize = reader.ReadInt32();
            var map = new MapFile(width, height, chunkSize);

            var offsets = new long[map.ChunkCountX * map.ChunkCountY];

            for (var i = 0; i < offsets.Length; i++)
            {
                offsets[i] = reader.ReadInt64();
            }

            for (var chunkY = 0; chunkY < map.ChunkCountY; chunkY++)
            {
                for (var chunkX = 0; chunkX < map.ChunkCountX; chunkX++)
                {
                    var offset = offsets[chunkY * map.ChunkCountX + chunkX];
                    if (offset == 0)
                    {
                        continue;
                    }

                    stream.Position = offset;
                    map.Chunks[chunkX, chunkY] = ReadChunk(reader, chunkSize);
                }
            }

            return map;
        }

        private static MapChunk ReadChunk(BinaryReader reader, int chunkSize)
        {
            var chunk = new MapChunk(chunkSize);

            for (var i = 0; i < chunk.Tiles.Length; i++)
            {
                chunk.Tiles[i] = new MapTile
                {
                    Height = reader.ReadSingle(),
                    ColorR = reader.ReadByte(),
                    ColorG = reader.ReadByte(),
                    ColorB = reader.ReadByte(),
                    Layer0 = ReadLayer(reader),
                    Layer1 = ReadLayer(reader),
                    Layer2 = ReadLayer(reader),
                    Layer3 = ReadLayer(reader),
                    Region = reader.ReadUInt16(),
                    Island = reader.ReadByte(),
                    Cell00 = reader.ReadByte(),
                    Cell10 = reader.ReadByte(),
                    Cell01 = reader.ReadByte(),
                    Cell11 = reader.ReadByte(),
                };
            }

            var placementCount = reader.ReadInt32();

            for (var i = 0; i < placementCount; i++)
            {
                chunk.Placements.Add(new MapPlacement
                {
                    Kind = (PlacementKind)reader.ReadByte(),
                    Id = reader.ReadInt32(),
                    X = reader.ReadSingle(),
                    Y = reader.ReadSingle(),
                    HeightOffset = reader.ReadSingle(),
                    Yaw = reader.ReadSingle(),
                });
            }

            return chunk;
        }

        private static MapTileLayer ReadLayer(BinaryReader reader)
        {
            return new MapTileLayer
            {
                TerrainId = reader.ReadByte(),
                MaskIndex = reader.ReadByte(),
            };
        }
    }
}
