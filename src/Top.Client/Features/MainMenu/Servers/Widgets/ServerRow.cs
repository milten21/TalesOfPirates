using System;
using Godot;
using Top.Client.UI;

namespace Top.Client.Features.MainMenu.Servers.Widgets;

public partial class ServerRow : ConfirmRow
{
    [Signal]
    public delegate void EditPressedEventHandler();

    [Signal]
    public delegate void RemovePressedEventHandler();

    [Export] public Label NameLabel { get; set; } = null!;

    [Export] public Label AddressLabel { get; set; } = null!;

    [Export] public Label DetailLabel { get; set; } = null!;

    [Export] public Button EditButton { get; set; } = null!;

    [Export] public Button RemoveButton { get; set; } = null!;

    public ServerEntry Entry { get; private set; } = null!;

    public void Setup(ServerEntry entry, ButtonGroup group, DateTime now)
    {
        Entry = entry;

        Row.ButtonGroup = group;

        NameLabel.Text = entry.Name;
        AddressLabel.Text = entry.Address;
        DetailLabel.Text = Describe(entry, now);
    }

    public override void _Ready()
    {
        base._Ready();

        EditButton.Pressed += () => EmitSignal(SignalName.EditPressed);
        RemoveButton.Pressed += () => EmitSignal(SignalName.RemovePressed);

        Row.FocusNeighborRight = Row.GetPathTo(EditButton);
        EditButton.FocusNeighborLeft = EditButton.GetPathTo(Row);
        EditButton.FocusNeighborRight = EditButton.GetPathTo(RemoveButton);
        RemoveButton.FocusNeighborLeft = RemoveButton.GetPathTo(EditButton);
    }

    public void AskToRemove()
    {
        Ask($"Remove {Entry.Name} from your list?");
    }

    private static string Describe(ServerEntry entry, DateTime now)
    {
        int count = entry.Accounts.Count;

        if (count == 0)
        {
            return "Not played yet";
        }

        string accounts = count == 1 ? "1 account" : $"{count} accounts";

        return entry.LastLogin is { } lastLogin ? $"{accounts} · {RelativeTime.Describe(lastLogin, now)}" : accounts;
    }
}
