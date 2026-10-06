using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Top.Logging;

namespace Top.Client.UI.Cursors;

public class CursorSkin(string themeFolder)
{
    private const string MetadataFile = "cursors.json";

    private static readonly Input.CursorShape[] Shapes = Enum.GetValues<Input.CursorShape>();

    private readonly Dictionary<CursorKey, Image[]?> _frames = [];

    private readonly Dictionary<Input.CursorShape, (CursorKey Key, int FrameIndex)> _shown = [];

    private CursorKey? _replacementKey;

    private bool _isReplacingEveryShape;

    private CursorEntries? _entries;

    private int _scaleFactor;

    private long _elapsedMilliseconds;

    private int _generation;

    private bool _isWatchingWindow;

    public void Reload()
    {
        _generation++;
        _entries = null;
        _frames.Clear();
        _shown.Clear();
        _elapsedMilliseconds = 0;
        _scaleFactor = CursorEntries.ScaleFactorFor(GetWindowHeight());

        foreach (var shape in Shapes)
        {
            Input.SetCustomMouseCursor(null, shape);
        }

        WatchWindow();

        if (ReadEntries() is not { } entries)
        {
            return;
        }

        if (entries.Resolve(CursorKey.Normal) == null)
        {
            Log.Error($"the theme in {themeFolder} has no {CursorKey.Normal} cursor");

            return;
        }

        _entries = entries;

        Show();
    }

    public void Replace(CursorKey? key, bool isEveryShape)
    {
        if (key == _replacementKey && isEveryShape == _isReplacingEveryShape)
        {
            return;
        }

        _replacementKey = key;
        _isReplacingEveryShape = isEveryShape;

        if (_entries == null)
        {
            return;
        }

        _generation++;

        Show();
    }

    private static int GetWindowHeight()
    {
        return Engine.GetMainLoop() is SceneTree tree ? tree.Root.Size.Y : 0;
    }

    private Image[]? FramesFor(CursorKey key)
    {
        if (!_frames.TryGetValue(key, out var frames))
        {
            frames = CutFrames(_entries!.Resolve(key)!);
            _frames[key] = frames;
        }

        return frames;
    }

    private CursorKey? KeyShownFor(Input.CursorShape shape)
    {
        if (_replacementKey != null && (_isReplacingEveryShape || shape == Input.CursorShape.Arrow))
        {
            return _replacementKey;
        }

        return CursorKeyResolver.KeyFor(shape);
    }

    private Image[]? CutFrames(CursorEntry entry)
    {
        var imagePath = themeFolder + entry.Image;

        if (ResourceLoader.Load<Texture2D>(imagePath)?.GetImage() is not { } image)
        {
            Log.Error($"cursor image {imagePath} did not load");

            return null;
        }

        var frameCount = Math.Max(1, image.GetWidth() / Math.Max(1, entry.Frame.X));
        var frames = new Image[frameCount];

        for (var i = 0; i < frameCount; i++)
        {
            frames[i] = image.GetRegion(new Rect2I(new Vector2I(i * entry.Frame.X, 0), entry.Frame));

            if (_scaleFactor > 1)
            {
                frames[i].Resize(entry.Frame.X * _scaleFactor, entry.Frame.Y * _scaleFactor,
                    Image.Interpolation.Nearest);
            }
        }

        return frames;
    }

    private void WatchWindow()
    {
        if (_isWatchingWindow || Engine.GetMainLoop() is not SceneTree tree)
        {
            return;
        }

        _isWatchingWindow = true;

        tree.Root.SizeChanged += ReloadWhenScaleChanges;
    }

    private void ReloadWhenScaleChanges()
    {
        if (CursorEntries.ScaleFactorFor(GetWindowHeight()) != _scaleFactor)
        {
            Reload();
        }
    }

    private void Show()
    {
        var millisecondsToNextShow = 0;

        foreach (var shape in Shapes)
        {
            if (KeyShownFor(shape) is not { } key || FramesFor(key) is not { } frames)
            {
                ClearShape(shape);

                continue;
            }

            var pose = _entries!.PoseAt(key, frames.Length, _elapsedMilliseconds)!.Value;
            var frameIndex = Math.Min(pose.FrameIndex, frames.Length - 1);

            if (!_shown.TryGetValue(shape, out var shown) || shown.Key != key || shown.FrameIndex != frameIndex)
            {
                _shown[shape] = (key, frameIndex);

                Input.SetCustomMouseCursor(frames[frameIndex], shape, pose.Hotspot);
            }

            if (pose.MillisecondsToNextStep > 0)
            {
                millisecondsToNextShow = millisecondsToNextShow == 0
                    ? pose.MillisecondsToNextStep
                    : Math.Min(millisecondsToNextShow, pose.MillisecondsToNextStep);
            }
        }

        if (millisecondsToNextShow > 0)
        {
            ShowAfter(millisecondsToNextShow);
        }
    }

    private void ClearShape(Input.CursorShape shape)
    {
        if (_shown.Remove(shape))
        {
            Input.SetCustomMouseCursor(null, shape);
        }
    }

    private void ShowAfter(int milliseconds)
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            return;
        }

        var generation = _generation;

        tree.CreateTimer(milliseconds / 1000.0, true, false, true).Timeout += () =>
        {
            if (generation != _generation)
            {
                return;
            }

            _elapsedMilliseconds += milliseconds;

            Show();
        };
    }

    private CursorEntries? ReadEntries()
    {
        var path = themeFolder + MetadataFile;

        if (!FileAccess.FileExists(path))
        {
            Log.Error($"no cursor metadata at {path}");

            return null;
        }

        try
        {
            return CursorEntries.Read(FileAccess.GetFileAsString(path), _scaleFactor);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException
                                              or InvalidOperationException)
        {
            Log.Error($"cursor metadata at {path} did not read", exception);

            return null;
        }
    }
}
