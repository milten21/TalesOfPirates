using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Top.Client.Rendering.Textures;
using Top.Content;
using Top.Contracts.Assets.Maps;
using Top.Contracts.Tables.World;

namespace Top.Client.Features.Maps.Terrain;

public static class TerrainMaterialReader
{
    private const string ShaderPath = "res://Features/Maps/Terrain/Assets/Terrain.gdshader";

    public static async Task<ShaderMaterial> Read(IContentSource contentSource, TerrainTable terrainTable,
        CancellationToken cancellationToken = default)
    {
        var textureReader = new TextureReader(contentSource);
        var terrainTextures = await new TextureArrayReader(textureReader)
            .Read(SelectTexturePaths(terrainTable), cancellationToken);
        var maskAtlas = await ReadMaskAtlas(textureReader);
        var material = new ShaderMaterial { Shader = GD.Load<Shader>(ShaderPath) };

        material.SetShaderParameter("terrain_textures", terrainTextures);
        material.SetShaderParameter("mask_atlas", maskAtlas);

        return material;
    }

    private static string[] SelectTexturePaths(TerrainTable terrains)
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

    private static async Task<Texture2D> ReadMaskAtlas(TextureReader textureReader)
    {
        var image = await textureReader.Read(MapTexturePaths.MaskAtlas)
            ?? TextureReader.CreateWhiteImage(1);

        image.Convert(Image.Format.Rgba8);

        return ImageTexture.CreateFromImage(image);
    }
}
