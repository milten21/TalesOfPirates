using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Top.Client.Cli.Core;
using Top.Client.Messages;
using Top.Logging;

namespace Top.Client.Cli;

public class LiveListener
{
    public const int DefaultPort = 7973;

    private const int MaximumUnreadCount = 10_000;

    private const string StepPath = "/step";

    private const string EventsPath = "/events";

    private const string Help = $"POST {StepPath} with one step line, or GET {EventsPath}";

    private readonly StepExecutor _executor;

    private readonly string _address;

    private readonly TextWriter _output;

    private readonly HttpListener _listener = new();

    private readonly SemaphoreSlim _stepLock = new(1, 1);

    private readonly ConcurrentQueue<IServerEvent> _unreadEvents = new();

    private readonly CancellationTokenSource _endSource = new();

    private bool _hasEnded;

    public LiveListener(StepExecutor executor, int port, TextWriter output)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _output = output ?? throw new ArgumentNullException(nameof(output));

        _address = string.Create(CultureInfo.InvariantCulture, $"http://localhost:{port}/");

        _listener.Prefixes.Add(_address);
        _executor.EventArrived += KeepUnread;
    }

    public async Task<int> RunAsync()
    {
        try
        {
            _listener.Start();
        }
        catch (HttpListenerException failure)
        {
            _output.WriteLine($"cannot listen on {_address}: {failure.Message}");

            return ExitCode.ListenFailed;
        }

        _output.WriteLine($"listening on {_address}: {Help}");

        var requests = new List<Task>();

        try
        {
            while (true)
            {
                HttpListenerContext context;

                try
                {
                    context = await _listener.GetContextAsync().WaitAsync(_endSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                requests.RemoveAll(request => request.IsCompleted);
                requests.Add(Task.Run(() => AnswerAsync(context)));
            }

            await Task.WhenAll(requests);
        }
        finally
        {
            _executor.EventArrived -= KeepUnread;
            _listener.Close();
        }

        return ExitCode.Success;
    }

    private async Task AnswerAsync(HttpListenerContext context)
    {
        var request = context.Request;
        HttpStatusCode status;
        string text;

        try
        {
            (status, text) = (request.HttpMethod, request.Url?.AbsolutePath) switch
            {
                ("POST", StepPath) => await RunStepAsync(request),
                ("GET", EventsPath) => (HttpStatusCode.OK, ReadUnreadEvents()),
                _ => (HttpStatusCode.NotFound, $"send {Help}\n"),
            };
        }
        catch (Exception failure)
        {
            Log.Error($"the live request {request.HttpMethod} {request.Url?.AbsolutePath} failed", failure);

            (status, text) = (HttpStatusCode.InternalServerError, $"{StepResult.Failed(failure.Message)}\n");
        }

        await WriteResponseAsync(context.Response, status, text);
    }

    private async Task<(HttpStatusCode Status, string Text)> RunStepAsync(HttpListenerRequest request)
    {
        string line;

        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
        {
            line = (await reader.ReadToEndAsync()).Trim();
        }

        await _stepLock.WaitAsync();

        try
        {
            var result = RunStep(line);

            _output.WriteLine($"{line}: {result}");

            if (result.IsEndOfRun)
            {
                _hasEnded = true;
                await _endSource.CancelAsync();
            }

            return (result.IsDone ? HttpStatusCode.OK : HttpStatusCode.UnprocessableContent, $"{result}\n");
        }
        finally
        {
            _stepLock.Release();
        }
    }

    private StepResult RunStep(string line)
    {
        if (_hasEnded)
        {
            return StepResult.Failed("the run has ended");
        }

        if (line.Contains('\n', StringComparison.Ordinal))
        {
            return StepResult.Failed("the request body must be one step line");
        }

        return _executor.TryParse(line, out var step, out var error) ? _executor.Run(step) : StepResult.Failed(error);
    }

    private string ReadUnreadEvents()
    {
        if (_stepLock.Wait(0))
        {
            try
            {
                _executor.Drain();
            }
            finally
            {
                _stepLock.Release();
            }
        }

        var text = new StringBuilder();

        while (_unreadEvents.TryDequeue(out var serverEvent))
        {
            text.Append(ValueText.FromEvent(serverEvent)).Append('\n');
        }

        return text.ToString();
    }

    private void KeepUnread(IServerEvent serverEvent)
    {
        _unreadEvents.Enqueue(serverEvent);

        while (_unreadEvents.Count > MaximumUnreadCount)
        {
            _unreadEvents.TryDequeue(out _);
        }
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, HttpStatusCode status, string text)
    {
        var body = Encoding.UTF8.GetBytes(text);

        try
        {
            response.StatusCode = (int)status;
            response.ContentType = "text/plain; charset=utf-8";
            response.ContentLength64 = body.Length;

            await response.OutputStream.WriteAsync(body);
            response.Close();
        }
        catch (Exception failure) when (failure is HttpListenerException or IOException or ObjectDisposedException)
        {
            Log.Warning($"the live response could not be sent: {failure.Message}");
        }
    }
}
