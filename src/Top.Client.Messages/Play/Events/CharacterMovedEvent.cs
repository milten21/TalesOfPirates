using System;
using System.Collections.Generic;
using System.Numerics;
using Top.Client.Messages.Play.Data;

namespace Top.Client.Messages.Play.Events
{
    public class CharacterMovedEvent : IServerEvent
    {
        public CharacterMovedEvent(uint worldId, IReadOnlyList<Vector2> points, MoveStates moveState,
            int? stopState)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (points.Count == 0)
            {
                throw new ArgumentException("A move names at least the position the character reached.",
                    nameof(points));
            }

            WorldId = worldId;
            Points = points;
            MoveState = moveState;
            StopState = stopState;
        }

        public uint WorldId { get; }

        public IReadOnlyList<Vector2> Points { get; }

        public MoveStates MoveState { get; }

        public int? StopState { get; }
    }
}
