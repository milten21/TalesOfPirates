using System;
using System.Collections.Generic;
using Top.Legacy.Tables;
using Top.Legacy.Tables.Records;
using Top.Logging;
using Converted = Top.Contracts.Assets.Maps;
using Original = Top.Legacy.MindPower.World;

namespace Top.Conversion.Pipeline.Maps
{
    /// <summary>
    /// Builds a converted map from a .map terrain file and its .obj objects.
    /// Both tile record versions convert, in meters and degrees.
    /// </summary>
    public class MapBuilder(Original.MapFile terrain, Original.ObjFile objects, Table<TerrainInfoRecord> terrains)
    {
        public const int ChunkSize = 64;

        private const int ModelKind = 0;
        private const int EffectKind = 1;
        private const int IdMask = 0x3FFF;
        private const int Centimeters = 100;
        private const float RadianHundredths = 100f;

        /// <summary>
        /// The base layer always covers fully. The earlier record's own value is
        /// dropped, as TileInfo_8To5 drops it (MPMapDef.h).
        /// </summary>
        private const byte BaseMask = 15;

        private readonly HashSet<byte> _reported = [];

        private int _offMap;
        private int _openWater;
        private int _unknownKind;

        public Converted.MapFile Build()
        {
            var countX = ChunkCount(terrain.Width);
            var countY = ChunkCount(terrain.Height);
            var chunks = new Converted.MapChunk[countX, countY];

            for (var chunkY = 0; chunkY < countY; chunkY++)
            {
                for (var chunkX = 0; chunkX < countX; chunkX++)
                {
                    chunks[chunkX, chunkY] = Chunk(chunkX * ChunkSize, chunkY * ChunkSize);
                }
            }

            var map = new Converted.MapFile(terrain.Width, terrain.Height, ChunkSize);

            Array.Copy(chunks, map.Chunks, chunks.Length);

            Place(map);

            return map;
        }

        private static int ChunkCount(int tiles)
        {
            return (tiles + ChunkSize - 1) / ChunkSize;
        }

        /// <summary>
        /// The current record packs color as B5G6R5, not R5G6B5
        /// (LW_RGB565TODWORD, MapDataVer.cpp). The earlier one stores 0xAARRGGBB.
        /// </summary>
        private static uint PackedColor(Original.MapTile tile, bool oldFormat)
        {
            if (oldFormat)
            {
                return tile.Color888;
            }

            var packed = (ushort)tile.Color565;
            var red = (uint)((packed & 0x001F) << 3);
            var green = (uint)((packed & 0x07E0) >> 3);
            var blue = (uint)((packed & 0xF800) >> 8);

            return (red << 16) | (green << 8) | blue;
        }

        private Converted.MapChunk Chunk(int originX, int originY)
        {
            if (!HasTerrain(originX, originY))
            {
                return null;
            }

            var chunk = new Converted.MapChunk(ChunkSize);

            for (var y = 0; y < ChunkSize; y++)
            {
                for (var x = 0; x < ChunkSize; x++)
                {
                    chunk.Tiles[(y * ChunkSize) + x] = Tile(originX + x, originY + y);
                }
            }

            return chunk;
        }

