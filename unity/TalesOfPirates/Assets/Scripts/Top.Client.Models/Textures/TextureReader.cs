using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Top.Client.Core;
using Top.Content;
using Top.Logging;
using UnityEngine;

namespace Top.Client.Models.Textures
{
    public class TextureReader
    {
        private readonly IContentSource _contentSource;

        public TextureReader(IContentSource contentSource)
        {
            _contentSource = contentSource;
        }

        public async Task<Texture2D> Read(string path)
        {
            if (!_contentSource.Exists(path))
            {
                Log.Warning($"no '{path}', drawing white");

                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);

            if (texture.LoadImage(await _contentSource.Read(path)))
            {
                return texture;
            }

            Log.Warning($"'{path}' is not an image, drawing white");
            UnityObjects.Destroy(texture);

            return null;
        }

        public async Task<Texture2DArray> ReadArray(IReadOnlyList<string> paths,
            CancellationToken cancellationToken = default)
        {
            var images = new Texture2D[paths.Count];
            var size = 1;

            for (var i = 0; i < images.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(paths[i]))
                {
                    continue;
                }

                images[i] = await Read(paths[i]);

                if (images[i] != null)
                {
                    size = Mathf.Max(size, images[i].width, images[i].height);
                }
            }

            var array = new Texture2DArray(size, size, images.Length, TextureFormat.RGBA32, mipChain: true)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
            };

            for (var i = 0; i < images.Length; i++)
            {
                array.SetPixels32(images[i] == null ? CreateWhitePixels(size) : Resize(images[i], size), i);
                UnityObjects.Destroy(images[i]);
            }

            array.Apply(updateMipmaps: true, makeNoLongerReadable: true);

            return array;
        }

        public Color32[] CreateWhitePixels(int size)
        {
            var pixels = new Color32[size * size];

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            return pixels;
        }

        public Texture2D CreateWhiteTexture()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);

            texture.SetPixels32(CreateWhitePixels(1));
            texture.Apply();

            return texture;
        }

        public Color32[] Resize(Texture2D texture, int size)
        {
            var source = texture.GetPixels32();
            var width = texture.width;
            var height = texture.height;

            if (width == size && height == size)
            {
                return source;
            }

            var pixels = new Color32[size * size];

            for (var y = 0; y < size; y++)
            {
                var sourceY = ((y + 0.5f) * height / size) - 0.5f;
                var y0 = Mathf.FloorToInt(sourceY);
                var fy = sourceY - y0;

                for (var x = 0; x < size; x++)
                {
                    var sourceX = ((x + 0.5f) * width / size) - 0.5f;
                    var x0 = Mathf.FloorToInt(sourceX);
                    var fx = sourceX - x0;

                    var top = Color32.Lerp(PixelAt(source, width, height, x0, y0),
                        PixelAt(source, width, height, x0 + 1, y0), fx);
                    var bottom = Color32.Lerp(PixelAt(source, width, height, x0, y0 + 1),
                        PixelAt(source, width, height, x0 + 1, y0 + 1), fx);

                    pixels[(y * size) + x] = Color32.Lerp(top, bottom, fy);
                }
            }

            return pixels;
        }

        private static Color32 PixelAt(Color32[] pixels, int width, int height, int x, int y)
        {
            return pixels[(Wrap(y, height) * width) + Wrap(x, width)];
        }

        private static int Wrap(int index, int count)
        {
            return ((index % count) + count) % count;
        }
    }
}
