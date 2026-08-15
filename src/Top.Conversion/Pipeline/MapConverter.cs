using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Top.Conversion.Pipeline.Maps;
using Top.Logging;
using Original = Top.Legacy.MindPower.World;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// What converting one map came to.
    /// </summary>
    public class MapResult(string name, ConversionOutcome outcome) : UnitResult(0, name, outcome)
    {
        public override IEnumerable<ModelArtifact> Artifacts => [];
    }

    /// <summary>
    /// Converts each .map in the client's map folder, with the .obj beside it,
    /// to maps/name.map, and writes the textures the map paints with.
    /// </summary>
    public class MapConverter(ConversionSettings settings, ClientTables tables)
    {
        private readonly TerrainTextureWriter _textureWriter = new TerrainTextureWriter(settings);

        public MapResult Convert(string name)
        {
            var path = settings.Output.Map(name);

            if (!settings.Overwrite && File.Exists(path))
            {
                return new MapResult(name, ConversionOutcome.Skipped);
            }

            var terrainPath = settings.Source.MapTerrain(name);

            if (!File.Exists(terrainPath))
            {
                Log.Error($"missing '{terrainPath}'");

                return new MapResult(name, ConversionOutcome.Failed);
            }

            Log.Info($"converting map '{name}'");

            var map = new MapBuilder(ReadTerrain(terrainPath), ReadObjects(name), tables.Terrain).Build();

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            Write(map, path);
            _textureWriter.Write(tables.Terrain);

            return new MapResult(name, ConversionOutcome.Converted);
        }

        public IEnumerable<MapResult> ConvertAll(IProgress<ConversionProgress> progress = null,
            CancellationToken cancellation = default)
        {
            return Batch.Run(Names(),
                name => $"map {name}",
                Convert,
                name => new MapResult(name, ConversionOutcome.Failed),
                progress, cancellation);
        }

        private static Original.MapFile ReadTerrain(string path)
        {
            using var stream = File.OpenRead(path);

            return Original.MapFile.Read(stream);
        }

        /// <summary>
        /// Removes the partial file when a write fails, so nothing truncated
        /// stays in the tree.
        /// </summary>
        private static void Write(Contracts.Assets.Maps.MapFile map, string path)
        {
            try
            {
                using var output = File.Create(path);

                map.Write(output);
            }
            catch
            {
                File.Delete(path);

                throw;
            }
        }

        private IReadOnlyList<string> Names()
        {
            if (!Directory.Exists(settings.Source.Maps))
            {
                Log.Error($"missing '{settings.Source.Maps}'");

                return [];
            }

            return Directory.GetFiles(settings.Source.Maps, "*.map")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private Original.ObjFile ReadObjects(string name)
        {
            var path = settings.Source.MapObjects(name);

            if (!File.Exists(path))
            {
                Log.Warning($"no '{path}', converting without placements");

                return null;
            }

            using var stream = File.OpenRead(path);

            return Original.ObjFile.Read(stream);
        }
    }
}
