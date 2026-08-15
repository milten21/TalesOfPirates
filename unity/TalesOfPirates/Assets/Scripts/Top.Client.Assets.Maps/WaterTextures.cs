using System;
using Top.Client.Core;
using Top.Contracts.Assets.Maps;
using UnityEngine;

namespace Top.Client.Assets.Maps
{
    /// <summary>
    /// The water loop as an array of frame slices and the material animating them.
    /// </summary>
    public class WaterTextures : IDisposable
    {
        public const string ShaderName = "Top/Water";

        private static readonly int FramesId = Shader.PropertyToID("_Frames");
        private static readonly int FrameCountId = Shader.PropertyToID("_FrameCount");

        public WaterTextures(Texture2DArray frames)
        {
            Frames = frames;
            Material = new Material(Shader.Find(ShaderName));
            Material.SetTexture(FramesId, frames);
            Material.SetFloat(FrameCountId, MapTexturePaths.WaterFrames);
        }

        public Texture2DArray Frames { get; }

        public Material Material { get; }

        public void Dispose()
        {
            UnityObjects.Destroy(Material);
            UnityObjects.Destroy(Frames);
        }
    }
}
