using Godot;
using Top.Client.UI;

namespace Top.Client.Features.World;

public partial class GameMenu : Screen
{
    private const double FadeSeconds = 0.15;

    private const string GoldPillVariation = "GoldPill";

    private const string DangerPillVariation = "DangerPill";

    private Choice _choice;

    private Tween? _fade;

    [Signal]
    public delegate void SwitchCharacterConfirmedEventHandler();

    [Signal]
    public delegate void LogOutConfirmedEventHandler();

    [Signal]
    public delegate void ExitConfirmedEventHandler();

    [Export] public ActionArea ActionArea { get; set; } = null!;

    [Export] public Control Items { get; set; } = null!;

    [Export] public MenuItem SwitchCharacterItem { get; set; } = null!;

    [Export] public MenuItem LogOutItem { get; set; } = null!;

    [Export] public MenuItem ExitItem { get; set; } = null!;

    [Export] public InlineConfirm Confirm { get; set; } = null!;

    [Export] public Label ConfirmTitle { get; set; } = null!;

    public bool IsOpen { get; private set; }

    public bool IsLocked { get; private set; }

    public bool IsConfirming => Confirm.IsOpen;

    protected override Control? NavigationStart => IsOpen && !IsLocked && !IsConfirming ? SwitchCharacterItem : null;

    private MenuItem ChoiceItem => _choice switch
    {
        Choice.SwitchCharacter => SwitchCharacterItem,
        Choice.LogOut => LogOutItem,
        _ => ExitItem,
    };

    public override void _Ready()
    {
        SwitchCharacterItem.Pressed += () => Ask(Choice.SwitchCharacter);
        LogOutItem.Pressed += () => Ask(Choice.LogOut);
        ExitItem.Pressed += () => Ask(Choice.Exit);
        Confirm.Confirmed += ConfirmChoice;
        Confirm.Kept += ShowItems;

        StackFocus(SwitchCharacterItem, LogOutItem, ExitItem);

        Hide();
    }

    public void Open()
    {
        if (IsOpen || IsLocked)
        {
            return;
        }

        IsOpen = true;

        GetViewport().GuiReleaseFocus();

        Confirm.Close();
        Confirm.Hide();
        Items.Show();
        Items.Modulate = Colors.White;

        if (!Visible)
        {
            Modulate = Colors.Transparent;

            Show();
        }

        FadeTo(1f);
    }

    public void Dismiss()
    {
        if (!IsOpen || IsLocked)
        {
            return;
        }

        FadeOut();
    }

    public void CancelConfirm()
    {
        if (!IsLocked)
        {
            Confirm.Keep();
        }
    }

    private void Ask(Choice choice)
    {
        if (!IsOpen || IsLocked || IsConfirming)
        {
            return;
        }

        _choice = choice;

        var (title, question, pillCaption, pillVariation) = choice switch
        {
            Choice.SwitchCharacter => ("SWITCH CHARACTER", "Return to character selection?", "SWITCH",
                GoldPillVariation),
            Choice.LogOut => ("LOG OUT", "Log out and return to the main menu?", "LOG OUT", GoldPillVariation),
            _ => ("EXIT GAME", "Close the game?", "EXIT", DangerPillVariation),
        };

        ConfirmTitle.Text = title;
        Confirm.ConfirmButton.Text = pillCaption;
        Confirm.ConfirmButton.ThemeTypeVariation = pillVariation;

        ActionArea.Switch(Confirm);

        Confirm.Ask(question);
    }

    private void ShowItems()
    {
        ActionArea.Switch(Items);

        if (Confirm.WasOpenedWithKeyboard)
        {
            ChoiceItem.GrabFocus();
        }
    }

    private void ConfirmChoice()
    {
        IsLocked = true;

        switch (_choice)
        {
            case Choice.SwitchCharacter:
                FadeOut();
                EmitSignal(SignalName.SwitchCharacterConfirmed);
                break;
            case Choice.LogOut:
                EmitSignal(SignalName.LogOutConfirmed);
                break;
            default:
                EmitSignal(SignalName.ExitConfirmed);
                break;
        }
    }

    private void FadeOut()
    {
        IsOpen = false;

        GetViewport().GuiReleaseFocus();

        FadeTo(0f).Chain().TweenCallback(Callable.From(Hide));
    }

    private Tween FadeTo(float alpha)
    {
        _fade?.Kill();
        _fade = CreateTween();
        _fade.TweenProperty(this, "modulate:a", alpha, FadeSeconds);

        return _fade;
    }

    private enum Choice
    {
        SwitchCharacter,
        LogOut,
        Exit,
    }
}
