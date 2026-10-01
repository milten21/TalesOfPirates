using System;
using System.Globalization;
using Godot;

namespace Top.Client.Features.Party;

public partial class InviteCard : Control
{
    private uint _inviterId;

    private DateTime _receivedAt;

    [Signal] public delegate void AcceptedEventHandler(uint inviterId);

    [Signal] public delegate void DeclinedEventHandler(uint inviterId);

    [Export] public Label SecondsLabel { get; set; } = null!;

    [Export] public Label NameLabel { get; set; } = null!;

    [Export] public Control MeterFill { get; set; } = null!;

    [Export] public Button DeclineButton { get; set; } = null!;

    [Export] public Button AcceptButton { get; set; } = null!;

    public override void _Ready()
    {
        DeclineButton.Pressed += () => EmitSignal(SignalName.Declined, _inviterId);
        AcceptButton.Pressed += () => EmitSignal(SignalName.Accepted, _inviterId);

        VisibilityChanged += () => SetProcess(Visible);

        SetProcess(Visible);
    }

    public override void _Process(double delta)
    {
        UpdateTimer();
    }

    public void ShowInvite(PendingInvite invite)
    {
        _inviterId = invite.InviterId;
        _receivedAt = invite.ReceivedAt;
        NameLabel.Text = invite.Name;

        UpdateTimer();
    }

    private void UpdateTimer()
    {
        var remaining = PartyInviteQueue.InviteSeconds - (DateTime.UtcNow - _receivedAt).TotalSeconds;
        var fraction = Mathf.Clamp((float)(remaining / PartyInviteQueue.InviteSeconds), 0f, 1f);

        SecondsLabel.Text = string.Create(CultureInfo.InvariantCulture,
            $"{Math.Clamp((int)Math.Ceiling(remaining), 0, PartyInviteQueue.InviteSeconds)}s");
        MeterFill.AnchorRight = fraction;
    }
}
