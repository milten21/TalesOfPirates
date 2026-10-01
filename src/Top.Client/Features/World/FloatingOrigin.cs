using Godot;

namespace Top.Client.Features.World;

public class FloatingOrigin(Node3D worldRoot)
{
    private const float MaxDistanceFromOrigin = 100f;

    public void Recentre(Vector3 position)
    {
        var origin = -worldRoot.Position;

        if (Mathf.Abs(position.X - origin.X) <= MaxDistanceFromOrigin
            && Mathf.Abs(position.Z - origin.Z) <= MaxDistanceFromOrigin)
        {
            return;
        }

        worldRoot.Position = new Vector3(-Mathf.Round(position.X), 0f, -Mathf.Round(position.Z));
    }
}
