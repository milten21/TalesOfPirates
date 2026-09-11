using System.Collections.Generic;
using Top.Client.Core;
using Top.Client.Game.World.Terrain;
using Top.Contracts.Assets.Maps;
using UnityEngine;
using UnityEngine.Rendering;

namespace Top.Client.Game.World
{
    public class TerrainLoader : IChunkLoader
    {
        private const int TileLayers = 4;

        private readonly MapData _mapData;
        private readonly Transform _parent;
        private readonly Material _material;
        private readonly Dictionary<Vector2Int, GameObject> _loadedChunks = new Dictionary<Vector2Int, GameObject>();

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<Vector4> _uv0 = new List<Vector4>();
        private readonly List<Vector4> _uv1 = new List<Vector4>();
        private readonly List<Vector4> _slices = new List<Vector4>();
        private readonly List<int> _triangles = new List<int>();

        public TerrainLoader(MapData mapData, Transform parent, Material material)
        {
            _mapData = mapData;
            _parent = parent;
            _material = material;
        }

        public void Load(Vector2Int chunk)
        {
            var chunkSize = _mapData.ChunkSize;
            var chunkObject = new GameObject($"chunk_{chunk.x}_{chunk.y}");

            chunkObject.transform.SetParent(_parent, worldPositionStays: false);
            chunkObject.transform.localPosition = MapSpace.ToWorld(chunk.x * chunkSize, chunk.y * chunkSize, 0f);

            var filter = chunkObject.AddComponent<MeshFilter>();
            var renderer = chunkObject.AddComponent<MeshRenderer>();

            filter.sharedMesh = BuildMesh(chunk);
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;

            _loadedChunks[chunk] = chunkObject;
        }

        public void Unload(Vector2Int chunk)
        {
            if (_loadedChunks.Remove(chunk, out var chunkObject))
            {
                Destroy(chunkObject);
            }
        }

        private Mesh BuildMesh(Vector2Int chunk)
        {
            var chunkSize = _mapData.ChunkSize;
            var layers = new MapTileLayer[TileLayers];

            Clear();

            if (_mapData.HasChunk(chunk.x, chunk.y))
            {
                for (var localY = 0; localY < chunkSize; localY++)
                {
                    for (var localX = 0; localX < chunkSize; localX++)
                    {
                        var tileX = (chunk.x * chunkSize) + localX;
                        var tileY = (chunk.y * chunkSize) + localY;

                        if (TryReadLayers(tileX, tileY, layers))
                        {
                            AddTile(localX, localY, tileX, tileY, layers);
                        }
                    }
                }
            }
            else
            {
                AddOpenWater(chunkSize);
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
            var ended = false;

            for (var i = 0; i < TileLayers; i++)
            {
                var layer = _mapData.LayerAt(tileX, tileY, i);
                var painted = !ended && layer.TerrainId != 0 && layer.MaskIndex != 0;

                ended |= layer.TerrainId == 0;
                layers[i] = painted ? layer : default;
            }

            return layers[0].TerrainId != 0;
        }

        private void AddTile(int localX, int localY, int tileX, int tileY, MapTileLayer[] layers)
        {
            var firstVertex = _vertices.Count;
            var slices = new Vector4(layers[0].TerrainId, layers[1].TerrainId, layers[2].TerrainId,
                layers[3].TerrainId);

            for (var cornerY = 0; cornerY <= 1; cornerY++)
            {
                for (var cornerX = 0; cornerX <= 1; cornerX++)
                {
                    var height = _mapData.HeightAt(tileX + cornerX, tileY + cornerY);
                    var baseUv = TerrainUv.BaseUvAt(tileX, tileY, cornerX, cornerY);
                    var mask1 = TerrainUv.MaskUvAt(layers[1].MaskIndex, cornerX, cornerY);
                    var mask2 = TerrainUv.MaskUvAt(layers[2].MaskIndex, cornerX, cornerY);
                    var mask3 = TerrainUv.MaskUvAt(layers[3].MaskIndex, cornerX, cornerY);

                    _vertices.Add(MapSpace.ToWorld(localX + cornerX, localY + cornerY, height));
                    _colors.Add(_mapData.ColorAt(tileX + cornerX, tileY + cornerY));
                    _uv0.Add(new Vector4(baseUv.x, baseUv.y, mask1.x, mask1.y));
                    _uv1.Add(new Vector4(mask2.x, mask2.y, mask3.x, mask3.y));
                    _slices.Add(slices);
                }
            }

            _triangles.Add(firstVertex);
            _triangles.Add(firstVertex + 1);
            _triangles.Add(firstVertex + 2);
            _triangles.Add(firstVertex + 1);
            _triangles.Add(firstVertex + 3);
            _triangles.Add(firstVertex + 2);
        }

        private void AddOpenWater(int chunkSize)
        {
            var ground = MapTile.Underwater;
            var color = new Color32(ground.ColorR, ground.ColorG, ground.ColorB, byte.MaxValue);
            var slices = new Vector4(ground.Layer0.TerrainId, 0f, 0f, 0f);

            for (var cornerY = 0; cornerY <= 1; cornerY++)
            {
                for (var cornerX = 0; cornerX <= 1; cornerX++)
                {
                    var baseUv = TerrainUv.BaseUvAtSpanCorner(chunkSize, cornerX, cornerY);

                    _vertices.Add(MapSpace.ToWorld(cornerX * chunkSize, cornerY * chunkSize, ground.Height));
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
