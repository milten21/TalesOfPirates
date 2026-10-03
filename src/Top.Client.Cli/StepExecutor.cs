using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Top.Client.Cli.Core;
using Top.Client.Cli.Features.Chat;
using Top.Client.Cli.Features.Chat.Steps;
using Top.Client.Cli.Features.Inventory.Steps;
using Top.Client.Cli.Features.Party;
using Top.Client.Cli.Features.Party.Steps;
using Top.Client.Cli.Features.Play;
using Top.Client.Cli.Features.Play.Steps;
using Top.Client.Cli.Features.Waits;
using Top.Client.Cli.Features.Waits.Steps;
using Top.Client.Messages;

namespace Top.Client.Cli;

public class StepExecutor : IDisposable
{
    private readonly ScenarioSession _session;

    private readonly StepRegistry _registry = CreateStepRegistry();

    public StepExecutor(IServerConnection connection, ServerAccount account, TimeSpan sayInterval)
    {
        _session = new ScenarioSession(connection, account);

        _session.AddState(new PlayState(_session));
        _session.AddState(new SightState(_session));
        _session.AddState(new PartyInviteState(_session));
        _session.AddState(new PartyState(_session));
        _session.AddState(new ChatState(sayInterval));
    }

    public event Action<IServerEvent>? EventArrived
    {
        add => _session.EventArrived += value;
        remove => _session.EventArrived -= value;
    }

    public static void WriteStepHelp(TextWriter output)
    {
        if (output == null)
        {
            throw new ArgumentNullException(nameof(output));
        }

        var defaultTimeLimit = ValueText.FromSeconds(ScenarioSession.DefaultTimeLimit);

        output.WriteLine("Steps. Positions are in meters, times in seconds. A [timeout] must be more than 0.");
        output.WriteLine($"A step with no [timeout] waits {defaultTimeLimit} at most.");
        output.WriteLine();
        CreateStepRegistry().WriteHelp(output);
    }

    public bool TryParse(string line, [NotNullWhen(true)] out IStep? step,
        [NotNullWhen(false)] out string? error)
    {
        try
        {
            step = _registry.Parse(line);
            error = null;

            return true;
        }
        catch (StepFailedException failure)
        {
            step = null;
            error = failure.Message;

            return false;
        }
    }

    public StepResult Run(IStep step)
    {
        try
        {
            _session.Drain();

            return step.Run(_session);
        }
        catch (StepFailedException failure)
        {
            return StepResult.Failed(failure.Message);
        }
    }

    public void Drain()
    {
        _session.Drain();
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    private static StepRegistry CreateStepRegistry()
    {
        var registry = new StepRegistry();

        registry.Add<PlayStep>();
        registry.Add<MoveStep>();
        registry.Add<WaitActorStep>();
        registry.Add<WaitEventStep>();
        registry.Add<SleepStep>();
        registry.Add<AcceptStep>();
        registry.Add<DeclineStep>();
        registry.Add<InviteStep>();
        registry.Add<LeaveStep>();
        registry.Add<KickStep>();
        registry.Add<SayStep>();
        registry.Add<UseStep>();
        registry.Add<QuitStep>();

        return registry;
    }
}
