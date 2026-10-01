using System.Threading.Tasks;
using Godot;
using Top.Content;
using Top.Logging;

namespace Top.Client.Rendering.Textures;

public class TextureReader(IContentSource contentSource)
{
    public async Task<Image?> Read(string path)
    {
        if (!contentSource.Exists(path))
        {
            Log.Warning($"no '{path}'");

            return null;
        }

        var image = new Image();

        if (image.LoadPngFromBuffer(await contentSource.Read(path)) == Error.Ok)
        {
            return image;
        }

        Log.Warning($"'{path}' is not a png");

        return null;
    }

    public static Image CreateWhiteImage(int size)
    {
        var image = Image.CreateEmpty(size, size, useMipmaps: false, Image.Format.Rgba8);

        image.Fill(Colors.White);

        return image;
    }
}
