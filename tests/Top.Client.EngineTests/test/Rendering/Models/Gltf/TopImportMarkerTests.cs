using GdUnit4;
using Godot;
using Top.Client.Rendering.Models.Gltf;
using static GdUnit4.Assertions;

namespace Top.Client.EngineTests.Test.Rendering.Models.Gltf;

[TestSuite]
[RequireGodotRuntime]
public class TopImportMarkerTests
{
    [TestCase]
    public void Scene_without_stamp_is_not_stamped()
    {
        AssertThat(TopImportMarker.IsStamped(Pack(new Node3D()))).IsFalse();
    }

    [TestCase]
    public void Scene_packed_after_stamp_is_stamped()
    {
        var root = new Node3D();

        TopImportMarker.Stamp(root);

        AssertThat(TopImportMarker.IsStamped(Pack(root))).IsTrue();
    }

    [TestCase("models/scene/bl-bd001.glb")]
    [TestCase("models/character/0000000000.glb")]
    public void Models_loaded_through_the_extensions_are_stamped(string contentPath)
    {
        var model = AutoFree(TestContent.ServerContent.ModelFactory.Instantiate(contentPath)!)!;

        AssertThat(TopImportMarker.IsStamped(Pack(model))).IsTrue();
    }

    private static PackedScene Pack(Node root)
    {
        var scene = new PackedScene();

        scene.Pack(root);
        root.Free();

        return scene;
    }
}
