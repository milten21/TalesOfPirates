using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Top.Legacy.MindPower.Cursors
{
    public static class AnimatedCursorReader
    {
        private const int IconFramesFlag = 1;

        public static AnimatedCursor Read(byte[] bytes)
        {
            if (bytes.Length >= 4 && ReadId(bytes, 0) != "RIFF")
            {
                return new AnimatedCursor([IconReader.Read(bytes)], null, [0]);
            }

            if (bytes.Length < 12 || ReadId(bytes, 8) != "ACON")
            {
                throw new InvalidDataException("not a RIFF ACON file");
            }

            var end = Math.Min(bytes.Length, 8 + BitConverter.ToInt32(bytes, 4));
            var hasHeader = false;
            var defaultRate = 0;
            List<CursorFrame> frames = null;
            int[] steps = null;
            int[] rates = null;

            foreach (var (id, offset, size) in ReadChunks(bytes, 12, end))
            {
                if (id == "anih")
                {
                    if (size < 36)
                    {
                        throw new InvalidDataException("anih chunk is too short");
                    }

                    if ((BitConverter.ToInt32(bytes, offset + 32) & IconFramesFlag) == 0)
                    {
                        throw new InvalidDataException("raw bitmap frames are not supported");
                    }

                    defaultRate = BitConverter.ToInt32(bytes, offset + 28);
                    hasHeader = true;
                }
                else if (id == "seq ")
                {
                    steps = ReadIntegers(bytes, offset, size);
                }
                else if (id == "rate")
                {
                    rates = ReadIntegers(bytes, offset, size);
                }
                else if (id == "LIST" && size >= 4 && ReadId(bytes, offset) == "fram")
                {
                    frames = ReadFrames(bytes, offset + 4, offset + size);
                }
            }

            if (!hasHeader)
            {
                throw new InvalidDataException("no anih chunk");
            }

            if (frames == null || frames.Count == 0)
            {
                throw new InvalidDataException("no frames");
            }

            var width = frames[0].Image.Width;
            var height = frames[0].Image.Height;

            if (frames.Exists(frame => frame.Image.Width != width || frame.Image.Height != height))
            {
                throw new InvalidDataException("frames differ in size");
            }

            if (steps != null && steps.Any(step => step < 0 || step >= frames.Count))
            {
                throw new InvalidDataException("seq chunk names a frame that does not exist");
            }

            var stepCount = steps?.Length ?? frames.Count;

            if (rates != null && rates.Length != stepCount)
            {
                throw new InvalidDataException("rate chunk does not match the steps");
            }

            return new AnimatedCursor(frames, steps, rates ?? Enumerable.Repeat(defaultRate, stepCount).ToArray());
        }

        private static int[] ReadIntegers(byte[] bytes, int offset, int size)
        {
            var values = new int[size / 4];

            for (var i = 0; i < values.Length; i++)
            {
                values[i] = BitConverter.ToInt32(bytes, offset + (i * 4));
            }

            return values;
        }

        private static List<CursorFrame> ReadFrames(byte[] bytes, int start, int end)
        {
            var frames = new List<CursorFrame>();

            foreach (var (id, offset, size) in ReadChunks(bytes, start, end))
            {
                if (id != "icon")
                {
                    continue;
                }

                var icon = new byte[size];

                Array.Copy(bytes, offset, icon, 0, size);
                frames.Add(IconReader.Read(icon));
            }

            return frames;
        }

        private static IEnumerable<(string Id, int Offset, int Size)> ReadChunks(byte[] bytes, int start, int end)
        {
            var offset = start;

            while (offset + 8 <= end)
            {
                var size = BitConverter.ToInt32(bytes, offset + 4);

                if (size < 0 || offset + 8 + size > end)
                {
                    throw new InvalidDataException($"chunk '{ReadId(bytes, offset)}' is out of the file");
                }

                yield return (ReadId(bytes, offset), offset + 8, size);

                offset += 8 + size + (size % 2);
            }
        }

        private static string ReadId(byte[] bytes, int offset)
        {
            return Encoding.ASCII.GetString(bytes, offset, 4);
        }
    }
}
