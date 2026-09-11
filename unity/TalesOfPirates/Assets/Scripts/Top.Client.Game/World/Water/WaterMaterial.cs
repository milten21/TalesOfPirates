using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Top.Client.Core;
using Top.Client.Models.Textures;
using Top.Content;
using Top.Contracts.Assets.Maps;
using UnityEngine;

namespace Top.Client.Game.World.Water
{
    public class WaterMaterial : IDisposable
    {
        private static readonly int FramesId = Shader.PropertyToID("_Frames");
        private static readonly int FrameCountId = Shader.PropertyToID("_FrameCount");

        private readonly Texture2DArray _textureFrames;

        private WaterMaterial(Texture2DArray textureFrames, Shader shader)
        {
            _textureFrames = textureFrames;

            Material = new Material(shader);
            Material.SetTexture(FramesId, textureFrames);
            Material.SetFloat(FrameCountId, MapTexturePaths.WaterFrameCount);
        }

        public Material Material { get; }

        public static async Task<WaterMaterial> Load(IContentSource contentSource, Shader shader,
            CancellationToken cancellationToken = default)
        {
            var paths = Enumerable
                .Range(0, MapTexturePaths.WaterFrameCount)
                .Select(MapTexturePaths.GetWaterFramePath)
                .ToArray();

            var frames = await new TextureReader(contentSource).ReadArray(paths, cancellationToken);

            return new WaterMaterial(frames, shader);
        }

        public void Dispose()
        {
            UnityObjects.Destroy(Material);
            UnityObjects.Destroy(_textureFrames);
        }
    }
}
