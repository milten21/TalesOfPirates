using System;

namespace Top.Client.Messages.Play.Data
{
    [Flags]
    public enum MoveStates
    {
        None = 0,
        Arrive = 1,
        Block = 2,
        Cancel = 4,
        InRange = 8,
        NoTarget = 16,
        CannotMove = 32,
    }
}
