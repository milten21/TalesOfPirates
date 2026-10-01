using System.Collections.Generic;
using System.Linq;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace Top.Client.EngineTests.Test.Rendering.Models;

[TestSuite]
[RequireGodotRuntime]
public class ModelFactoryTests
{
    private const string BaseMap = "base_map";

    private const string FlipbookModel = "models/scene/cc-bd044.glb";

    [TestCase(FlipbookModel)]
    [TestCase("models/scene/bl-bd001.glb")]
    [TestCase("models/character/0000000000.glb")]
    public void Material_textures_have_mipmaps(string contentPath)
    {
        var model = AutoFree(TestContent.ServerContent.ModelFactory.Instantiate(contentPath)!)!;

        var textures = ReadBaseMaps(model).ToList();

        AssertThat(textures).IsNotEmpty();

        foreach (var texture in textures)
        {
            AssertThat(texture.GetImage().HasMipmaps()).IsTrue();
        }
    }

    [TestCase]
    public void Flipbook_frames_share_base_color_textures_with_mipmaps()
    {
        var model = AutoFree(TestContent.ServerContent.ModelFactory.Instantiate(FlipbookModel)!)!;

        var player = model.GetNode<AnimationPlayer>("MaterialAnimationPlayer");
        var animation = player.GetAnimation("materials");
        var track = Enumerable.Range(0, animation.GetTrackCount())
            .First(index => animation.TrackGetPath(index).ToString().EndsWith($"shader_parameter/{BaseMap}"));

        var frames = Enumerable.Range(0, animation.TrackGetKeyCount(track))
            .Select(key => animation.TrackGetKeyValue(track, key).As<Texture2D>())
            .ToList();

        AssertThat(ReadBaseMaps(model).Any(texture => ReferenceEquals(texture, frames[0]))).IsTrue();

        foreach (var frame in frames)
        {
            AssertThat(frame.GetImage().HasMipmaps()).IsTrue();
        }
    }

    private static IEnumerable<Texture2D> ReadBaseMaps(Node node)
    {
        if (node is MeshInstance3D { Mesh: { } mesh })
        {
            for (var surface = 0; surface < mesh.GetSurfaceCount(); surface++)
            {
                if (mesh.SurfaceGetMaterial(surface) is ShaderMaterial material
                    && material.GetShaderParameter(BaseMap).As<Texture2D>() is { } texture)
                {
                    yield return texture;
                }
            }
        }

        foreach (var child in node.GetChildren())
        {
            foreach (var texture in ReadBaseMaps(child))
            {
                yield return texture;
            }
        }
    }
}
