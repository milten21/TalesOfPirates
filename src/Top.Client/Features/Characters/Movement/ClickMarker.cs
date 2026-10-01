using Godot;

namespace Top.Client.Features.Characters.Movement;

public partial class ClickMarker : Decal
{
    private Tween? _fade;

    private float _fullEmissionEnergy;

    [Export] public double FadeDuration { get; set; } = 1.0;

    public override void _Ready()
    {
        _fullEmissionEnergy = EmissionEnergy;

        Hide();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged && !Visible)
        {
            _fade?.Kill();
        }
    }

    public void ShowAt(Vector3 position)
    {
        _fade?.Kill();

        Position = position;
        ChangeOpacity(1f);
        Show();

        _fade = CreateTween();
        _fade.TweenMethod(Callable.From<float>(ChangeOpacity), 1f, 0f, FadeDuration);
        _fade.TweenCallback(Callable.From(Hide));
    }

    private void ChangeOpacity(float opacity)
    {
        Modulate = Modulate with { A = opacity };
        EmissionEnergy = _fullEmissionEnergy * opacity;
    }
}
