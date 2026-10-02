using System.Collections.Generic;
using Godot;
using Top.Client.Features.Maps;

namespace Top.Client.Tests.Features.Maps;

public class RecordingChunkLoader : IChunkLoader
{
    public List<Vector2I> Loads { get; } = [];

    public List<Vector2I> Unloads { get; } = [];

    public void Load(Vector2I chunk)
    {
        Loads.Add(chunk);
    }

    public void Unload(Vector2I chunk)
    {
        Unloads.Add(chunk);
    }

    public void Forget()
    {
        Loads.Clear();
        Unloads.Clear();
    }
}
