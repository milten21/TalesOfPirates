using UnityEngine;

namespace Top.Client.Game.World
{
    public interface IChunkLoader
    {
        void Load(Vector2Int chunk);

        void Unload(Vector2Int chunk);
    }
}
