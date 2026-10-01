using Godot;

namespace Top.Client.Rendering.Models.Gltf;

public static class TopImportMarker
{
    private const string MetaName = "top_gltf_extensions";

    private static readonly string PropertyName = $"metadata/{MetaName}";

    public static void Stamp(Node root)
    {
        root.SetMeta(MetaName, true);
    }

    public static bool IsStamped(PackedScene scene)
    {
        var state = scene.GetState();

        if (state.GetNodeCount() == 0)
        {
            return false;
        }

        for (var i = 0; i < state.GetNodePropertyCount(0); i++)
        {
            if (state.GetNodePropertyName(0, i) == PropertyName)
            {
                return true;
            }
        }

        return false;
    }
}
