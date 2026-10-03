using System;
using System.Numerics;
using Top.Client.Cli.Core;
using Top.Client.Messages.Events;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;

namespace Top.Client.Cli.Features.Play;

public class PlayState
{
    public PlayState(ScenarioSession session)
    {
        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        session.Subscribe<CharacterMovedEvent>(FollowOwnMove);
        session.Subscribe<DisconnectedEvent>(_ => Leave());
    }

    public CharacterState? Character { get; private set; }

    public Vector2 Position { get; private set; }

    public CharacterState RequireCharacter()
    {
        return Character ?? throw new StepFailedException("no character is in the world, use play first");
    }

    public void Enter(CharacterState character)
    {
        Character = character;
        Position = character.Position;
    }

    public void Leave()
    {
        Character = null;
    }

    private void FollowOwnMove(CharacterMovedEvent moved)
    {
        if (moved.WorldId == Character?.WorldId)
        {
            Position = moved.Points[^1];
        }
    }
}
