using System;
using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Client.Cli.Core;
using Top.Client.Messages.Account.Commands;
using Top.Client.Messages.Account.Data;
using Top.Client.Messages.Account.Events;
using Top.Client.Messages.Inventory.Data;
using Top.Client.Messages.Play.Commands;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;

namespace Top.Client.Cli.Tests;

public abstract class ScenarioTest
{
    protected const uint PlayerId = 40;

    protected static readonly TimeSpan SayInterval = TimeSpan.FromSeconds(0.3);

    private static readonly ServerAccount Account = new("127.0.0.1", 1973, "godot", "secret");

    protected FakeServerConnection Connection { get; private set; }

    protected StringWriter Output { get; private set; }

    [SetUp]
    public void CreateConnection()
    {
        Connection = new FakeServerConnection();
        Output = new StringWriter();
    }

    protected int Run(params string[] lines)
    {
        using var executor = CreateExecutor();

        return ScenarioRunner.Run(string.Join("\n", lines), executor, Output);
    }

    protected StepExecutor CreateExecutor()
    {
        return new StepExecutor(Connection, Account, SayInterval);
    }

    protected void ReplyToLogIn(params string[] characterNames)
    {
        var characters = characterNames
            .Select(name => new CharacterSlot(name, "Newbie", 1, 1, new CharacterLook(0, [])));

        Connection.ReplyTo<LogInCommand>(_ => [new LoginSucceededEvent(characters, 3, hasSecurityCode: false)]);
        Connection.ReplyTo<BeginPlayCommand>(command =>
            [CreateEnteredMap(characterNames[command.CharacterIndex], new Vector2(2100f, 2780f))]);
    }

    protected static MapEnteredEvent CreateEnteredMap(string name, Vector2 position)
    {
        return new MapEnteredEvent("garner", EnterType.Edge, CreateCharacter(PlayerId, name, position),
            new BagSync(null, []), isBagLocked: false, []);
    }

    protected static CharacterAppearedEvent Appear(uint worldId, string name)
    {
        return new CharacterAppearedEvent(CreateCharacter(worldId, name, new Vector2(2134f, 2786f)), SeeType.New);
    }

    private static CharacterState CreateCharacter(uint worldId, string name, Vector2 position)
    {
        return new CharacterState(worldId, CharacterControlType.Player, 1, name, position, 0, 0, 0, 0, string.Empty,
            string.Empty, 0, 1, 100, 100, 100, 100, 0, 0, 0, 0, 0, 4.8f, null, null);
    }
}
