namespace Top.Client.Features.Camera;

public readonly record struct CameraRange(float Start, float End)
{
    public float Interpolate(float weight)
    {
        return float.Lerp(Start, End, weight);
    }
}
