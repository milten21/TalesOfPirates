using System;
using System.Collections.Generic;
using System.Numerics;

namespace Top.Client.Messages.Play.Commands
{
    public class MoveCommand : IServerCommand
    {
        public MoveCommand(uint worldId, IReadOnlyList<Vector2> path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.Count < 2)
            {
                throw new ArgumentException(
                    "A path runs from the character's position to at least one more point.", nameof(path));
            }

            WorldId = worldId;
            Path = path;
        }

        public uint WorldId { get; }

        public IReadOnlyList<Vector2> Path { get; }
    }
}
