using System.IO;
using System.Linq;
using Top.Contracts.Assets.Maps;
using Top.Conversion.Pipeline.Tables;
using Top.Conversion.Textures;
using Top.Legacy.Tables;
using Top.Legacy.Tables.Records;
using Top.Logging;

namespace Top.Conversion.Pipeline.Maps
{
    /// <summary>
    /// Writes every terrain texture the table names, the mask sheet and the
    /// water loop into the tree as PNG. A texture already there is left alone.
    /// </summary>
    public class TerrainTextureWriter(ConversionSettings settings)
    {
        public const string MaskSource = "texture/terrain/alpha/total.tga";

        private bool _done;

        public void Write(Table<TerrainInfoRecord> terrain)
        {
            if (_done)
            {
                return;
            }

            _done = true;

            foreach (var row in terrain ?? Enumerable.Empty<TerrainInfoRecord>())
            {
                if (!string.IsNullOrEmpty(row.Name))
                {
                    Write(row.Name, OutputPaths.TextureContentPath(TerrainTableUnit.TextureKind, row.Name));
                }
            }

            Write(MaskSource, MapTexturePaths.Masks);

            for (var frame = 0; frame < MapTexturePaths.WaterFrames; frame++)
            {
                Write(WaterSource(frame), MapTexturePaths.WaterFrame(frame));
            }
        }

        private static string WaterSource(int frame)
        {
            return $"texture/terrain/water/ocean_h.{frame + 1:00}.bmp";
        }

        private void Write(string sourcePath, string contentPath)
        {
            var path = settings.Output.At(contentPath);

            if (File.Exists(path))
            {
                return;
            }

            var source = Path.Combine(settings.ClientRoot, sourcePath);

            if (!File.Exists(source))
            {
                Log.Warning($"missing '{source}'");

                return;
            }

            byte[] png;

            try
            {
                png = TextureConversion.ToPng(File.ReadAllBytes(source));
            }
            catch (InvalidDataException exception)
            {
                Log.Warning($"texture '{sourcePath}'", exception);

                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, png);
        }
    }
}
