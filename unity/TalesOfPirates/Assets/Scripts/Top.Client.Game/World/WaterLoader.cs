using System.Collections.Generic;
using Top.Client.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Top.Client.Game.World
{
    public class WaterLoader : IChunkLoader
    {
        private const int QuadTiles = 4;
        private const float FadeDepth = -0.5f;

        private static readonly Color32 WaterColor = new Color32(140, 140, 220, 207);
        private static readonly Color32 FadedColor = new Color32(255, 255, 255, 0);

        private readonly MapData _mapData;
        private readonly Transform _parent;
        private readonly Material _material;
        private readonly Dictionary<Vector2Int, GameObject> _loadedChunks = new Dictionary<Vector2Int, GameObject>();

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<Vector2> _uv = new List<Vector2>();
        private readonly List<int> _triangles = new List<int>();

        public WaterLoader(MapData mapData, Transform parent, Material material)
        {
            _mapData = mapData;
            _parent = parent;
            _material = material;
        }

        public void Load(Vector2Int chunk)
        {
            var mesh = BuildMesh(chunk);

            if (mesh == null)
            {
                return;
            }

            var chunkSize = _mapData.ChunkSize;
            var chunkObject = new GameObject($"water_{chunk.x}_{chunk.y}");

            chunkObject.transform.SetParent(_parent, worldPositionStays: false);
            chunkObject.transform.localPosition = MapSpace.ToWorld(chunk.x * chunkSize, chunk.y * chunkSize, 0f);

            var filter = chunkObject.AddComponent<MeshFilter>();
            var renderer = chunkObject.AddComponent<MeshRenderer>();

            filter.sharedMesh = mesh;
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

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
            var quadsPerEdge = _mapData.ChunkSize / QuadTiles;
            var verticesPerRow = quadsPerEdge + 1;
            var visible = false;

            Clear();

            for (var y = 0; y <= quadsPerEdge; y++)
            {
                for (var x = 0; x <= quadsPerEdge; x++)
                {
                    var mapX = ((chunk.x * quadsPerEdge) + x) * QuadTiles;
                    var mapY = ((chunk.y * quadsPerEdge) + y) * QuadTiles;
                    var color = VertexColorAt(mapX, mapY);

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

            for (var y = 0; y < quadsPerEdge; y++)
            {
                for (var x = 0; x < quadsPerEdge; x++)
                {
                    var firstVertex = (y * verticesPerRow) + x;

                    _triangles.Add(firstVertex);
                    _triangles.Add(firstVertex + 1);
                    _triangles.Add(firstVertex + verticesPerRow);
                    _triangles.Add(firstVertex + 1);
                    _triangles.Add(firstVertex + verticesPerRow + 1);
                    _triangles.Add(firstVertex + verticesPerRow);
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

        private Color32 VertexColorAt(int mapX, int mapY)
        {
            return _mapData.HeightAt(mapX, mapY) > FadeDepth ? FadedColor : WaterColor;
        }

        private void Clear()
        {
            _vertices.Clear();
            _colors.Clear();
            _uv.Clear();
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
