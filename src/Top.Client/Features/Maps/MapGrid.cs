using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Top.Contracts.Assets.Maps;

namespace Top.Client.Features.Maps;

public class MapGrid(MapFile file)
{
    private const byte BlockedBit = 0x80;
    private const byte SignBit = 0x40;
    private const byte LevelMask = 0x3F;
    private const float LevelStep = 0.05f;

    public const int CellsPerTile = 2;

    public const float MaximumCellHeight = LevelMask * LevelStep;

    private static readonly MapPlacement[] NoPlacements = [];

    public int TileCountX => file.Width;
    public int TileCountY => file.Height;
    public int ChunkSize => file.ChunkSize;
    public int ChunkCountX => file.ChunkCountX;
    public int ChunkCountY => file.ChunkCountY;

    public float HeightAt(float x, float y)
    {
        var tileX = (int)Math.Floor(x);
        var tileY = (int)Math.Floor(y);

        var fx = x - tileX;
        var fy = y - tileY;

        var h10 = VertexHeightAt(tileX + 1, tileY);
        var h01 = VertexHeightAt(tileX, tileY + 1);

        if (fx + fy <= 1f)
        {
            var h00 = VertexHeightAt(tileX, tileY);

            return h00 + (fx * (h10 - h00)) + (fy * (h01 - h00));
        }

        var h11 = VertexHeightAt(tileX + 1, tileY + 1);

        return h11 + ((1f - fx) * (h01 - h11)) + ((1f - fy) * (h10 - h11));
    }

    public bool IsBlocked(int cellX, int cellY)
    {
        return !TryGetCell(cellX, cellY, out var cell) || (cell & BlockedBit) != 0;
    }

    public float CellHeightAt(int cellX, int cellY)
    {
        if (!TryGetCell(cellX, cellY, out var cell))
        {
            return 0f;
        }

        var height = (cell & LevelMask) * LevelStep;

        return (cell & SignBit) != 0 ? -height : height;
    }

    public float GroundHeightAt(float mapX, float mapY)
    {
        return CellHeightAt((int)(mapX * CellsPerTile), (int)(mapY * CellsPerTile));
    }

    public Vector3 GroundPointAt(Vector2 mapPoint)
    {
        return MapSpace.ToWorld(mapPoint.X, mapPoint.Y, GroundHeightAt(mapPoint.X, mapPoint.Y));
    }

    public Color ColorAt(int tileX, int tileY)
    {
        var tile = TileAt(tileX, tileY);

        return Color.Color8(tile.ColorR, tile.ColorG, tile.ColorB);
    }

    public MapTileLayer LayerAt(int tileX, int tileY, int layer)
    {
        var tile = TileAt(tileX, tileY);

        switch (layer)
        {
            case 0: return tile.Layer0;
            case 1: return tile.Layer1;
            case 2: return tile.Layer2;
            case 3: return tile.Layer3;
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public ushort RegionAt(int tileX, int tileY)
    {
        return TryGetTile(tileX, tileY, out var tile) ? tile.Region : (ushort)0;
    }

    public byte IslandAt(int tileX, int tileY)
    {
        return TryGetTile(tileX, tileY, out var tile) ? tile.Island : (byte)0;
    }

    public bool HasChunk(int chunkX, int chunkY)
    {
        return TryGetChunk(chunkX, chunkY, out _);
    }

    public IReadOnlyList<MapPlacement> PlacementsAt(int chunkX, int chunkY)
    {
        return TryGetChunk(chunkX, chunkY, out var chunk) ? chunk.Placements : NoPlacements;
    }

    public float MeasurePlacementHeight(MapPlacement placement)
    {
        return Mathf.Max(HeightAt(placement.X, placement.Y), 0f) + placement.HeightOffset;
    }

    private bool TryGetChunk(int chunkX, int chunkY, [NotNullWhen(true)] out MapChunk? chunk)
    {
        chunk = null;

        if (chunkX < 0 || chunkY < 0 || chunkX >= ChunkCountX || chunkY >= ChunkCountY)
        {
            return false;
        }

        chunk = file.Chunks[chunkX, chunkY];

        return chunk != null;
    }

    private bool TryGetCell(int cellX, int cellY, out byte cell)
    {
        cell = 0;

        if (cellX < 0 || cellY < 0 || !TryGetTile(cellX / 2, cellY / 2, out var tile))
        {
            return false;
        }

        cell = cellX % 2 == 0
            ? (cellY % 2 == 0 ? tile.Cell00 : tile.Cell01)
            : (cellY % 2 == 0 ? tile.Cell10 : tile.Cell11);

        return true;
    }

    private float VertexHeightAt(int x, int y)
    {
        return TileAt(x, y).Height;
    }

    private MapTile TileAt(int x, int y)
    {
        return TryGetTile(x, y, out var tile) ? tile : MapTile.Underwater;
    }

    private bool TryGetTile(int x, int y, out MapTile tile)
    {
        tile = default;

        if (x < 0 || y < 0 || x >= TileCountX || y >= TileCountY
            || !TryGetChunk(x / ChunkSize, y / ChunkSize, out var chunk))
        {
            return false;
        }

        tile = chunk.Tiles[((y % ChunkSize) * ChunkSize) + (x % ChunkSize)];

        return true;
    }
}
