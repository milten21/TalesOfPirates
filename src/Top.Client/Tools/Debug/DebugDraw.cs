using System.Collections.Generic;
using Godot;

namespace Top.Client.Tools.Debug;

public partial class DebugDraw : Node3D
{
    private const int RingSegmentCount = 32;

    private const float LabelPixelSize = 0.0003f;

    private const int LabelFontSize = 32;

    private const int LabelOutlineSize = 8;

    private static readonly Vector2 LabelOffset = new Vector2(0f, -20f);

    private readonly ImmediateMesh _mesh = new ImmediateMesh();

    private readonly List<(Vector3 Position, Color Color)> _vertices = [];

    private readonly List<Label3D> _labels = [];

    private int _usedLabelCount;

    public DebugDraw()
    {
        Name = nameof(DebugDraw);

        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            NoDepthTest = true,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            VertexColorUseAsAlbedo = true,
            RenderPriority = (int)Material.RenderPriorityMax
        };

        AddChild(new MeshInstance3D
        {
            Mesh = _mesh,
            MaterialOverride = material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        });
    }

    public void Begin()
    {
        _vertices.Clear();

        for (var index = 0; index < _usedLabelCount; index++)
        {
            _labels[index].Hide();
        }

        _usedLabelCount = 0;
    }

    public void AddLine(Vector3 from, Vector3 to, Color color)
    {
        _vertices.Add((from, color));
        _vertices.Add((to, color));
    }

    public void AddPath(IReadOnlyList<Vector3> points, Color color)
    {
        for (var index = 1; index < points.Count; index++)
        {
            AddLine(points[index - 1], points[index], color);
        }
    }

    public void AddRing(Vector3 center, float radius, Color color)
    {
        var previous = center + new Vector3(radius, 0f, 0f);

        for (var segment = 1; segment <= RingSegmentCount; segment++)
        {
            var angle = Mathf.Tau * segment / RingSegmentCount;
            var next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            AddLine(previous, next, color);

            previous = next;
        }
    }

    public void AddText(Vector3 position, string text, Color color)
    {
        if (_usedLabelCount == _labels.Count)
        {
            var created = new Label3D
            {
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                FixedSize = true,
                VerticalAlignment = VerticalAlignment.Top,
                Offset = LabelOffset,
                NoDepthTest = true,
                Shaded = false,
                PixelSize = LabelPixelSize,
                FontSize = LabelFontSize,
                OutlineSize = LabelOutlineSize,
                RenderPriority = (int)Material.RenderPriorityMax,
                OutlineRenderPriority = (int)Material.RenderPriorityMax - 1
            };

            _labels.Add(created);

            AddChild(created);
        }

        var label = _labels[_usedLabelCount];

        label.Position = position;
        label.Text = text;
        label.Modulate = color;
        label.Show();

        _usedLabelCount++;
    }

    public void End()
    {
        _mesh.ClearSurfaces();

        if (_vertices.Count == 0)
        {
            return;
        }

        _mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

        foreach (var (position, color) in _vertices)
        {
            _mesh.SurfaceSetColor(color);
            _mesh.SurfaceAddVertex(position);
        }

        _mesh.SurfaceEnd();
    }
}
