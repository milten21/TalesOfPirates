using System.Diagnostics.CodeAnalysis;
using Godot;
using Top.Client.Rendering.Models;
using Top.Contracts.Tables.World;

namespace Top.Client.Features.Maps.SceneObjects;

public class SceneObjectFactory(SceneObjectTable sceneObjectTable, ModelFactory modelFactory)
{
    public Node3D? Instantiate(int id)
    {
        return TryFindModelPath(id, out var modelPath) ? modelFactory.Instantiate(modelPath) : null;
    }

    public void Preload(int id)
    {
        if (TryFindModelPath(id, out var modelPath))
        {
            modelFactory.Preload(modelPath);
        }
    }

    private bool TryFindModelPath(int id, [NotNullWhen(true)] out string? modelPath)
    {
        modelPath = null;

        if (!sceneObjectTable.TryGetById(id, out var entry)
            || entry.Kind != SceneObjectKind.Model
            || string.IsNullOrEmpty(entry.ModelPath))
        {
            return false;
        }

        modelPath = entry.ModelPath;

        return true;
    }
}
