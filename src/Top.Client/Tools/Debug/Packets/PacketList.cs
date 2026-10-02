using System;
using System.Collections.Generic;
using Godot;
using Top.Client.LegacyBridge;

namespace Top.Client.Tools.Debug.Packets;

public partial class PacketList : Control
{
    private const int WheelRows = 3;

    private const float CellPadding = 4f;

    private const float SelectionAlpha = 0.25f;

    private static readonly float[] ColumnWidths = [96f, 40f, 220f, 56f];

    private static readonly string[] Headers = ["Time", "Dir", "Opcode", "Size", "Messages"];

    private static readonly StringName FontName = "font";

    private static readonly StringName FontSizeName = "font_size";

    private static readonly StringName FontColor = "font_color";

    private static readonly StringName LabelType = "Label";

    private readonly VScrollBar _scrollBar = new VScrollBar { Step = 1, MaxValue = 0 };

    private readonly string[] _cells = new string[Headers.Length];

    private IReadOnlyList<ObservedPacket> _rows = [];

    [Signal] public delegate void SelectionChangedEventHandler();

    public ObservedPacket? Selected { get; private set; }

    public PacketList()
    {
        ClipContents = true;

        _scrollBar.CustomMinimumSize = new Vector2(DebugWindow.ScrollBarWidth, 0f);
        _scrollBar.ValueChanged += _ => QueueRedraw();

        AddChild(_scrollBar);
    }

    public override void _Ready()
    {
        _scrollBar.SetAnchorsAndOffsetsPreset(LayoutPreset.RightWide);
        _scrollBar.GrowHorizontal = GrowDirection.Begin;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            UpdateScrollBar();
        }
    }

    public void SetRows(IReadOnlyList<ObservedPacket> rows)
    {
        var wasAtEnd = _scrollBar.Value >= _scrollBar.MaxValue - _scrollBar.Page;

        _rows = rows;

        UpdateScrollBar();

        if (wasAtEnd)
        {
            _scrollBar.Value = _scrollBar.MaxValue;
        }

        QueueRedraw();
    }

    public void Select(ObservedPacket? packet)
    {
        if (packet == Selected)
        {
            return;
        }

        Selected = packet;

        QueueRedraw();

        EmitSignal(SignalName.SelectionChanged);
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton { Pressed: true } button)
        {
            return;
        }

        switch (button.ButtonIndex)
        {
            case MouseButton.WheelUp:
                _scrollBar.Value -= WheelRows;
                break;

            case MouseButton.WheelDown:
                _scrollBar.Value += WheelRows;
                break;

            case MouseButton.Left:
                Select(FindRow(button.Position.Y));
                break;

            default:
                return;
        }

        AcceptEvent();
    }

    public override void _Draw()
    {
        var font = GetThemeFont(FontName, LabelType);
        var fontSize = GetThemeFontSize(FontSizeName, LabelType);
        var color = GetThemeColor(FontColor, LabelType);
        var rowHeight = MeasureRowHeight();
        var rowWidth = Size.X - _scrollBar.Size.X;

        DrawCells(Headers, 0f, font, fontSize, color);

        var firstRow = (int)_scrollBar.Value;
        var lastRow = Math.Min(firstRow + CountVisibleRows(), _rows.Count);

        for (var index = firstRow; index < lastRow; index++)
        {
            var packet = _rows[index];
            var top = (index - firstRow + 1) * rowHeight;

            if (packet == Selected)
            {
                DrawRect(new Rect2(0f, top, rowWidth, rowHeight), color with { A = color.A * SelectionAlpha });
            }

            _cells[0] = PacketText.Time(packet);
            _cells[1] = PacketText.Direction(packet);
            _cells[2] = PacketText.Opcode(packet);
            _cells[3] = PacketText.Size(packet);
            _cells[4] = PacketText.Messages(packet);

            DrawCells(_cells, top, font, fontSize, color);
        }
    }

    private void DrawCells(string[] cells, float top, Font font, int fontSize, Color color)
    {
        var baseline = top + CellPadding + font.GetAscent(fontSize);
        var left = CellPadding;

        for (var column = 0; column < cells.Length; column++)
        {
            DrawString(font, new Vector2(left, baseline), cells[column], fontSize: fontSize, modulate: color);

            if (column < ColumnWidths.Length)
            {
                left += ColumnWidths[column];
            }
        }
    }

    private ObservedPacket? FindRow(float y)
    {
        var index = (int)_scrollBar.Value + (int)(y / MeasureRowHeight()) - 1;

        return index >= (int)_scrollBar.Value && index < _rows.Count ? _rows[index] : null;
    }

    private void UpdateScrollBar()
    {
        _scrollBar.MaxValue = _rows.Count;
        _scrollBar.Page = CountVisibleRows();
    }

    private int CountVisibleRows()
    {
        return Math.Max(0, (int)(Size.Y / MeasureRowHeight()) - 1);
    }

    private float MeasureRowHeight()
    {
        return GetThemeFont(FontName, LabelType).GetHeight(GetThemeFontSize(FontSizeName, LabelType))
               + (2 * CellPadding);
    }
}
