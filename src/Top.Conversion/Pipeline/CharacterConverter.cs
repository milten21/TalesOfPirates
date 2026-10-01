using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Top.Conversion.Models;
using Top.Conversion.Models.Gltf;
using Top.Conversion.Textures;
using Top.Logging;
using Top.Legacy.MindPower;
using Top.Legacy.MindPower.Animation;
using Top.Legacy.MindPower.Geometry;
using Top.Legacy.Tables.Records;
using BoatSlots = Top.Contracts.Tables.Characters.BoatSlots;
using TowerSlots = Top.Contracts.Tables.Characters.TowerSlots;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// A characterinfo unit. A player character stays in pieces - a rig its
    /// equipment binds to - while a monster is merged into a single glTF that
    /// carries its own skeleton.
    /// </summary>
    public class CharacterResult : UnitResult
    {
        private static readonly IReadOnlyList<ModelArtifact> NoClips = Array.Empty<ModelArtifact>();

        private static readonly IReadOnlyList<ModelArtifact> NoMachineParts = Array.Empty<ModelArtifact>();

        public CharacterResult(int id, string name, ConversionOutcome outcome,
            RigArtifacts rig, ModelArtifact model, IReadOnlyList<ItemResult> parts,
            IReadOnlyList<ModelArtifact> machineParts = null)
            : base(id, name, outcome)
        {
            Rig = rig?.Rig;
            Clips = rig?.Clips ?? NoClips;
            Model = model;
            Parts = parts;
            MachineParts = machineParts ?? NoMachineParts;
        }

        /// <summary>
        /// The skeleton a player character animates on, null for a monster.
        /// </summary>
        public ModelArtifact Rig { get; }

        /// <summary>
        /// The clips a player character plays, a file each, none for a
        /// monster.
        /// </summary>
        public IReadOnlyList<ModelArtifact> Clips { get; }

        /// <summary>
        /// The merged glTF a monster is, null for a player character.
        /// </summary>
        public ModelArtifact Model { get; }

        /// <summary>
        /// The equipment a player character wears, one result per occupied
        /// slot, each converted for that character's framework only.
        /// </summary>
        public IReadOnlyList<ItemResult> Parts { get; }

        /// <summary>
        /// The parts a boat or a tower is built of, a rigid model each, none
        /// for any other character.
        /// </summary>
        public IReadOnlyList<ModelArtifact> MachineParts { get; }

        public override IEnumerable<ModelArtifact> Artifacts
        {
            get
            {
                if (Rig != null)
                {
                    yield return Rig;
                }

                foreach (var clip in Clips)
                {
                    yield return clip;
                }

                if (Model != null)
                {
                    yield return Model;
                }

                foreach (var artifact in Parts.SelectMany(part => part.Artifacts))
                {
                    yield return artifact;
                }

                foreach (var part in MachineParts)
                {
                    yield return part;
                }
            }
        }
    }

    /// <summary>
    /// Converts characterinfo units. Characters that share a skeleton share
    /// its rig and, when they are monsters, its merged model: the first one
    /// converts, the rest read as skipped. A boat or a tower is built of
    /// rigid parts, and each part converts once to a model of its own.
    /// </summary>
    public class CharacterConverter
    {
        private static readonly IReadOnlyList<ItemResult> NoParts = Array.Empty<ItemResult>();

        private static readonly IReadOnlyList<ClipRange> NoClips = Array.Empty<ClipRange>();

        private readonly ConversionSettings _settings;
        private readonly ClientTables _tables;
        private readonly RigConverter _rigs;
        private readonly ItemConverter _items;
        private readonly ModelConverter _models;
        private readonly TextureTranslucency _translucency;

        private readonly ArtifactMemo _merged = new ArtifactMemo();
        private readonly RigBoneCheck _boneCheck = new RigBoneCheck();

        private readonly Dictionary<int, IReadOnlyList<ClipRange>> _machineClips =
            new Dictionary<int, IReadOnlyList<ClipRange>>();

        public CharacterConverter(ConversionSettings settings, ClientTables tables, RigConverter rigs,
            ItemConverter items, ModelConverter models, TextureTranslucency translucency = null)
        {
            _settings = settings;
            _tables = tables;
            _rigs = rigs;
            _items = items;
            _models = models;
            _translucency = translucency ?? new TextureTranslucency();
        }

        public CharacterResult Convert(int id)
        {
            if (_tables.Characters == null || !_tables.Characters.TryGetById(id, out var row))
            {
                Log.Error($"no characterinfo row {id}");

                return new CharacterResult(id, null, ConversionOutcome.Failed, null, null, NoParts);
            }

            return Convert(row);
        }

        public IEnumerable<CharacterResult> ConvertModel(int model, IProgress<ConversionProgress> progress = null,
            CancellationToken cancellation = default)
        {
            var rows = _tables.CharactersOn(model);

            if (rows.Count == 0)
            {
                Log.Warning($"no characterinfo row uses model {model}");
            }

            return Run(rows, progress, cancellation);
        }

        public IEnumerable<CharacterResult> ConvertAll(IProgress<ConversionProgress> progress = null,
            CancellationToken cancellation = default)
        {
            return Run(_tables.Characters?.ToList() ?? new List<CharacterInfoRecord>(),
                progress, cancellation);
        }

        private IEnumerable<CharacterResult> Run(IReadOnlyList<CharacterInfoRecord> rows,
            IProgress<ConversionProgress> progress, CancellationToken cancellation)
        {
            return Batch.Run(rows, row => $"character {row.Id}",
                Convert,
                row => new CharacterResult(row.Id, Named(row.Id), ConversionOutcome.Failed,
                    null, null, NoParts),
                progress, cancellation);
        }

        private CharacterResult Convert(CharacterInfoRecord row)
        {
            Log.Info($"converting character {row.Id} '{row.Name}'");

            return row.ModalType switch
            {
                CharacterModalType.MainCharacter => ConvertPlayer(row),
                CharacterModalType.Other => ConvertMonster(row),
                CharacterModalType.Boat or CharacterModalType.Employee => ConvertMachine(row),
                _ => new CharacterResult(row.Id, Named(row.Id), ConversionOutcome.Skipped, null, null, NoParts)
            };
        }

        private CharacterResult ConvertPlayer(CharacterInfoRecord row)
        {
            var name = Named(row.Id);
            var rig = _rigs.Convert(row.Model);

            if (rig == null)
            {
                return new CharacterResult(row.Id, name, ConversionOutcome.Failed, null, null, NoParts);
            }

            var parts = new List<ItemResult>();

            foreach (var itemId in ClientTables.SelectDefaultItems(row).Where(itemId => itemId != 0))
            {
                if (_tables.Items == null || !_tables.Items.TryGetById(itemId, out var item))
                {
                    Log.Warning($"characterinfo {row.Id}: unknown item {itemId}");

                    continue;
                }

                var part = _items.ConvertModules(item, row.Model);

                parts.Add(part);
                ReportUnknownBones(row, rig.Rig, part.ModuleArtifacts.ElementAtOrDefault(row.Model));
            }

            if (!parts.SelectMany(part => part.Artifacts).Any())
            {
                Log.Error($"no part of character {row.Id} loaded");

                return new CharacterResult(row.Id, name, ConversionOutcome.Failed, rig, null, parts);
            }

            var outcome = rig.Rig.Outcome == ConversionOutcome.Converted ||
                          parts.Any(part => part.Outcome == ConversionOutcome.Converted)
                ? ConversionOutcome.Converted
                : ConversionOutcome.Skipped;

            return new CharacterResult(row.Id, name, outcome, rig, null, parts);
        }

        private void ReportUnknownBones(CharacterInfoRecord row, ModelArtifact rig, ModelArtifact part)
        {
            if (part == null)
            {
                return;
            }

            var unknown = _boneCheck.FindUnknownBones(rig, part);

            if (unknown.Count > 0)
            {
                Log.Warning($"characterinfo {row.Id}: part '{part.Name}' binds bones rig '{rig.Name}' does not have, " +
                            $"they bind to its first bone: {string.Join(", ", unknown)}");
            }
        }

        private CharacterResult ConvertMonster(CharacterInfoRecord row)
        {
            var name = Named(row.Model);
            var labPath = _settings.Source.Skeleton(row.Model);

            if (_merged.TryGet(labPath, out var already))
            {
                return new CharacterResult(row.Id, name, ConversionOutcome.Skipped, null, already, NoParts);
            }

            var glbPath = _settings.Output.Model(ContentKind.Character, name);

            if (!_settings.Overwrite && File.Exists(glbPath))
            {
                return Merged(row, labPath, new ModelArtifact(name, ContentKind.Character, glbPath,
                    Array.Empty<string>(), ConversionOutcome.Skipped));
            }

            if (!File.Exists(labPath))
            {
                Log.Error($"missing '{labPath}'");

                return new CharacterResult(row.Id, name, ConversionOutcome.Failed, null, null, NoParts);
            }

            BoneAnimation skeleton;

            try
            {
                using var stream = File.OpenRead(labPath);
                skeleton = LabFile.Read(stream).Animation;
            }
            catch (ParseException exception)
            {
                Log.Error($"failed to parse '{labPath}'", exception);

                return new CharacterResult(row.Id, name, ConversionOutcome.Failed, null, null, NoParts);
            }

            var siblings = _tables.CharactersOn(row.Model)
                .Where(record => record.ModalType == CharacterModalType.Other)
                .ToList();
            var parts = LoadParts(siblings[0], row.Model);

            if (parts.Count == 0)
            {
                Log.Error($"no part of model {row.Model} loaded");

                return new CharacterResult(row.Id, name, ConversionOutcome.Failed, null, null, NoParts);
            }

            var packaging = new ModelPackaging(glbPath, _settings.Output.TextureDir(ContentKind.Character));
            var textureDir = _settings.Source.TextureDir(ContentKind.Character);
            var file = GltfExport.Character(name, skeleton, parts.ToArray(),
                _tables.ActionsFor(siblings, row.Model), packaging.TextureUriPrefix,
                isTextureTranslucent: (stage, cutoff) => _translucency.IsTranslucent(textureDir, stage, cutoff));

            var packaged = packaging.Write(file, parts, textureDir);

            return Merged(row, labPath, new ModelArtifact(name, ContentKind.Character, packaged.ModelPath,
                packaged.TexturePaths, ConversionOutcome.Converted));
        }

        private CharacterResult ConvertMachine(CharacterInfoRecord row)
        {
            var name = Named(row.Id);
            var drivingSlot = row.ModalType == CharacterModalType.Boat ? BoatSlots.Driving : TowerSlots.Driving;
            var clips = DecodeMachineClips(row);
            var parts = _tables.SelectMachineModules(row)
                .Select((module, slot) => (Slot: slot, Module: module))
                .Concat(_tables.SelectShipModules(row))
                .Where(part => part.Module != null)
                .DistinctBy(part => part.Module)
                .Select(part => _models.ConvertMachinePart(part.Module, part.Slot == drivingSlot ? clips : NoClips))
                .Where(part => part != null)
                .ToList();

            if (parts.Count == 0)
            {
                Log.Error($"no part of character {row.Id} loaded");

                return new CharacterResult(row.Id, name, ConversionOutcome.Failed, null, null, NoParts);
            }

            var outcome = parts.Any(part => part.Outcome == ConversionOutcome.Converted)
                ? ConversionOutcome.Converted
                : ConversionOutcome.Skipped;

            return new CharacterResult(row.Id, name, outcome, null, null, NoParts, parts);
        }


        private IReadOnlyList<ClipRange> DecodeMachineClips(CharacterInfoRecord row)
        {
            if (_tables.Actions == null || !_tables.Actions.TryGetActions(row.ActionId, out var poses))
            {
                Log.Warning($"characterinfo {row.Id}: no action set {row.ActionId}, its parts play their whole tracks");

                return NoClips;
            }

            if (!_machineClips.TryGetValue(row.ActionId, out var clips))
            {
                clips = RigClipSet.DecodeWithoutStances(row.ActionId, poses).Clips;
                _machineClips[row.ActionId] = clips;
            }

            return clips;
        }

        private List<GeometryObject> LoadParts(CharacterInfoRecord row, int model)
        {
            var parts = new List<GeometryObject>();

            for (var slot = 0; slot < row.Parts.Length; slot++)
            {
                if (row.Parts[slot] == 0)
                {
                    continue;
                }

                var fileId = model * 1000000L + row.SuitId * 10000L + slot;
                var partPath = _settings.Source.Model("character", $"{fileId:D10}.lgo");

                if (!File.Exists(partPath))
                {
                    Log.Warning($"missing part '{partPath}'");

                    continue;
                }

                try
                {
                    using var stream = File.OpenRead(partPath);
                    var part = LgoFile.Read(stream).Object;

                    part.Id = (uint)slot;
                    parts.Add(part);
                }
                catch (ParseException exception)
                {
                    Log.Warning($"failed to parse '{partPath}'", exception);
                }
            }

            return parts;
        }

        private CharacterResult Merged(CharacterInfoRecord row, string labPath, ModelArtifact artifact)
        {
            _merged.Add(labPath, artifact);

            return new CharacterResult(row.Id, artifact.Name, artifact.Outcome, null, artifact, NoParts);
        }

        private static string Named(int id) => $"{id:D4}";
    }
}
