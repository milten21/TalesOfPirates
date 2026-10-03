using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Top.Client.Messages.Party.Events;
using Top.Client.Messages.Play.Commands;

namespace Top.Client.Cli.Tests;

public class LiveListenerTests : ScenarioTest
{
    private static readonly TimeSpan EndTimeLimit = TimeSpan.FromSeconds(5);

    private StepExecutor _executor;

    private HttpClient _client;

    private Task<int> _run;

    [SetUp]
    public void StartListener()
    {
        var port = FindFreePort();

        _executor = CreateExecutor();
        _client = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}/") };
        _run = new LiveListener(_executor, port, Output).RunAsync();
    }

    [TearDown]
    public async Task StopListener()
    {
        try
        {
            if (!_run.IsCompleted)
            {
                await SendStepAsync("quit");
            }

            await _run.WaitAsync(EndTimeLimit);
        }
        finally
        {
            _client.Dispose();
            _executor.Dispose();
        }
    }

    [Test]
    public async Task A_step_request_returns_the_result_of_the_step()
    {
        ReplyToLogIn("T1", "Observer");

        var (status, text) = await SendStepAsync("play Observer");

        Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(text, Is.EqualTo("done, Observer entered garner at (2100, 2780)\n"));
        Assert.That(Connection.Sent.OfType<BeginPlayCommand>().Single().CharacterIndex, Is.EqualTo(1));
        Assert.That(Output.ToString(), Does.Contain("play Observer: done, Observer entered garner at (2100, 2780)"));
    }

    [Test]
    public async Task A_wait_step_returns_the_event_that_the_server_sends()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
        {
            Connection.Queue(new PartyInvitedEvent(7, "T1"), afterDrains: 5);

            return [CreateEnteredMap("Observer", Vector2.Zero)];
        });

        await SendStepAsync("play Observer");
        var (status, text) = await SendStepAsync("wait-event PartyInvitedEvent 5");

        Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(text, Is.EqualTo("done, PartyInvitedEvent { InviterId = 7, InviterName = \"T1\" } arrived\n"));
    }

    [Test]
    public async Task A_wait_step_fails_at_its_time_limit()
    {
        ReplyToLogIn("Observer");

        await SendStepAsync("play Observer");
        var (status, text) = await SendStepAsync("wait-event PartyInvitedEvent 0.1");

        Assert.That(status, Is.EqualTo(HttpStatusCode.UnprocessableContent));
        Assert.That(text, Is.EqualTo("failed, no PartyInvitedEvent in 0.1 s\n"));
    }

    [Test]
    public async Task A_bad_step_returns_its_error_and_the_next_step_runs()
    {
        ReplyToLogIn("Observer");

        var unknown = await SendStepAsync("jump 3");
        var refused = await SendStepAsync("play Nobody");
        var empty = await SendStepAsync(" ");
        var twoLines = await SendStepAsync("sleep 0\nsleep 0");
        var done = await SendStepAsync("play Observer");

        Assert.That(unknown, Is.EqualTo((HttpStatusCode.UnprocessableContent, "failed, unknown step 'jump'\n")));
        Assert.That(refused, Is.EqualTo((HttpStatusCode.UnprocessableContent,
            "failed, the account godot has no character named Nobody, only: Observer\n")));
        Assert.That(empty, Is.EqualTo((HttpStatusCode.UnprocessableContent, "failed, the line has no step\n")));
        Assert.That(twoLines, Is.EqualTo((HttpStatusCode.UnprocessableContent,
            "failed, the request body must be one step line\n")));
        Assert.That(done.Status, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(_run.IsCompleted, Is.False);
    }

    [Test]
    public async Task The_events_endpoint_returns_the_events_that_arrived_since_its_last_read()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
            [CreateEnteredMap("Observer", Vector2.Zero), new PartyInvitedEvent(7, "T1")]);

        await SendStepAsync("play Observer");
        var first = await ReadEventsAsync();
        Connection.Queue(new PartyInvitedEvent(8, "T2"));
        var second = await ReadEventsAsync();
        var third = await ReadEventsAsync();

        Assert.That(first.Split('\n').Select(line => line.Split(' ')[0]),
            Is.EqualTo(new[] { "LoginSucceededEvent", "MapEnteredEvent", "PartyInvitedEvent", "" }));
        Assert.That(first, Does.Contain("\nPartyInvitedEvent { InviterId = 7, InviterName = \"T1\" }\n"));
        Assert.That(second, Is.EqualTo("PartyInvitedEvent { InviterId = 8, InviterName = \"T2\" }\n"));
        Assert.That(third, Is.Empty);
    }

    [Test]
    public async Task Quit_returns_its_result_and_ends_the_run_with_a_zero_exit_code()
    {
        ReplyToLogIn("Observer");

        await SendStepAsync("play Observer");
        var (status, text) = await SendStepAsync("quit");

        Assert.That(status, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(text, Is.EqualTo("done, the connection is closed\n"));
        Assert.That(await _run.WaitAsync(EndTimeLimit), Is.EqualTo(ExitCode.Success));
        Assert.That(Connection.IsOpen, Is.False);
    }

    [Test]
    public async Task Another_path_is_not_found()
    {
        var response = await _client.GetAsync("step");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await response.Content.ReadAsStringAsync(),
            Is.EqualTo("send POST /step with one step line, or GET /events\n"));
    }

    private async Task<(HttpStatusCode Status, string Text)> SendStepAsync(string line)
    {
        using var response = await _client.PostAsync("step", new StringContent(line, Encoding.UTF8, "text/plain"));

        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private async Task<string> ReadEventsAsync()
    {
        using var response = await _client.GetAsync("events");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        return await response.Content.ReadAsStringAsync();
    }

    private static int FindFreePort()
    {
        var socket = new TcpListener(IPAddress.Loopback, 0);

        socket.Start();

        try
        {
            return ((IPEndPoint)socket.LocalEndpoint).Port;
        }
        finally
        {
            socket.Stop();
        }
    }
}
