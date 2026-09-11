using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Top.Client.Core;
using Top.Client.Models.Textures;
using Top.Content;
using Top.Contracts.Assets.Maps;
using Top.Contracts.Tables.World;
using UnityEngine;

namespace Top.Client.Game.World.Terrain
{
    public class TerrainMaterial : IDisposable
    {
        private static readonly int TexturesId = Shader.PropertyToID("_Textures");
        private static readonly int MasksId = Shader.PropertyToID("_Masks");

        private readonly Texture2DArray _textureArray;
        private readonly Texture2D _masks;

        private TerrainMaterial(Texture2DArray textureArray, Texture2D masks, Shader shader)
        {
            _textureArray = textureArray;
            _masks = masks;

            Material = new Material(shader);
            Material.SetTexture(TexturesId, textureArray);
            Material.SetTexture(MasksId, masks);
        }

        public Material Material { get; }

        public static async Task<TerrainMaterial> Load(IContentSource contentSource, TerrainTable terrainTable,
            Shader shader, CancellationToken cancellationToken = default)
        {
            var reader = new TextureReader(contentSource);
            var textureArray = await reader.ReadArray(GetTexturePaths(terrainTable), cancellationToken);
            var masks = await ReadMasks(reader);

            return new TerrainMaterial(textureArray, masks, shader);
        }

        public void Dispose()
        {
            UnityObjects.Destroy(Material);
            UnityObjects.Destroy(_textureArray);
            UnityObjects.Destroy(_masks);
        }

        private static string[] GetTexturePaths(TerrainTable terrains)
        {
            var paths = new string[terrains.Select(terrain => terrain.Id).Prepend(0).Max() + 1];

            foreach (var terrain in terrains)
            {
                if (terrain.Id > 0 && !string.IsNullOrEmpty(terrain.TexturePath))
                {
                    paths[terrain.Id] = terrain.TexturePath;
                }
            }

            return paths;
        }

        private static async Task<Texture2D> ReadMasks(TextureReader reader)
        {
            var masks = await reader.Read(MapTexturePaths.MaskAtlas) ?? reader.CreateWhiteTexture();

            masks.wrapMode = TextureWrapMode.Clamp;
            masks.filterMode = FilterMode.Bilinear;

            return masks;
        }
    }
}
