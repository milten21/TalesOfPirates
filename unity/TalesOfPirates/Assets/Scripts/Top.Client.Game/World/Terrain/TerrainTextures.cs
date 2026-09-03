using System;
using Top.Client.Core;
using UnityEngine;

namespace Top.Client.Game.World.Terrain
{
    /// <summary>
    /// The terrain textures as an array indexed by terrain id, the mask sheet,
    /// and the material blending them.
    /// </summary>
    public class TerrainTextures : IDisposable
    {
        private static readonly int TexturesId = Shader.PropertyToID("_Textures");
        private static readonly int MasksId = Shader.PropertyToID("_Masks");

        public TerrainTextures(Texture2DArray textures, Texture2D masks, Shader shader)
        {
            Textures = textures;
            Masks = masks;
            Material = new Material(shader);
            Material.SetTexture(TexturesId, textures);
            Material.SetTexture(MasksId, masks);
        }

        public Texture2DArray Textures { get; }

        public Texture2D Masks { get; }

        public Material Material { get; }

        public void Dispose()
        {
            UnityObjects.Destroy(Material);
            UnityObjects.Destroy(Textures);
            UnityObjects.Destroy(Masks);
        }
    }
}
