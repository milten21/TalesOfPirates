using System;
using System.Collections.Generic;
using Top.Client.Core;
using Top.Contracts.Assets.Maps;
using UnityEngine;
using UnityEngine.Rendering;

namespace Top.Client.Game.World.Terrain
{
    /// <summary>
    /// Terrain meshes for the chunks in the window, one quad per painted tile carrying
    /// its heights, vertex colors and texture layers.
    /// </summary>
    public class MapTerrain : IDisposable
    {
        private const int Layers = 4;

        private readonly MapData _map;
        private readonly ChunkWindow _chunks;
        private readonly Transform _parent;
        private readonly Material _material;
        private readonly Dictionary<Vector2Int, GameObject> _built = new Dictionary<Vector2Int, GameObject>();

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<Vector4> _uv0 = new List<Vector4>();
        private readonly List<Vector4> _uv1 = new List<Vector4>();
        private readonly List<Vector4> _slices = new List<Vector4>();
        private readonly List<int> _triangles = new List<int>();

        public MapTerrain(MapData map, ChunkWindow chunks, Transform parent, Material material)
        {
            _map = map;
            _chunks = chunks;
            _parent = parent;
            _material = material;

            _chunks.Added += Build;
            _chunks.Removed += Release;

            foreach (var chunk in _chunks.Chunks)
            {
                Build(chunk);
            }
        }

        public void Dispose()
        {
            _chunks.Added -= Build;
            _chunks.Removed -= Release;

            foreach (var chunk in _built.Values)
            {
                Destroy(chunk);
            }

            _built.Clear();
        }

        private void Build(Vector2Int chunk)
        {
            var size = _map.ChunkSize;
            var chunkObject = new GameObject($"chunk_{chunk.x}_{chunk.y}");

            chunkObject.transform.SetParent(_parent, worldPositionStays: false);
            chunkObject.transform.localPosition = MapSpace.ToWorld(chunk.x * size, chunk.y * size, 0f);

            var filter = chunkObject.AddComponent<MeshFilter>();
            var renderer = chunkObject.AddComponent<MeshRenderer>();

            filter.sharedMesh = BuildMesh(chunk);
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;

            _built[chunk] = chunkObject;
        }

        private Mesh BuildMesh(Vector2Int chunk)
        {
            var size = _map.ChunkSize;
            var layers = new MapTileLayer[Layers];

            Clear();

            if (_map.HasChunk(chunk.x, chunk.y))
            {
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var tileX = (chunk.x * size) + x;
                        var tileY = (chunk.y * size) + y;

                        if (TryReadLayers(tileX, tileY, layers))
                        {
                            AddTile(x, y, tileX, tileY, layers);
                        }
                    }
                }
            }
            else
            {
                AddUnderwater(size);
            }

            var mesh = new Mesh();

            mesh.SetVertices(_vertices);
            mesh.SetColors(_colors);
            mesh.SetUVs(0, _uv0);
            mesh.SetUVs(1, _uv1);
            mesh.SetUVs(2, _slices);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();

            Clear();

            return mesh;
        }

        private bool TryReadLayers(int tileX, int tileY, MapTileLayer[] layers)
        {
            var stopped = false;

            for (var i = 0; i < Layers; i++)
            {
                var layer = _map.LayerAt(tileX, tileY, i);
                var painted = !stopped && layer.TerrainId != 0 && layer.MaskIndex != 0;

                stopped |= layer.TerrainId == 0;
                layers[i] = painted ? layer : default;
            }

            return layers[0].TerrainId != 0;
        }

        private void AddTile(int x, int y, int tileX, int tileY, MapTileLayer[] layers)
        {
            var first = _vertices.Count;
            var slices = new Vector4(layers[0].TerrainId, layers[1].TerrainId, layers[2].TerrainId,
                layers[3].TerrainId);

            for (var cornerY = 0; cornerY <= 1; cornerY++)
            {
                for (var cornerX = 0; cornerX <= 1; cornerX++)
                {
                    var height = _map.HeightAt(tileX + cornerX, tileY + cornerY);
                    var baseUv = TerrainUv.BaseUv(tileX, tileY, cornerX, cornerY);
                    var mask1 = TerrainUv.MaskUv(layers[1].MaskIndex, cornerX, cornerY);
                    var mask2 = TerrainUv.MaskUv(layers[2].MaskIndex, cornerX, cornerY);
                    var mask3 = TerrainUv.MaskUv(layers[3].MaskIndex, cornerX, cornerY);

                    _vertices.Add(MapSpace.ToWorld(x + cornerX, y + cornerY, height));
                    _colors.Add(_map.ColorAt(tileX + cornerX, tileY + cornerY));
                    _uv0.Add(new Vector4(baseUv.x, baseUv.y, mask1.x, mask1.y));
                    _uv1.Add(new Vector4(mask2.x, mask2.y, mask3.x, mask3.y));
                    _slices.Add(slices);
                }
            }

            _triangles.Add(first);
            _triangles.Add(first + 1);
            _triangles.Add(first + 2);
            _triangles.Add(first + 1);
            _triangles.Add(first + 3);
            _triangles.Add(first + 2);
        }

        private void AddUnderwater(int size)
        {
            var ground = MapTile.Underwater;
            var color = new Color32(ground.ColorR, ground.ColorG, ground.ColorB, byte.MaxValue);
            var slices = new Vector4(ground.Layer0.TerrainId, 0f, 0f, 0f);

            for (var cornerY = 0; cornerY <= 1; cornerY++)
            {
                for (var cornerX = 0; cornerX <= 1; cornerX++)
                {
                    var baseUv = TerrainUv.BaseUvSpan(size, cornerX, cornerY);

                    _vertices.Add(MapSpace.ToWorld(cornerX * size, cornerY * size, ground.Height));
                    _colors.Add(color);
                    _uv0.Add(new Vector4(baseUv.x, baseUv.y, 0f, 0f));
                    _uv1.Add(Vector4.zero);
                    _slices.Add(slices);
                }
            }

            _triangles.Add(0);
            _triangles.Add(1);
            _triangles.Add(2);
            _triangles.Add(1);
            _triangles.Add(3);
            _triangles.Add(2);
        }

        private void Clear()
        {
            _vertices.Clear();
            _colors.Clear();
            _uv0.Clear();
            _uv1.Clear();
            _slices.Clear();
            _triangles.Clear();
        }

        private void Release(Vector2Int chunk)
        {
            if (_built.Remove(chunk, out var chunkObject))
            {
                Destroy(chunkObject);
            }
        }

        private static void Destroy(GameObject chunkObject)
        {
            if (chunkObject == null)
            {
                return;
            }

            UnityObjects.Destroy(chunkObject.GetComponent<MeshFilter>().sharedMesh);
            UnityObjects.Destroy(chunkObject);
        }
    }
}
