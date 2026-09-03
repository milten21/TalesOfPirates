using Top.Contracts;

namespace Top.Conversion.Pipeline.Tables
{
    public static class TableColor
    {
        public static Rgb Read(int[] channels)
        {
            return channels != null && channels.Length >= 3
                ? new Rgb((byte)channels[0], (byte)channels[1], (byte)channels[2])
                : default;
        }
    }
}
