using Godot;

namespace Top.Client.UI.Cursors;

public readonly record struct CursorPose(int FrameIndex, Vector2I Hotspot, int MillisecondsToNextStep);
