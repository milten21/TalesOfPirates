using System;
using System.Collections.Generic;
using Top.Client.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Top.Client.Assets.Maps
{
    /// <summary>
    /// The water at height zero over the chunks in the window, one quad per four-by-four tiles.
    /// </summary>
    public class MapWater : IDisposable
    {
        private const int QuadTiles = 4;
        private const float FadeDepth = -0.5f;

        private static readonly Color32 Water = new Color32(140, 140, 220, 207);
        private static readonly Color32 Faded = new Color32(255, 255, 255, 0);

        private readonly MapData _map;
        private readonly ChunkWindow _chunks;
        private readonly Transform _parent;
        private readonly Material _material;
        private readonly Dictionary<Vector2Int, GameObject> _built = new Dictionary<Vector2Int, GameObject>();

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<Vector2> _uv = new List<Vector2>();
        private readonly List<int> _triangles = new List<int>();

        public MapWater(MapData map, ChunkWindow chunks, Transform parent, Material material)
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
            var mesh = BuildMesh(chunk);

            if (mesh == null)
            {
                return;
            }

            var size = _map.ChunkSize;
            var chunkObject = new GameObject($"water_{chunk.x}_{chunk.y}");

            chunkObject.transform.SetParent(_parent, worldPositionStays: false);
            chunkObject.transform.localPosition = MapSpace.ToWorld(chunk.x * size, chunk.y * size, 0f);

            var filter = chunkObject.AddComponent<MeshFilter>();
            var renderer = chunkObject.AddComponent<MeshRenderer>();

            filter.sharedMesh = mesh;
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            _built[chunk] = chunkObject;
        }

        private Mesh BuildMesh(Vector2Int chunk)
        {
            var quads = _map.ChunkSize / QuadTiles;
            var vertexRow = quads + 1;
            var visible = false;

            Clear();

            for (var y = 0; y <= quads; y++)
            {
                for (var x = 0; x <= quads; x++)
                {
                    var mapX = ((chunk.x * quads) + x) * QuadTiles;
                    var mapY = ((chunk.y * quads) + y) * QuadTiles;
                    var color = VertexColor(mapX, mapY);

                    visible |= color.a != 0;

                    _vertices.Add(MapSpace.ToWorld(x * QuadTiles, y * QuadTiles, 0f));
                    _colors.Add(color);
                    _uv.Add(new Vector2(mapX / (float)QuadTiles, 1f - (mapY / (float)QuadTiles)));
                }
            }

            if (!visible)
            {
                Clear();

                return null;
            }

            for (var y = 0; y < quads; y++)
            {
                for (var x = 0; x < quads; x++)
                {
                    var first = (y * vertexRow) + x;

                    _triangles.Add(first);
                    _triangles.Add(first + 1);
                    _triangles.Add(first + vertexRow);
                    _triangles.Add(first + 1);
                    _triangles.Add(first + vertexRow + 1);
                    _triangles.Add(first + vertexRow);
                }
            }

            var mesh = new Mesh();

            mesh.SetVertices(_vertices);
            mesh.SetColors(_colors);
            mesh.SetUVs(0, _uv);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();

            Clear();

            return mesh;
        }

        private Color32 VertexColor(int mapX, int mapY)
        {
            return _map.HeightAt(mapX, mapY) > FadeDepth ? Faded : Water;
        }

        private void Clear()
        {
            _vertices.Clear();
            _colors.Clear();
            _uv.Clear();
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
