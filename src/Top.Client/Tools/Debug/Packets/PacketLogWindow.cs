using System.Collections.Generic;
using Godot;
using Top.Client.LegacyBridge;

namespace Top.Client.Tools.Debug.Packets;

public partial class PacketLogWindow : DebugWindow
{
    private const double RefreshIntervalSeconds = 0.25;

    private const float FilterWidth = 160f;

    private const float DetailsHeight = 200f;

    private static readonly StringName FontName = "font";

    private readonly LineEdit _opcodeFilter = new LineEdit { PlaceholderText = "Opcode name or number" };

    private readonly CheckBox _showsIncoming = new CheckBox { Text = "In", ButtonPressed = true };

    private readonly CheckBox _showsOutgoing = new CheckBox { Text = "Out", ButtonPressed = true };

    private readonly Button _pause = new Button { Text = "Pause", ToggleMode = true };

    private readonly Button _clear = new Button { Text = "Clear" };

    private readonly PacketList _list = new PacketList();

    private readonly TextEdit _details = new TextEdit { Editable = false };

    private readonly List<ObservedPacket> _rows = [];

    private PacketLog _log = null!;

    private IReadOnlyList<ObservedPacket> _packets = [];

    private double _secondsSinceRefresh = RefreshIntervalSeconds;

    public static PacketLogWindow Create(PacketLog log)
    {
        var window = new PacketLogWindow { _log = log };

        window.Build();

        return window;
    }

    public override void _Process(double delta)
    {
        _secondsSinceRefresh += delta;

        if (_secondsSinceRefresh < RefreshIntervalSeconds)
        {
            return;
        }

        _secondsSinceRefresh = 0;

        if (_pause.ButtonPressed)
        {
            return;
        }

        _packets = _log.Snapshot();

        ShowRows();
    }

    private void Build()
    {
        Name = nameof(PacketLogWindow);
        Title = "Packet log";

        _opcodeFilter.CustomMinimumSize = new Vector2(FilterWidth, 0f);
        _opcodeFilter.TextChanged += _ => ShowRows();
        _showsIncoming.Toggled += _ => ShowRows();
        _showsOutgoing.Toggled += _ => ShowRows();
        _clear.Pressed += Clear;

        var controls = new HBoxContainer();

        controls.AddChild(_opcodeFilter);
        controls.AddChild(_showsIncoming);
        controls.AddChild(_showsOutgoing);
        controls.AddChild(_pause);
        controls.AddChild(_clear);

        _list.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _list.SelectionChanged += ShowDetails;

        _details.CustomMinimumSize = new Vector2(0f, DetailsHeight);
        _details.GetVScrollBar().CustomMinimumSize = new Vector2(ScrollBarWidth, 0f);
        _details.GetHScrollBar().CustomMinimumSize = new Vector2(0f, ScrollBarWidth);
        _details.AddThemeFontOverride(FontName, new SystemFont { FontNames = ["Consolas", "Courier New", "monospace"] });

        var split = new VSplitContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };

        split.AddChild(_list);
        split.AddChild(_details);

        var layout = new VBoxContainer();

        layout.AddChild(controls);
        layout.AddChild(split);

        AddContent(layout);
    }

    private void ShowRows()
    {
        var filter = _opcodeFilter.Text.Trim();

        _rows.Clear();

        foreach (var packet in _packets)
        {
            var showsDirection = packet.Direction == PacketDirection.Incoming
                ? _showsIncoming.ButtonPressed
                : _showsOutgoing.ButtonPressed;

            if (showsDirection && (filter.Length == 0 || PacketText.MatchesOpcode(packet, filter)))
            {
                _rows.Add(packet);
            }
        }

        _list.SetRows(_rows);
    }

    private void ShowDetails()
    {
        _details.Text = _list.Selected == null ? string.Empty : PacketText.Details(_list.Selected);
    }

    private void Clear()
    {
        _log.Clear();
        _packets = [];

        _list.Select(null);

        ShowRows();
    }
}
