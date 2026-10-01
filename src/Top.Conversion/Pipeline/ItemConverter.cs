using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Top.Logging;
using Top.Legacy.Tables.Custom;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// An iteminfo unit. The item's identity plus the module each player
    /// model wears or holds it as.
    /// </summary>
    public class ItemResult : UnitResult
    {
        public ItemResult(int id, string name, ConversionOutcome outcome, bool wearable,
            IReadOnlyList<string> modules, IReadOnlyList<ModelArtifact> moduleArtifacts)
            : base(id, name, outcome)
        {
            Wearable = wearable;
            Modules = modules;
            ModuleArtifacts = moduleArtifacts;
        }

        /// <summary>
        /// Whether the item is worn as a skinned body part rather than held.
        /// </summary>
        public bool Wearable { get; }

        public IReadOnlyList<string> Modules { get; }

        public IReadOnlyList<ModelArtifact> ModuleArtifacts { get; }

        public override IEnumerable<ModelArtifact> Artifacts => ModuleArtifacts.Where(artifact => artifact != null);
    }

    /// <summary>
    /// Converts iteminfo units. An item names up to one module per player
    /// model, wearables live under the client's character models and held
    /// items under its item models, with a fallback to the other folder.
    /// Each item also brings its icon.
    /// </summary>
    public class ItemConverter
    {
        public const int Models = 4;

        private readonly ConversionSettings _settings;
        private readonly ClientTables _tables;
        private readonly ModelConverter _models;
        private readonly ItemIconWriter _icons;

        public ItemConverter(ConversionSettings settings, ClientTables tables, ModelConverter models)
        {
            _settings = settings;
            _tables = tables;
            _models = models;
            _icons = new ItemIconWriter(settings);
        }

        public ItemResult Convert(int id)
        {
            if (_tables.Items == null || !_tables.Items.TryGetById(id, out var item))
            {
                Log.Error($"no iteminfo row {id}");

                return new ItemResult(id, null, ConversionOutcome.Failed, false,
                    new string[Models], new ModelArtifact[Models]);
            }

            return ConvertItem(item);
        }

        public IEnumerable<ItemResult> ConvertAll(IProgress<ConversionProgress> progress = null,
            CancellationToken cancellation = default)
        {
            var rows = _tables.Items?.ToList() ?? new List<ItemInfoRecord>();

            return Batch.Run(rows,
                item => $"item {item.Id}",
                ConvertItem,
                item => new ItemResult(item.Id, item.Name, ConversionOutcome.Failed,
                    ItemModules.IsWearable(item.Type), new string[Models], new ModelArtifact[Models]),
                progress, cancellation);
        }

        private ItemResult ConvertItem(ItemInfoRecord item)
        {
            _icons.Write(item);

            return ConvertModules(item);
        }

        public ItemResult ConvertModules(ItemInfoRecord item, int? model = null)
        {
            Log.Info($"converting item {item.Id} '{item.Name}'");

            var wearable = ItemModules.IsWearable(item.Type);
            var modules = new string[Models];
            var artifacts = new ModelArtifact[Models];

            for (var i = 0; i < Models; i++)
            {
                if (ItemModules.TryGetModule(item, i, out var module))
                {
                    modules[i] = module;
                }
            }

            if (modules.All(module => module == null))
            {
                Log.Warning($"item {item.Id} '{item.Name}' has no models for any model");

                return new ItemResult(item.Id, item.Name, ConversionOutcome.Skipped, wearable, modules, artifacts);
            }

            if (model != null && ModuleFor(modules, model.Value) == null)
            {
                Log.Warning($"item {item.Id} '{item.Name}' has no model for model {model.Value}");

                return new ItemResult(item.Id, item.Name, ConversionOutcome.Failed, wearable, modules, artifacts);
            }

            for (var i = 0; i < Models; i++)
            {
                if (modules[i] == null || (model != null && model.Value != i))
                {
                    continue;
                }

                artifacts[i] = ConvertModule(item, wearable, modules[i]);
            }

            return new ItemResult(item.Id, item.Name, Outcome(artifacts), wearable, modules, artifacts);
        }

        private static string ModuleFor(IReadOnlyList<string> modules, int model)
        {
            return model >= 0 && model < modules.Count ? modules[model] : null;
        }

        private ModelArtifact ConvertModule(ItemInfoRecord item, bool wearable, string module)
        {
            var source = FindSource(item, wearable, module);

            if (source != null)
            {
                return _models.Convert(source);
            }

            Log.Warning($"item {item.Id} '{item.Name}': no source file for module {module}");

            return null;
        }

        private string FindSource(ItemInfoRecord item, bool wearable, string module)
        {
            var preferred = wearable ? "character" : "item";
            var other = wearable ? "item" : "character";
            var path = _settings.Source.Model(preferred, module + ".lgo");

            if (File.Exists(path))
            {
                return path;
            }

            path = _settings.Source.Model(other, module + ".lgo");

            if (!File.Exists(path))
            {
                return null;
            }

            Log.Warning($"item {item.Id} '{item.Name}' (type {item.Type}): module " +
                        $"{module} found under model/{other} instead of model/{preferred}");

            return path;
        }

        private static ConversionOutcome Outcome(IReadOnlyList<ModelArtifact> artifacts)
        {
            if (artifacts.Any(artifact => artifact?.Outcome == ConversionOutcome.Converted))
            {
                return ConversionOutcome.Converted;
            }

            return artifacts.Any(artifact => artifact != null)
                ? ConversionOutcome.Skipped
                : ConversionOutcome.Failed;
        }
    }
}