        private bool HasTerrain(int originX, int originY)
        {
            var firstX = originX / terrain.SectionWidth;
            var firstY = originY / terrain.SectionHeight;
            var lastX = Math.Min((originX + ChunkSize - 1) / terrain.SectionWidth, terrain.SectionCountX - 1);
            var lastY = Math.Min((originY + ChunkSize - 1) / terrain.SectionHeight, terrain.SectionCountY - 1);

            for (var sectionY = firstY; sectionY <= lastY; sectionY++)
            {
                for (var sectionX = firstX; sectionX <= lastX; sectionX++)
                {
                    if (terrain.Sections[(sectionY * terrain.SectionCountX) + sectionX] != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Converted.MapTile Tile(int x, int y)
        {
            var source = OriginalTile(x, y);

            if (source == null)
            {
                return Converted.MapTile.Underwater;
            }

            var oldFormat = terrain.IsOldFormat;
            var color = PackedColor(source, oldFormat);

            return new Converted.MapTile
            {
                Height = (oldFormat ? source.HeightCm : source.HeightStep * 10) / (float)Centimeters,
                ColorR = (byte)((color >> 16) & 0xFF),
                ColorG = (byte)((color >> 8) & 0xFF),
                ColorB = (byte)(color & 0xFF),
                Layer0 = Layer(source.Texture0, BaseMask),
                Layer1 = Layer(source.Texture1, source.Alpha1),
                Layer2 = Layer(source.Texture2, source.Alpha2),
                Layer3 = Layer(source.Texture3, source.Alpha3),
                Region = (ushort)source.Region,
                Island = source.Island,
                Cell00 = source.Block[0],
                Cell10 = source.Block[1],
                Cell01 = source.Block[2],
                Cell11 = source.Block[3],
            };
        }

        private Original.MapTile OriginalTile(int x, int y)
        {
            var sectionX = x / terrain.SectionWidth;
            var sectionY = y / terrain.SectionHeight;

            if (sectionX >= terrain.SectionCountX || sectionY >= terrain.SectionCountY)
            {
                return null;
            }

            var section = terrain.Sections[(sectionY * terrain.SectionCountX) + sectionX];

            return section?.Tiles[((y % terrain.SectionHeight) * terrain.SectionWidth)
                                  + (x % terrain.SectionWidth)];
        }

        private Converted.MapTileLayer Layer(byte terrainId, byte mask)
        {
            if (terrainId == 0)
            {
                return default;
            }

            if (terrains == null || !terrains.TryGetById(terrainId, out var row) || string.IsNullOrEmpty(row.Name))
            {
                if (_reported.Add(terrainId))
                {
                    Log.Warning($"no terraininfo row {terrainId}, leaving the layer unpainted");
                }

                return default;
            }

            return new Converted.MapTileLayer
            {
                TerrainId = terrainId,
                MaskIndex = mask,
            };
        }

        /// <summary>
        /// Object positions are relative to their section, so the section corner
        /// is added back (CSceneObjFile::ReadSectionObjInfo, SceneObjFile.cpp).
        /// </summary>
        private void Place(Converted.MapFile map)
        {
            if (objects?.Sections == null)
            {
                return;
            }

            for (var i = 0; i < objects.Sections.Length; i++)
            {
                var section = objects.Sections[i];

                if (section?.Objects == null)
                {
                    continue;
                }

                var originX = (i % objects.SectionCountX) * objects.SectionWidth * Centimeters;
                var originY = (i / objects.SectionCountX) * objects.SectionHeight * Centimeters;

                foreach (var placed in section.Objects)
                {
                    Place(map, placed, originX, originY);
                }
            }

            if (_unknownKind > 0)
            {
                Log.Warning($"dropped {_unknownKind} placements of a kind the map format has no record for");
            }

            if (_offMap > 0)
            {
                Log.Warning($"dropped {_offMap} placements standing past the map edge");
            }

            if (_openWater > 0)
            {
                Log.Warning($"dropped {_openWater} placements standing where no section wrote terrain");
            }
        }

        private void Place(Converted.MapFile map, Original.SceneObject placed, int originX, int originY)
        {
            var kind = ((ushort)placed.TypeId >> 14) & 3;

            if (kind != ModelKind && kind != EffectKind)
            {
                _unknownKind++;

                return;
            }

            var x = (placed.X + originX) / (float)Centimeters;
            var y = (placed.Y + originY) / (float)Centimeters;
            var chunkX = (int)Math.Floor(x / ChunkSize);
            var chunkY = (int)Math.Floor(y / ChunkSize);

            if (chunkX < 0 || chunkY < 0 || chunkX >= map.ChunkCountX || chunkY >= map.ChunkCountY)
            {
                _offMap++;

                return;
            }

            if (map.Chunks[chunkX, chunkY] == null)
            {
                _openWater++;

                return;
            }

            map.Chunks[chunkX, chunkY].Placements.Add(new Converted.MapPlacement
            {
                Kind = kind == ModelKind ? Converted.PlacementKind.Model : Converted.PlacementKind.Effect,
                Id = placed.TypeId & IdMask,
                X = x,
                Y = y,
                HeightOffset = placed.HeightOff / (float)Centimeters,
                Yaw = kind == ModelKind
                    ? placed.YawAngle
                    : (placed.YawAngle / RadianHundredths) * (180f / MathF.PI),
            });
        }
    }
}
