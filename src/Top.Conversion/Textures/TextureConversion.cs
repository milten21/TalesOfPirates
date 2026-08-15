using System.IO;
using Top.Legacy.MindPower.Geometry;
using Top.Legacy.MindPower.Textures;

namespace Top.Conversion.Textures
{
    /// <summary>
    /// Turns a legacy texture file into a Unity-ready PNG: decode by content,
    /// knock out the stage's color key, bleed the remaining color into the
    /// transparent texels, and encode.
    /// </summary>
    public static class TextureConversion
    {
        private const int DilationPasses = 4;

        /// <summary>
        /// What a legacy texture file is called once converted.
        /// </summary>
        public static string PngName(string fileName)
        {
            return Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant() + ".png";
        }

        /// <summary>
        /// Encodes as is, no color key and no dilation. Throws
        /// <see cref="InvalidDataException"/> on a source the readers reject.
        /// </summary>
        public static byte[] ToPng(byte[] source)
        {
            return PngWriter.Write(TextureReader.Read(source));
        }

        /// <summary>
        /// Throws <see cref="InvalidDataException"/> when the source is not a
        /// texture the readers recognize.
        /// </summary>
        public static byte[] ToPng(byte[] source, TextureStage stage)
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

            AlphaDilation.Apply(image, DilationPasses);

            return PngWriter.Write(image);
        }
    }
}
