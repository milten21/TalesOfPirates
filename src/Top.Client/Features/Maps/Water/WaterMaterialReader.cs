using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Top.Client.Rendering.Textures;
using Top.Content;
using Top.Contracts.Assets.Maps;

namespace Top.Client.Features.Maps.Water;

public static class WaterMaterialReader
{
    private const string ShaderPath = "res://Features/Maps/Water/Assets/Water.gdshader";

    public static async Task<ShaderMaterial> Read(IContentSource contentSource,
        CancellationToken cancellationToken = default)
    {
        var paths = Enumerable
            .Range(0, MapTexturePaths.WaterFrameCount)
            .Select(MapTexturePaths.GetWaterFramePath)
            .ToArray();

        var frames = await new TextureArrayReader(new TextureReader(contentSource))
            .Read(paths, cancellationToken);
        var material = new ShaderMaterial { Shader = GD.Load<Shader>(ShaderPath) };

        material.SetShaderParameter("frames", frames);
        material.SetShaderParameter("frame_count", MapTexturePaths.WaterFrameCount);

        return material;
    }
}
