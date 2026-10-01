namespace Top.Client.LegacyBridge
{
    public static class MoveSpeedMapper
    {
        private const float CentimetersPerMeter = 100f;

        public static float ToMoveSpeed(int centimetersPerSecond)
        {
            return centimetersPerSecond / CentimetersPerMeter;
        }
    }
}
