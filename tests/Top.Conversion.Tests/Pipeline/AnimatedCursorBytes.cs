using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Top.Legacy.MindPower;

namespace Top.Conversion.Tests.Pipeline
{
    /// <summary>
    /// One frame of a fixture cursor: a single color over the whole frame, with
    /// the pixels that the mask makes transparent.
    /// </summary>
    internal class AnimatedCursorFrame(Rgba32 color, int hotspotX = 0, int hotspotY = 0, int bitsPerPixel = 24,
        ushort type = AnimatedCursorFrame.CursorType)
    {
        public const ushort IconType = 1;

        public const ushort CursorType = 2;

        public Rgba32 Color { get; } = color;

        public int HotspotX { get; } = hotspotX;

        public int HotspotY { get; } = hotspotY;

        public int BitsPerPixel { get; } = bitsPerPixel;

        public ushort Type { get; } = type;

        public HashSet<(int X, int Y)> Transparent { get; } = [];
    }

    /// <summary>
    /// Writes RIFF ACON files as the original client has them: an anih chunk
    /// and a fram list of cursor-type icon chunks.
    /// </summary>
    internal static class AnimatedCursorBytes
    {
        internal const int FrameSize = 4;

        private const int IconFlag = 1;

        private const int SequenceFlag = 2;

        internal static byte[] Write(params AnimatedCursorFrame[] frames)
        {
            return WriteTimed(frames);
        }

        internal static byte[] WriteIcon(AnimatedCursorFrame frame)
        {
            return WriteCursor(frame);
        }

        internal static byte[] WriteTimed(AnimatedCursorFrame[] frames, int[] steps = null, int[] rates = null,
            int defaultRate = 10)
        {
            using var body = new MemoryStream();
            using var writer = new BinaryWriter(body);

            writer.Write(Encoding.ASCII.GetBytes("ACON"));

            var header = new byte[36];

            using (var headerWriter = new BinaryWriter(new MemoryStream(header)))
            {
                headerWriter.Write(36);
                headerWriter.Write(frames.Length);
                headerWriter.Write(steps?.Length ?? frames.Length);
                headerWriter.Write(0);
                headerWriter.Write(0);
                headerWriter.Write(0);
                headerWriter.Write(0);
                headerWriter.Write(defaultRate);
                headerWriter.Write(steps == null ? IconFlag : IconFlag | SequenceFlag);
            }

            WriteChunk(writer, "anih", header);

            if (rates != null)
            {
                WriteChunk(writer, "rate", ToBytes(rates));
            }

            if (steps != null)
            {
                WriteChunk(writer, "seq ", ToBytes(steps));
            }

            using var list = new MemoryStream();
            using var listWriter = new BinaryWriter(list);

            listWriter.Write(Encoding.ASCII.GetBytes("fram"));

            foreach (var frame in frames)
            {
                WriteChunk(listWriter, "icon", WriteCursor(frame));
            }

            WriteChunk(writer, "LIST", list.ToArray());

            using var file = new MemoryStream();
            using var fileWriter = new BinaryWriter(file);

            WriteChunk(fileWriter, "RIFF", body.ToArray());

            return file.ToArray();
        }

        private static byte[] ToBytes(int[] values)
        {
            return values.SelectMany(BitConverter.GetBytes).ToArray();
        }

        private static void WriteChunk(BinaryWriter writer, string id, byte[] data)
        {
            writer.Write(Encoding.ASCII.GetBytes(id));
            writer.Write(data.Length);
            writer.Write(data);

            if (data.Length % 2 != 0)
            {
                writer.Write((byte)0);
            }
        }

        private static byte[] WriteCursor(AnimatedCursorFrame frame)
        {
            var image = WriteBitmap(frame);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write((ushort)0);
            writer.Write(frame.Type);
            writer.Write((ushort)1);
            writer.Write((byte)FrameSize);
            writer.Write((byte)FrameSize);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)frame.HotspotX);
            writer.Write((ushort)frame.HotspotY);
            writer.Write(image.Length);
            writer.Write(22);
            writer.Write(image);

            return stream.ToArray();
        }

        private static byte[] WriteBitmap(AnimatedCursorFrame frame)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(40);
            writer.Write(FrameSize);
            writer.Write(FrameSize * 2);
            writer.Write((ushort)1);
            writer.Write((ushort)frame.BitsPerPixel);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            var bytesPerPixel = frame.BitsPerPixel / 8;
            var colorStride = ((FrameSize * frame.BitsPerPixel) + 31) / 32 * 4;

            for (var y = FrameSize - 1; y >= 0; y--)
            {
                for (var x = 0; x < FrameSize; x++)
                {
                    var transparent = frame.Transparent.Contains((x, y));

                    writer.Write(frame.Color.B);
                    writer.Write(frame.Color.G);
                    writer.Write(frame.Color.R);

                    if (bytesPerPixel == 4)
                    {
                        writer.Write(transparent ? (byte)0 : frame.Color.A);
                    }
                }

                writer.Write(new byte[colorStride - (FrameSize * bytesPerPixel)]);
            }

            const int maskStride = 4;

            for (var y = FrameSize - 1; y >= 0; y--)
            {
                var row = new byte[maskStride];

                for (var x = 0; x < FrameSize; x++)
                {
                    if (frame.Transparent.Contains((x, y)))
                    {
                        row[x / 8] |= (byte)(0x80 >> (x % 8));
                    }
                }

                writer.Write(row);
            }

            return stream.ToArray();
        }
    }
}
