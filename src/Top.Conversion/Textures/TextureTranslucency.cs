using System;
using System.Collections.Generic;
using System.IO;
using Top.Legacy.MindPower.Geometry;
using Top.Logging;

namespace Top.Conversion.Textures
{
    /// <summary>
    /// Answers whether the texture behind a material stage carries any
    /// translucency that a draw keeps, decoding each file at most once for a
    /// whole run. A material only needs blending when its texture has a texel
    /// below full alpha, and an alpha-tested draw discards every texel under
    /// its cutoff, so only a texel from the cutoff up counts. Packaging writes
    /// one PNG per texture folder and file name, so the first answer for that
    /// pair is the one the shipped PNG matches.
    /// </summary>
    public class TextureTranslucency
    {
        private const float Opaque = 255f;

        private readonly Dictionary<string, DecodedTexture> _decoded =
            new Dictionary<string, DecodedTexture>(StringComparer.OrdinalIgnoreCase);

        public bool IsTranslucent(string searchDir, TextureStage stage, float cutoff = 0f)
        {
            if (searchDir == null || stage == null || string.IsNullOrEmpty(stage.FileName))
            {
                return false;
            }

            var key = Path.Combine(searchDir, TextureConversion.PngName(stage.FileName));

            if (_decoded.TryGetValue(key, out var decoded))
            {
                if (decoded.ColorKey != stage.ColorKeyType)
                {
                    Log.Warning($"texture '{stage.FileName}' is converted with colour key " +
                                $"{decoded.ColorKey}, not {stage.ColorKeyType}");
                }
            }
            else
            {
                decoded = new DecodedTexture(Measure(searchDir, stage), stage.ColorKeyType);
                _decoded[key] = decoded;
            }

            return decoded.HighestTranslucentAlpha is { } highest && highest / Opaque >= cutoff;
        }

        private static byte? Measure(string searchDir, TextureStage stage)
        {
            var sourcePath = TextureConversion.FindSource(searchDir, stage.FileName);

            if (sourcePath == null)
            {
                return null;
            }

            try
            {
                return TextureConversion.FindHighestTranslucentAlpha(
                    TextureConversion.Decode(File.ReadAllBytes(sourcePath), stage));
            }
            catch (InvalidDataException exception)
            {
                Log.Warning($"texture '{stage.FileName}' read as opaque", exception);

                return null;
            }
        }

        private readonly struct DecodedTexture
        {
            public DecodedTexture(byte? highestTranslucentAlpha, ColorKeyType colorKey)
            {
                HighestTranslucentAlpha = highestTranslucentAlpha;
                ColorKey = colorKey;
            }

            public byte? HighestTranslucentAlpha { get; }

            public ColorKeyType ColorKey { get; }
        }
    }
}
