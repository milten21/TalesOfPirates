using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Top.Client.Core;
using Top.Content.Packs;
using Top.Contracts.Assets.Maps;
using UnityEngine;

namespace Top.Client.Assets.Maps
{
    public class MapStore : IMapStore, IDisposable
    {
        private readonly IComposedContent _content;
        private readonly IReadOnlyList<string> _terrains;
        private readonly TextureReader _reader;

        private TerrainTextures _terrainTextures;
        private WaterTextures _waterTextures;

        public MapStore(IComposedContent content, IReadOnlyList<string> terrains)
        {
            _content = content;
            _terrains = terrains;
            _reader = new TextureReader(content);
        }

        public async Task<MapData> Load(string path, CancellationToken cancel = default)
        {
            var bytes = await _content.Read(path);

            cancel.ThrowIfCancellationRequested();

            using var stream = new MemoryStream(bytes);

            return new MapData(MapFile.Read(stream));
        }

        public void Dispose()
        {
            _terrainTextures?.Dispose();
            _terrainTextures = null;
            _waterTextures?.Dispose();
            _waterTextures = null;
        }

        public async Task<Material> GetTerrainMaterial(CancellationToken cancel = default)
        {
            _terrainTextures ??= new TerrainTextures(await BuildTerrainArray(cancel), await ReadMasks());

            return _terrainTextures.Material;
        }

        public async Task<Material> GetWaterMaterial(CancellationToken cancel = default)
        {
            _waterTextures ??= new WaterTextures(await BuildWaterArray(cancel));

            return _waterTextures.Material;
        }

        private async Task<Texture2DArray> BuildTerrainArray(CancellationToken cancel)
        {
            var images = new Texture2D[Mathf.Max(_terrains.Count, 1)];
            var size = 1;

            for (var i = 1; i < _terrains.Count; i++)
            {
                cancel.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(_terrains[i]))
                {
                    continue;
                }

                images[i] = await _reader.Read(_terrains[i]);

                if (images[i] != null)
                {
                    size = Mathf.Max(size, images[i].width, images[i].height);
                }
            }

            return BuildTextureArray(images, size);
        }

        private async Task<Texture2DArray> BuildWaterArray(CancellationToken cancel)
        {
            var images = new Texture2D[MapTexturePaths.WaterFrames];
            var size = 1;

            for (var frame = 0; frame < MapTexturePaths.WaterFrames; frame++)
            {
                cancel.ThrowIfCancellationRequested();

                images[frame] = await _reader.Read(MapTexturePaths.Water(frame));

                if (images[frame] != null)
                {
                    size = Mathf.Max(size, images[frame].width, images[frame].height);
                }
            }

            return BuildTextureArray(images, size);
        }

        private async Task<Texture2D> ReadMasks()
        {
            var masks = await _reader.Read(MapTexturePaths.Masks) ?? _reader.WhiteTexture();

            masks.wrapMode = TextureWrapMode.Clamp;
            masks.filterMode = FilterMode.Bilinear;

            return masks;
        }

        private Texture2DArray BuildTextureArray(Texture2D[] images, int size)
        {
            var array = new Texture2DArray(size, size, images.Length, TextureFormat.RGBA32, mipChain: true)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
            };

            for (var i = 0; i < images.Length; i++)
            {
                array.SetPixels32(images[i] == null ? _reader.WhitePixels(size) : _reader.Resize(images[i], size), i);
                UnityObjects.Destroy(images[i]);
            }

            array.Apply(updateMipmaps: true, makeNoLongerReadable: true);

            return array;
        }
    }
}
