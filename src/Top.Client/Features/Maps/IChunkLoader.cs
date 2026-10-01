using Godot;

namespace Top.Client.Features.Maps;

public interface IChunkLoader
{
    void Load(Vector2I chunk);

    void Unload(Vector2I chunk);
}
