using System;
using System.IO;
using System.Linq;
using Top.Legacy.MindPower;
using Top.Legacy.MindPower.Geometry;
using Top.Legacy.MindPower.Textures;

namespace Top.Conversion.Textures
{
    /// <summary>
    /// Turns a legacy texture file into a PNG: decode by content,
    /// knock out the stage's color key, bleed the remaining color into the
    /// transparent texels, and encode.
    /// </summary>
    public static class TextureConversion
    {
        private const int DilationPasses = 4;

        private const byte Opaque = 255;

        private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

        private static readonly EnumerationOptions AnyCase = new EnumerationOptions
        {
            MatchCasing = MatchCasing.CaseInsensitive,
            AttributesToSkip = 0,
        };

        /// <summary>
        /// What a legacy texture file is called once converted.
        /// </summary>
        public static string PngName(string fileName)
        {
            return Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant() + ".png";
        }

        /// <summary>
        /// Where a legacy texture file sits, or null when it is not there. The
        /// original names a texture without committing to its container.
        /// </summary>
        public static string FindSource(string searchDir, string fileName)
        {
            if (!Directory.Exists(searchDir))
            {
                return null;
            }

            return new[]
                {
                    Path.ChangeExtension(fileName, ".dds"),
                    fileName,
                    Path.ChangeExtension(fileName, ".tga"),
                }
                .Select(candidate => Directory.EnumerateFiles(searchDir, candidate, AnyCase).FirstOrDefault())
                .FirstOrDefault(path => path != null);
        }

        /// <summary>
        /// Encodes as is, no color key and no dilation. A PNG source passes
        /// through unchanged. Throws <see cref="InvalidDataException"/> on a
        /// source the readers reject.
        /// </summary>
        public static byte[] ToPng(byte[] source)
        {
            var decoded = TextureEncode.Decode(source);

            return IsPng(decoded) ? decoded : PngWriter.Write(TextureReader.Read(decoded));
        }

        /// <summary>
        /// Throws <see cref="InvalidDataException"/> when the source is not a
        /// texture the readers recognize.
        /// </summary>
        public static byte[] ToPng(byte[] source, TextureStage stage)
        {
            var image = Decode(source, stage);

            AlphaDilation.Apply(image, DilationPasses);

            return PngWriter.Write(image);
        }

        /// <summary>
        /// Reads the source and knocks out the stage's color key, stopping
        /// short of dilation and encoding. Throws
        /// <see cref="InvalidDataException"/> on a source the readers reject.
        /// </summary>
        public static RgbaImage Decode(byte[] source, TextureStage stage)
        {
            var image = TextureReader.Read(source);

            if (stage.ColorKeyType == ColorKeyType.Specific)
            {
                ColorKeyFilter.Apply(image, stage.ColorKey);
            }
            else if (stage.ColorKeyType == ColorKeyType.FirstPixel)
            {
                ColorKeyFilter.Apply(image, image.Pixels[0]);
            }

            return image;
        }

        private static bool IsPng(byte[] bytes)
        {
            return bytes.AsSpan().StartsWith(PngSignature);
        }

        /// <summary>
        /// The highest alpha below full among the texels, or null when every
        /// texel is opaque. An image that is opaque everywhere blends to the
        /// same pixels as an opaque draw, so it needs no blending.
        /// </summary>
        public static byte? FindHighestTranslucentAlpha(RgbaImage image)
        {
            byte? highest = null;

            foreach (var pixel in image.Pixels)
            {
                if (pixel.A != Opaque && (highest == null || pixel.A > highest))
                {
                    highest = pixel.A;
                }
            }

            return highest;
        }
    }
}
