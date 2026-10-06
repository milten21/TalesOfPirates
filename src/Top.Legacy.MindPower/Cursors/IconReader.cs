using System;
using System.IO;
using Top.Legacy.MindPower.Textures;

namespace Top.Legacy.MindPower.Cursors
{
    public static class IconReader
    {
        private const int IconType = 1;

        private const int CursorType = 2;

        private const int DirectoryHeaderSize = 6;

        public static CursorFrame Read(byte[] bytes)
        {
            if (bytes.Length < DirectoryHeaderSize + 16)
            {
                throw new InvalidDataException("not an icon or cursor file");
            }

            var type = BitConverter.ToUInt16(bytes, 2);
            var count = BitConverter.ToUInt16(bytes, 4);

            if (type != IconType && type != CursorType)
            {
                throw new InvalidDataException($"unsupported icon file type {type}");
            }

            if (count == 0)
            {
                throw new InvalidDataException("icon file has no images");
            }

            var image = ReadBitmap(bytes, BitConverter.ToInt32(bytes, DirectoryHeaderSize + 12));

            return new CursorFrame(image, BitConverter.ToUInt16(bytes, DirectoryHeaderSize + 4),
                BitConverter.ToUInt16(bytes, DirectoryHeaderSize + 6));
        }

        private static RgbaImage ReadBitmap(byte[] bytes, int offset)
        {
            if (offset < 0 || offset + 40 > bytes.Length)
            {
                throw new InvalidDataException("cursor image is out of the file");
            }

            if (bytes[offset] == 0x89 && bytes[offset + 1] == (byte)'P')
            {
                throw new InvalidDataException("PNG cursor images are not supported");
            }

            var headerSize = BitConverter.ToInt32(bytes, offset);
            var width = BitConverter.ToInt32(bytes, offset + 4);
            var height = Math.Abs(BitConverter.ToInt32(bytes, offset + 8)) / 2;
            int bitsPerPixel = BitConverter.ToUInt16(bytes, offset + 14);
            var compression = BitConverter.ToInt32(bytes, offset + 16);
            var usedColors = BitConverter.ToInt32(bytes, offset + 32);

            if (compression != 0)
            {
                throw new InvalidDataException($"unsupported cursor compression {compression}");
            }

            if (bitsPerPixel != 1 && bitsPerPixel != 4 && bitsPerPixel != 8 && bitsPerPixel != 24 &&
                bitsPerPixel != 32)
            {
                throw new InvalidDataException($"unsupported cursor bit count {bitsPerPixel}");
            }

            var paletteOffset = offset + headerSize;
            var paletteSize = bitsPerPixel <= 8 ? (usedColors > 0 ? usedColors : 1 << bitsPerPixel) : 0;
            var colorOffset = paletteOffset + (paletteSize * 4);
            var colorStride = ((width * bitsPerPixel) + 31) / 32 * 4;
            var maskOffset = colorOffset + (colorStride * height);
            var maskStride = (width + 31) / 32 * 4;

            if (maskOffset + (maskStride * height) > bytes.Length)
            {
                throw new InvalidDataException("cursor image is cut short");
            }

            var pixels = new Rgba32[width * height];
            var hasAlpha = false;

            for (var y = 0; y < height; y++)
            {
                var row = colorOffset + ((height - 1 - y) * colorStride);

                for (var x = 0; x < width; x++)
                {
                    var pixel = ReadPixel(bytes, row, x, bitsPerPixel, paletteOffset);

                    hasAlpha |= bitsPerPixel == 32 && pixel.A != 0;
                    pixels[(y * width) + x] = pixel;
                }
            }

            if (hasAlpha)
            {
                return new RgbaImage(width, height, pixels);
            }

            for (var y = 0; y < height; y++)
            {
                var row = maskOffset + ((height - 1 - y) * maskStride);

                for (var x = 0; x < width; x++)
                {
                    var masked = (bytes[row + (x / 8)] & (0x80 >> (x % 8))) != 0;
                    var pixel = pixels[(y * width) + x];

                    pixels[(y * width) + x] = new Rgba32(pixel.R, pixel.G, pixel.B, masked ? (byte)0 : (byte)255);
                }
            }

            return new RgbaImage(width, height, pixels);
        }

        private static Rgba32 ReadPixel(byte[] bytes, int row, int x, int bitsPerPixel, int paletteOffset)
        {
            switch (bitsPerPixel)
            {
                case 32:
                {
                    var at = row + (x * 4);

                    return new Rgba32(bytes[at + 2], bytes[at + 1], bytes[at], bytes[at + 3]);
                }

                case 24:
                {
                    var at = row + (x * 3);

                    return new Rgba32(bytes[at + 2], bytes[at + 1], bytes[at], 255);
                }

                default:
                {
                    var bit = x * bitsPerPixel;
                    var shift = 8 - bitsPerPixel - (bit % 8);
                    var index = (bytes[row + (bit / 8)] >> shift) & ((1 << bitsPerPixel) - 1);
                    var at = paletteOffset + (index * 4);

                    return new Rgba32(bytes[at + 2], bytes[at + 1], bytes[at], 255);
                }
            }
        }
    }
}
