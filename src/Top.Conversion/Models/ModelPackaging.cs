using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Conversion.Textures;
using Top.Conversion.Gltf;
using Top.Logging;
using Top.Legacy.MindPower.Geometry;

namespace Top.Conversion.Models
{
    /// <summary>
    /// What a built model became on disk.
    /// </summary>
    public class PackagedModel
    {
        public PackagedModel(string modelPath, string binPath, IReadOnlyList<string> texturePaths)
        {
            ModelPath = modelPath;
            BinPath = binPath;
            TexturePaths = texturePaths;
        }

        public string ModelPath { get; }

        public string BinPath { get; }

        public IReadOnlyList<string> TexturePaths { get; }
    }

    /// <summary>
    /// Writes a built model into the converted tree: the glTF itself, and the
    /// PNGs its source objects name.
    /// </summary>
    public class ModelPackaging
    {
        private readonly string _modelPath;
        private readonly string _modelOutputDir;
        private readonly string _textureOutputDir;
        private readonly GltfPackaging _packaging;

        public ModelPackaging(string modelPath, string textureOutputDir = null,
            GltfPackaging packaging = GltfPackaging.Glb)
        {
            if (!Enum.IsDefined(typeof(GltfPackaging), packaging))
            {
                throw new ArgumentOutOfRangeException(nameof(packaging), packaging, null);
            }

            _modelPath = modelPath;
            _modelOutputDir = Path.GetDirectoryName(modelPath);
            _textureOutputDir = textureOutputDir;
            _packaging = packaging;
        }

        public string TextureUriPrefix => Path
            .GetRelativePath(_modelOutputDir, _textureOutputDir)
            .Replace('\\', '/');

        public PackagedModel Write(GltfFile file)
        {
            Directory.CreateDirectory(_modelOutputDir);

            var modelPath = WriteModel(file, out var binPath);

            return new PackagedModel(modelPath, binPath, Array.Empty<string>());
        }

        public PackagedModel Write(GltfFile file, IReadOnlyList<GeometryObject> objects,
            string textureSearchDir)
        {
            Directory.CreateDirectory(_modelOutputDir);
            Directory.CreateDirectory(_textureOutputDir);

            var modelPath = WriteModel(file, out var binPath);
            var texturePaths = WriteTextures(objects, textureSearchDir);

            return new PackagedModel(modelPath, binPath, texturePaths);
        }

        private static IEnumerable<TextureStage> CollectTextureStages(IReadOnlyList<GeometryObject> objects)
        {
            var seen = new HashSet<string>();

            foreach (var obj in objects)
            {
                foreach (var material in obj.Materials ?? Enumerable.Empty<MaterialTexture>())
                {
                    if (material.Stages == null)
                    {
                        continue;
                    }

                    for (var s = 0; s < material.Stages.Length; s++)
                    {
                        var stage = material.Stages[s];

                        if (stage == null || string.IsNullOrEmpty(stage.FileName))
                        {
                            continue;
                        }

                        if (s > 0)
                        {
                            Log.Warning($"multi-stage texture '{stage.FileName}' (stage {s}) ignored");
                            continue;
                        }

                        if (seen.Add(TextureConversion.PngName(stage.FileName)))
                        {
                            yield return stage;
                        }
                    }
                }

                var flipbooks = obj.Animation?.TextureImage;

                if (flipbooks == null)
                {
                    continue;
                }

                for (var subset = 0; subset < flipbooks.GetLength(0); subset++)
                {
                    for (var stage = 0; stage < flipbooks.GetLength(1); stage++)
                    {
                        var track = flipbooks[subset, stage];

                        if (track?.DataSequence == null || track.DataSequence.Length == 0)
                        {
                            continue;
                        }

                        if (stage > 0)
                        {
                            Log.Warning($"texture-image animation on stage {stage} ignored");
                            continue;
                        }

                        foreach (var frame in track.DataSequence)
                        {
                            if (frame == null || string.IsNullOrEmpty(frame.FileName))
                            {
                                continue;
                            }

                            if (seen.Add(TextureConversion.PngName(frame.FileName)))
                            {
                                yield return frame;
                            }
                        }
                    }
                }
            }
        }

        private string WriteModel(GltfFile file, out string binPath)
        {
            binPath = null;

            switch (_packaging)
            {
                case GltfPackaging.GltfEmbedded:
                    {
                        using var fs = File.Create(_modelPath);
                        GltfWriter.WriteGltfEmbedded(file.Document, file.BinChunk, fs);

                        return _modelPath;
                    }

                case GltfPackaging.GltfWithBin:
                    {
                        binPath = Path.ChangeExtension(_modelPath, ".bin");

                        using var json = File.Create(_modelPath);
                        using var bin = File.Create(binPath);
                        GltfWriter.WriteGltfWithBin(file.Document, file.BinChunk,
                            Path.GetFileName(binPath), json, bin);

                        return _modelPath;
                    }

                case GltfPackaging.Glb:
                    {
                        using var fs = File.Create(_modelPath);
                        GltfWriter.WriteGlb(file.Document, file.BinChunk, fs);

                        return _modelPath;
                    }

                default:
                    throw new InvalidOperationException($"unhandled packaging {_packaging}");
            }
        }

        private List<string> WriteTextures(IReadOnlyList<GeometryObject> objects, string searchDir)
        {
            var written = new List<string>();

            foreach (var stage in CollectTextureStages(objects))
            {
                var fileName = stage.FileName;
                var pngPath = Path.Combine(_textureOutputDir, TextureConversion.PngName(fileName));

                if (File.Exists(pngPath))
                {
                    written.Add(pngPath);
                    continue;
                }

                var sourcePath = TextureConversion.FindSource(searchDir, fileName);

                if (sourcePath == null)
                {
                    Log.Warning($"texture '{fileName}' not found in '{searchDir}'");
                    continue;
                }

                byte[] png;

                try
                {
                    png = TextureConversion.ToPng(File.ReadAllBytes(sourcePath), stage);
                }
                catch (InvalidDataException exception)
                {
                    Log.Warning($"texture '{fileName}'", exception);
                    continue;
                }

                File.WriteAllBytes(pngPath, png);

                written.Add(pngPath);
            }

            return written;
        }
    }
}
