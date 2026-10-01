using System;

namespace Top.Client.Features.Lobby.Select;

public static class CameraFraming
{
    public static float MeasurePullBack(float horizontalFovDegrees, float aspect, float verticalFovDegrees,
        float depth)
    {
        if (aspect <= 0f || depth <= 0f)
        {
            return 0f;
        }

        var tanWanted = HalfAngleTangent(verticalFovDegrees);
        var tanNow = HalfAngleTangent(horizontalFovDegrees) / aspect;

        if (tanWanted <= 0f || tanNow <= 0f || tanNow >= tanWanted)
        {
            return 0f;
        }

        return depth * ((tanWanted / tanNow) - 1f);
    }

    private static float HalfAngleTangent(float degrees)
    {
        if (degrees <= 0f || degrees >= 180f)
        {
            return 0f;
        }

        return MathF.Tan(degrees * MathF.PI / 360f);
    }
}
