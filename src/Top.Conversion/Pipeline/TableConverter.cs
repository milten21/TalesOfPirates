using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Top.Contracts.Tables;
using Top.Conversion.Pipeline.Tables;
using Top.Logging;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// What converting one table came to.
    /// </summary>
    public class TableResult(string name, ConversionOutcome outcome) : UnitResult(0, name, outcome)
    {
        public override IEnumerable<ModelArtifact> Artifacts => [];
    }

    /// <summary>
    /// Converts table units.
    /// </summary>
    public class TableConverter(ConversionSettings settings, ClientTables tables)
    {
        private readonly TableFormat _format = new TableFormat();

        private readonly IReadOnlyList<ITableUnit> _units =
        [
            new SceneObjectTableUnit(tables),
            new TerrainTableUnit(tables),
            new MapTableUnit(tables)
        ];

        public TableResult Convert(ITableUnit unit)
        {
            var entries = unit.Entries();

            if (entries == null)
            {
                Log.Error($"no source table for '{unit.Name}'");

                return new TableResult(unit.Name, ConversionOutcome.Failed);
            }

            var path = settings.Output.At(unit.Path);

            if (!settings.Overwrite && File.Exists(path))
            {
                return new TableResult(unit.Name, ConversionOutcome.Skipped);
            }

            Log.Info($"converting table '{unit.Name}'");

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using var stream = File.Create(path);

            _format.Write(stream, entries);

            return new TableResult(unit.Name, ConversionOutcome.Converted);
        }

        public IEnumerable<TableResult> ConvertAll(IProgress<ConversionProgress> progress = null,
            CancellationToken cancellation = default)
        {
            return Batch.Run(_units,
                unit => $"table {unit.Name}",
                Convert,
                unit => new TableResult(unit.Name, ConversionOutcome.Failed),
                progress, cancellation);
        }
    }
}
