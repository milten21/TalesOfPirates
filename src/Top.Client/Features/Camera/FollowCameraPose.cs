using System;
using System.Numerics;

namespace Top.Client.Features.Camera;

public class FollowCameraPose(
    CameraRange distance,
    CameraRange height,
    CameraRange fov,
    float distanceShortening,
    float smoothingSpeed,
    float focusDeadZone,
    float focusSmoothingSpeed)
{
    private const float NearEnd = 0f;

    private const float WideEnd = 1f;

    private const float Tolerance = 1e-4f;

    private float _defaultYaw;

    private float _yawDestination;

    private float _zoomDestination = WideEnd;

    private float _focusHeightDestination;

    public float Zoom { get; private set; } = WideEnd;

    public float FocusHeight { get; private set; }

    public float Distance => distance.Interpolate(Zoom);

    public float Height => height.Interpolate(Zoom);

    public float Fov => fov.Interpolate(Zoom);

    public float Yaw { get; private set; }

    public bool IsDefaultView => Zoom >= WideEnd - Tolerance && MathF.Abs(MeasureTurn(Yaw, _defaultYaw)) <= Tolerance;

    public Vector3 EyeOffset
    {
        get
        {
            var across = Distance * distanceShortening;

            return new Vector3(MathF.Sin(Yaw) * across, Height, MathF.Cos(Yaw) * across);
        }
    }

    public void YawBy(float radians)
    {
        Yaw += radians;
        _yawDestination = Yaw;
    }

    public void ZoomBy(float amount)
    {
        _zoomDestination = Math.Clamp(_zoomDestination + amount, NearEnd, WideEnd);
    }

    public void Recentre(float yaw)
    {
        _defaultYaw = yaw;
        _yawDestination = Yaw + MeasureTurn(Yaw, yaw);
        _zoomDestination = WideEnd;
    }

    public void PlaceFocus(float height)
    {
        FocusHeight = height;
        _focusHeightDestination = height;
    }

    public void TrackFocus(float height)
    {
        _focusHeightDestination = Math.Clamp(_focusHeightDestination, height - focusDeadZone, height + focusDeadZone);
    }

    public void Advance(float seconds)
    {
        var weight = MeasureWeight(smoothingSpeed, seconds);

        Yaw = float.Lerp(Yaw, _yawDestination, weight);
        Zoom = float.Lerp(Zoom, _zoomDestination, weight);
        FocusHeight = float.Lerp(FocusHeight, _focusHeightDestination, MeasureWeight(focusSmoothingSpeed, seconds));
    }

    private static float MeasureWeight(float speed, float seconds)
    {
        return 1f - MathF.Exp(-speed * seconds);
    }

    private static float MeasureTurn(float from, float to)
    {
        return MathF.IEEERemainder(to - from, MathF.Tau);
    }
}
