using System.Diagnostics.CodeAnalysis;
using Godot;
using Top.Client.UI;

namespace Top.Client.Features.MainMenu.Servers;

public partial class ServerForm : ExpandContainer
{
    [Signal]
    public delegate void ChangedEventHandler();

    [Signal]
    public delegate void SubmittedEventHandler();

    [Export] public Label TitleLabel { get; set; } = null!;

    [Export] public LineEdit NameEdit { get; set; } = null!;

    [Export] public LineEdit HostEdit { get; set; } = null!;

    [Export] public LineEdit PortEdit { get; set; } = null!;

    public ServerEntry? EditedEntry { get; private set; }

    public bool IsValid => TryRead(out _, out _, out _);

    public override void _Ready()
    {
        base._Ready();

        foreach (LineEdit edit in new[] { NameEdit, HostEdit, PortEdit })
        {
            edit.TextChanged += _ => EmitSignal(SignalName.Changed);
            edit.TextSubmitted += _ => EmitSignal(SignalName.Submitted);
        }

        NameEdit.FocusNeighborBottom = NameEdit.GetPathTo(HostEdit);
        NameEdit.FocusNext = NameEdit.GetPathTo(HostEdit);
        HostEdit.FocusNeighborTop = HostEdit.GetPathTo(NameEdit);
        HostEdit.FocusNeighborRight = HostEdit.GetPathTo(PortEdit);
        HostEdit.FocusPrevious = HostEdit.GetPathTo(NameEdit);
        HostEdit.FocusNext = HostEdit.GetPathTo(PortEdit);
        PortEdit.FocusNeighborTop = PortEdit.GetPathTo(NameEdit);
        PortEdit.FocusNeighborLeft = PortEdit.GetPathTo(HostEdit);
        PortEdit.FocusPrevious = PortEdit.GetPathTo(HostEdit);
    }

    public void OpenToAdd()
    {
        Open(null, "Add server", string.Empty, string.Empty, ServerEntry.DefaultPort.ToString());
    }

    public void OpenToEdit(ServerEntry entry)
    {
        Open(entry, "Edit server", entry.Name, entry.Host, entry.Port.ToString());
    }

    public void Close()
    {
        EditedEntry = null;

        Collapse();
    }

    public bool TryRead([NotNullWhen(true)] out string? name, [NotNullWhen(true)] out string? host, out int port)
    {
        return ServerEntry.TryParse(NameEdit.Text, HostEdit.Text, PortEdit.Text, out name, out host, out port);
    }

    private void Open(ServerEntry? entry, string title, string name, string host, string port)
    {
        EditedEntry = entry;

        TitleLabel.Text = title;
        NameEdit.Text = name;
        HostEdit.Text = host;
        PortEdit.Text = port;

        Expand();

        NameEdit.Edit();

        EmitSignal(SignalName.Changed);
    }
}
