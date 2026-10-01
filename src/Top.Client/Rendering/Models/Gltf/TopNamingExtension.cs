using Godot;
using Top.Contracts.Assets.Models;

namespace Top.Client.Rendering.Models.Gltf;

[Tool]
public partial class TopNamingExtension : GltfDocumentExtension
{
    public override Error _ImportPostParse(GltfState state)
    {
        foreach (var node in state.Nodes)
        {
            var name = node.OriginalName;

            if (Naming.IsHelper(name) || Naming.IsLitShell(name))
            {
                node.Visible = false;
            }
        }

        return Error.Ok;
    }
}
