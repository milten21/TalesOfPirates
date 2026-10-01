using System;
using System.Numerics;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge
{
    public static class MapPointMapper
    {
        private const float CentimetersPerMeter = 100f;

        public static Vector2 ToMapPoint(Point packetPoint)
        {
            return new Vector2(packetPoint.X, packetPoint.Y) / CentimetersPerMeter;
        }

        public static Point ToPacketPoint(Vector2 mapPoint)
        {
            return new Point(ToWholeCentimeters(mapPoint.X), ToWholeCentimeters(mapPoint.Y));
        }

        private static int ToWholeCentimeters(float meters)
        {
            return (int)MathF.Round(meters * CentimetersPerMeter, MidpointRounding.AwayFromZero);
        }
    }
}
