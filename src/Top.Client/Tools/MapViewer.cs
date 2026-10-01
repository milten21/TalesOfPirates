using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Top.Client.Core;
using Top.Client.Features.Maps;
using Top.Logging;

namespace Top.Client.Tools;

public partial class MapViewer : Node3D
{
    private const float EyeHeight = 24f;

    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private ServerContent _content = null!;

    [Export] public int MapId { get; set; } = 1;

    [Export] public FlyCamera Camera { get; set; } = null!;

    [Export] public Map Map { get; set; } = null!;

    public void Setup(ServerContent content)
    {
        _content = content;
    }

    public override void _Ready()
    {
        Map.Setup(_content);

        EnterMap();
    }

    public override void _Process(double delta)
    {
        if (Camera != null)
        {
            Map.SetStreamingCenter(Camera.GlobalPosition);
        }
    }

    public override void _ExitTree()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    private async void EnterMap()
    {
        try
        {
            await LoadMap(_cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Log.Error($"map {MapId} broke while loading", exception);
        }
    }

    private async Task LoadMap(CancellationToken cancellationToken)
    {
        await Map.Load(lookup => lookup.FindById(MapId), cancellationToken: cancellationToken);

        PointCamera();

        Log.Info($"streaming {Map.Entry.DisplayName} ({Map.Entry.MapPath}) from {_content.ContentRoot}");
    }

    private void PointCamera()
    {
        if (Camera == null)
        {
            return;
        }

        var start = Map.Entry;
        var ground = Mathf.Max(Map.Grid.HeightAt(start.StartX, start.StartY), 0f);
        var eye = MapSpace.ToWorld(start.StartX, start.StartY, ground + EyeHeight);

        Camera.PointAt(eye, MapSpace.ToWorld(start.StartX + 20f, start.StartY + 20f, ground));
    }
}
