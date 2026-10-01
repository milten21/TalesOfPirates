using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Conversion.Gltf;
using Top.Conversion.Models;
using Top.Conversion.Models.Characters;
using Top.Conversion.Models.Gltf;
using Top.Logging;
using Top.Legacy.MindPower;
using Top.Legacy.MindPower.Animation;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// What converting a skeleton came to: the rig, and one file for each clip
    /// of the main characters that play on it.
    /// </summary>
    public class RigArtifacts
    {
        public RigArtifacts(ModelArtifact rig, IReadOnlyList<ModelArtifact> clips)
        {
            Rig = rig;
            Clips = clips;
        }

        public ModelArtifact Rig { get; }

        public IReadOnlyList<ModelArtifact> Clips { get; }

        public RigArtifacts AsSkipped()
        {
            return new RigArtifacts(Rig.AsSkipped(), Clips.Select(clip => clip.AsSkipped()).ToList());
        }
    }

    /// <summary>
    /// Converts a .lab skeleton to a packaged glTF rig - its bones, their skin
    /// and the dummies they carry - and writes each clip of the main
    /// characters that play on it to a file of its own. A source path already
    /// converted returns the same artifacts marked skipped.
    /// </summary>
    public class RigConverter
    {
        private readonly ConversionSettings _settings;
        private readonly ClientTables _tables;

        private readonly Dictionary<string, RigArtifacts> _converted =
            new Dictionary<string, RigArtifacts>(StringComparer.OrdinalIgnoreCase);

        public RigConverter(ConversionSettings settings, ClientTables tables)
        {
            _settings = settings;
            _tables = tables;
        }

        public RigArtifacts Convert(int model)
        {
            return Convert(_settings.Source.Skeleton(model));
        }

        public RigArtifacts Convert(string labPath)
        {
            if (_converted.TryGetValue(labPath, out var already))
            {
                return already.AsSkipped();
            }

            var name = Path.GetFileNameWithoutExtension(labPath).ToLowerInvariant();
            var rigPath = _settings.Output.Rig(name);
            var clips = LoadClips(name);

            if (!_settings.Overwrite && File.Exists(rigPath)
                                     && clips.All(clip => File.Exists(_settings.Output.Clip(name, clip.Name))))
            {
                return Remember(labPath, new RigArtifacts(Skipped(name, rigPath),
                    clips.Select(clip => Skipped(clip.Name, _settings.Output.Clip(name, clip.Name))).ToList()));
            }

            if (!File.Exists(labPath))
            {
                Log.Error($"missing '{labPath}'");

                return null;
            }

            Log.Info($"converting rig '{name}'");

            BoneAnimation skeleton;

            try
            {
                using var stream = File.OpenRead(labPath);
                skeleton = LabFile.Read(stream).Animation;
            }
            catch (ParseException exception)
            {
                Log.Error($"failed to parse '{labPath}'", exception);

                return null;
            }

            var rig = Write(name, rigPath, GltfExport.Rig(name, skeleton));
            var keys = BoneTracks.Convert(skeleton);
            var clipArtifacts = clips
                .Select(clip => Write(clip.Name, _settings.Output.Clip(name, clip.Name),
                    GltfExport.Clip(clip.Name, skeleton, keys, clip.StartFrame, clip.EndFrame)))
                .ToList();

            return Remember(labPath, new RigArtifacts(rig, clipArtifacts));
        }

        private static ModelArtifact Write(string name, string glbPath, GltfFile file)
        {
            var packaged = new ModelPackaging(glbPath).Write(file);

            return new ModelArtifact(name, ContentKind.Character, packaged.ModelPath, packaged.TexturePaths,
                ConversionOutcome.Converted);
        }

        private static ModelArtifact Skipped(string name, string glbPath)
        {
            return new ModelArtifact(name, ContentKind.Character, glbPath, Array.Empty<string>(),
                ConversionOutcome.Skipped);
        }

        private RigArtifacts Remember(string labPath, RigArtifacts artifacts)
        {
            _converted[labPath] = artifacts;

            return artifacts;
        }

        private IReadOnlyList<ClipRange> LoadClips(string name)
        {
            if (!int.TryParse(name, out var model))
            {
                Log.Warning($"'{name}' is not a skeleton model id");

                return Array.Empty<ClipRange>();
            }

            var rows = _tables.CharactersOn(model)
                .Where(row => row.ModalType == CharacterModalType.MainCharacter)
                .ToList();

            if (rows.Count == 0)
            {
                Log.Warning($"no main character row uses model {model}");

                return Array.Empty<ClipRange>();
            }

            if (_tables.ActionsFor(rows, model) is not { } poses)
            {
                Log.Warning($"rig {name} has no action set, it converts without clips");

                return Array.Empty<ClipRange>();
            }

            return RigClipSet.Decode(name, poses).Clips;
        }
    }
}
