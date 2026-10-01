using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace Top.Client.Rendering.Textures;

public class TextureArrayReader(TextureReader textureReader)
{
    public async Task<Texture2DArray> Read(IReadOnlyList<string> paths,
        CancellationToken cancellationToken = default)
    {
        var images = new Image?[paths.Count];
        var size = 1;

        for (var i = 0; i < images.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(paths[i]))
            {
                continue;
            }

            var image = await textureReader.Read(paths[i]);

            images[i] = image;

            if (image != null)
            {
                size = Mathf.Max(size, Mathf.Max(image.GetWidth(), image.GetHeight()));
            }
        }

        var layers = new Godot.Collections.Array<Image>();

        foreach (var image in images)
        {
            layers.Add(FitLayer(image, size));
        }

        var array = new Texture2DArray();

        array.CreateFromImages(layers);

        return array;
    }

    private static Image FitLayer(Image? image, int size)
    {
        var layer = image ?? TextureReader.CreateWhiteImage(size);

        layer.Convert(Image.Format.Rgba8);

        if (layer.GetWidth() != size || layer.GetHeight() != size)
        {
            layer.Resize(size, size, Image.Interpolation.Bilinear);
        }

        layer.GenerateMipmaps();

        return layer;
    }
}
