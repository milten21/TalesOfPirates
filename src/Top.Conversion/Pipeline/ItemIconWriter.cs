using System.Collections.Generic;
using System.IO;
using Top.Conversion.Textures;
using Top.Legacy.Tables.Records;
using Top.Logging;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// Writes the icon an iteminfo row names into the tree as PNG. The original
    /// shows texture/icon/name.png, and "0" or blank means the item has no
    /// icon. An icon several items share is written once per run.
    /// </summary>
    public class ItemIconWriter(ConversionSettings settings)
    {
        private const string TextureKind = "icons";

        private const string SourceFolder = "icon";

        private const string NoIcon = "0";

        private readonly HashSet<string> _written = new HashSet<string>();

        public static string ContentPath(ItemInfoRecord item)
        {
            return string.IsNullOrEmpty(item.Icon) || item.Icon == NoIcon
                ? null
                : OutputPaths.TextureContentPath(TextureKind, item.Icon + ".png");
        }

        public void Write(ItemInfoRecord item)
        {
            var contentPath = ContentPath(item);

            if (contentPath == null || !_written.Add(contentPath))
            {
                return;
            }

            var path = settings.Output.At(contentPath);

            if (!settings.Overwrite && File.Exists(path))
            {
                return;
            }

            var source = TextureConversion.FindSource(settings.Source.TextureDir(SourceFolder), item.Icon + ".png");

            if (source == null)
            {
                Log.Warning($"item {item.Id} '{item.Name}': no source file for icon {item.Icon}");

                return;
            }

            byte[] png;

            try
            {
                png = TextureConversion.ToPng(File.ReadAllBytes(source));
            }
            catch (InvalidDataException exception)
            {
                Log.Warning($"item {item.Id} '{item.Name}': icon '{source}'", exception);

                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, png);
        }
    }
}
