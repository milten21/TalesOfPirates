using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Conversion.Models;
using Top.Conversion.Textures;
using Top.Conversion.Models.Gltf;
using Top.Conversion.Gltf;
using Top.Logging;
using Top.Legacy.MindPower;
using Top.Legacy.MindPower.Animation;
using Top.Legacy.MindPower.Geometry;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// Converts a client model (.lmo scene or .lgo object) to a packaged glTF
    /// under the output kind folder. A skinned .lgo object binds to the
    /// skeleton of the body type that wears it in the item table. When no body
    /// type or more than one body type wears it, the first four characters of its name
    /// select the .lab. With no .lab there, it converts rigid. A source path
    /// already converted returns the same artifact marked skipped. A part of a
    /// boat or a tower converts rigid under the machine kind, and when it
    /// plays its character's actions, its track is cut into their clips.
    /// </summary>
    public class ModelConverter
    {
        private readonly ConversionSettings _settings;

        private readonly ClientTables _tables;

        private readonly TextureTranslucency _translucency;

        private readonly ArtifactMemo _memo = new ArtifactMemo();

        private readonly Dictionary<string, MachinePart> _machineParts =
            new Dictionary<string, MachinePart>(StringComparer.OrdinalIgnoreCase);

        public ModelConverter(ConversionSettings settings, ClientTables tables,
            TextureTranslucency translucency = null)
        {
            _settings = settings;
            _tables = tables;
            _translucency = translucency ?? new TextureTranslucency();
        }

        public ModelArtifact Convert(string modelPath)
        {
            if (_memo.TryGet(modelPath, out var already))
            {
                return already;
            }

            var kind = DetectKind(modelPath);

            if (kind == null)
            {
                Log.Error($"cannot derive model kind from '{modelPath}': expected a path under <...>/model/<kind>/");

                return null;
            }

            var name = Path.GetFileNameWithoutExtension(modelPath).ToLowerInvariant();
            var glbPath = _settings.Output.Model(kind, name);

            if (!_settings.Overwrite && File.Exists(glbPath))
            {
                return _memo.Add(modelPath,
                    new ModelArtifact(name, kind, glbPath, Array.Empty<string>(), ConversionOutcome.Skipped));
            }

            if (!File.Exists(modelPath))
            {
                Log.Error($"missing '{modelPath}'");

                return null;
            }

            Log.Info($"converting {kind} model '{name}'");

            var isObject = modelPath.EndsWith(".lgo", StringComparison.OrdinalIgnoreCase);

            SceneModel model = null;
            GeometryObject obj = null;

            try
            {
                using var stream = File.OpenRead(modelPath);

                if (isObject)
                {
                    obj = LgoFile.Read(stream).Object;
                }
                else
                {
                    model = LmoFile.Read(stream).Model;
                }
            }
            catch (ParseException exception)
            {
                Log.Error($"failed to parse '{modelPath}'", exception);

                return null;
            }

            var packaging = new ModelPackaging(glbPath, _settings.Output.TextureDir(kind));
            var uriPrefix = packaging.TextureUriPrefix;
            var textureDir = _settings.Source.TextureDir(kind);
            Func<TextureStage, float, bool> isTranslucent =
                (stage, cutoff) => _translucency.IsTranslucent(textureDir, stage, cutoff);
            var objects = isObject ? new[] { obj } : model.GeometryObjects;
            var file = isObject
                ? BuildObject(obj, name, kind, uriPrefix, isTranslucent)
                : GltfExport.Model(name, model, uriPrefix, isTranslucent);

            var packaged = packaging.Write(file, objects, textureDir);

            return _memo.Add(modelPath, new ModelArtifact(name, kind, packaged.ModelPath,
                packaged.TexturePaths, ConversionOutcome.Converted));
        }

        public ModelArtifact ConvertMachinePart(string module, IReadOnlyList<ClipRange> clips)
        {
            var sourcePath = _settings.Source.Model(ContentKind.Character, module + ".lgo");

            if (_machineParts.TryGetValue(sourcePath, out var already))
            {
                if (!already.Clips.SequenceEqual(clips))
                {
                    Log.Warning($"part '{already.Artifact.Name}' is asked for with other clips than it converted " +
                                "with, it keeps the first");
                }

                return already.Artifact.AsSkipped();
            }

            var name = module.ToLowerInvariant();
            var glbPath = _settings.Output.Model(ContentKind.Machine, name);

            if (!_settings.Overwrite && File.Exists(glbPath))
            {
                return RememberMachinePart(sourcePath, clips,
                    new ModelArtifact(name, ContentKind.Machine, glbPath, Array.Empty<string>(),
                        ConversionOutcome.Skipped));
            }

            if (!File.Exists(sourcePath))
            {
                Log.Warning($"missing part '{sourcePath}'");

                return null;
            }

            Log.Info($"converting machine part '{name}'");

            GeometryObject part;

            try
            {
                using var stream = File.OpenRead(sourcePath);

                part = LgoFile.Read(stream).Object;
            }
            catch (ParseException exception)
            {
                Log.Warning($"failed to parse '{sourcePath}'", exception);

                return null;
            }

            var packaging = new ModelPackaging(glbPath, _settings.Output.TextureDir(ContentKind.Machine));
            var textureDir = _settings.Source.TextureDir(ContentKind.Character);
            var file = GltfExport.RigidPart(name, part, clips, packaging.TextureUriPrefix,
                (stage, cutoff) => _translucency.IsTranslucent(textureDir, stage, cutoff));
            var packaged = packaging.Write(file, new[] { part }, textureDir);

            return RememberMachinePart(sourcePath, clips, new ModelArtifact(name, ContentKind.Machine,
                packaged.ModelPath, packaged.TexturePaths, ConversionOutcome.Converted));
        }

        private ModelArtifact RememberMachinePart(string sourcePath, IReadOnlyList<ClipRange> clips,
            ModelArtifact artifact)
        {
            _machineParts[sourcePath] = new MachinePart(artifact, clips);

            return artifact;
        }

        private GltfFile BuildObject(GeometryObject obj, string name, string kind, string uriPrefix,
            Func<TextureStage, float, bool> isTranslucent)
        {
            var skinned = obj.Mesh?.SkinBlends != null && obj.Mesh.SkinBlends.Length > 0;
            var skeleton = skinned ? LoadSkeleton(name) : null;

            return skeleton != null
                ? GltfExport.Object(name, obj, skeleton, uriPrefix, isTranslucent)
                : GltfExport.Object(name, obj, uriPrefix, kind == ContentKind.Item ? 1 : null,
                    isTranslucent);
        }

        private BoneAnimation LoadSkeleton(string modelName)
        {
            var labPath = FindSkeleton(modelName);

            if (labPath == null)
            {
                return null;
            }

            if (!File.Exists(labPath))
            {
                Log.Warning($"no skeleton at '{labPath}', converting rigid");

                return null;
            }

            try
            {
                using var stream = File.OpenRead(labPath);

                return LabFile.Read(stream).Animation;
            }
            catch (ParseException exception)
            {
                Log.Warning($"failed to parse '{labPath}'", exception);

                return null;
            }
        }

        private string FindSkeleton(string modelName)
        {
            var bodyTypes = _tables.FindBodyTypesWearing(modelName);

            if (bodyTypes.Count == 1)
            {
                return _settings.Source.Skeleton(bodyTypes.Single());
            }

            Log.Warning(bodyTypes.Count == 0
                ? $"'{modelName}' is worn by no body type, its skeleton comes from its name"
                : $"'{modelName}' is worn by body types {string.Join(", ", bodyTypes)}, its skeleton comes from its name");

            return modelName.Length < 4 ? null : _settings.Source.Skeleton(modelName.Substring(0, 4));
        }

        private record MachinePart(ModelArtifact Artifact, IReadOnlyList<ClipRange> Clips);

        private static string DetectKind(string modelPath)
        {
            for (var dir = Path.GetDirectoryName(modelPath);
                 !string.IsNullOrEmpty(dir);
                 dir = Path.GetDirectoryName(dir))
            {
                var parent = Path.GetDirectoryName(dir);

                if (!string.IsNullOrEmpty(parent) && string.Equals(Path.GetFileName(parent), "model",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetFileName(dir).ToLowerInvariant();
                }
            }

            return null;
        }
    }
}
