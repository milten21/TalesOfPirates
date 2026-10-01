using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace Top.Client.Rendering.Textures;

public class TextureCache(TextureReader textureReader)
{
    private readonly Dictionary<string, Task<Texture2D?>> _cache = new Dictionary<string, Task<Texture2D?>>();

    public Task<Texture2D?> Read(string path)
    {
        if (_cache.TryGetValue(path, out var known) && !known.IsFaulted)
        {
            return known;
        }

        var reading = Load(path);

        _cache[path] = reading;

        return reading;
    }

    private async Task<Texture2D?> Load(string path)
    {
        var image = await textureReader.Read(path);

        return image != null ? ImageTexture.CreateFromImage(image) : null;
    }
}
