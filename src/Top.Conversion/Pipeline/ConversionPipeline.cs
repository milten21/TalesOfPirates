using System;
using System.Collections.Generic;
using System.Threading;
using Top.Conversion.Textures;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// One run's converters, wired together. What they already converted is
    /// remembered for the life of the pipeline, so a batch builds each shared
    /// rig, module and merged monster once however many units name it.
    /// </summary>
    public class ConversionPipeline
    {
        public ConversionPipeline(ConversionSettings settings)
            : this(settings, new ClientTables(settings))
        {
        }

        public ConversionPipeline(ConversionSettings settings, ClientTables tables)
        {
            Settings = settings;
            ClientTables = tables;
            var translucency = new TextureTranslucency();

            Models = new ModelConverter(settings, tables, translucency);
            Rigs = new RigConverter(settings, tables);
            Items = new ItemConverter(settings, tables, Models);
            SceneObjects = new SceneObjectConverter(settings, tables, Models);
            Characters = new CharacterConverter(settings, tables, Rigs, Items, Models, translucency);
            Tables = new TableConverter(settings, tables);
            Maps = new MapConverter(settings, tables);
            Cursors = new CursorConverter(settings);
        }

        public ConversionSettings Settings { get; }

        public ClientTables ClientTables { get; }

        public ModelConverter Models { get; }

        public RigConverter Rigs { get; }

        public ItemConverter Items { get; }

        public SceneObjectConverter SceneObjects { get; }

        public CharacterConverter Characters { get; }

        public TableConverter Tables { get; }

        public MapConverter Maps { get; }

        public CursorConverter Cursors { get; }

        public IEnumerable<UnitResult> ConvertAll(string kind, IProgress<ConversionProgress> progress = null,
            CancellationToken cancellation = default)
        {
            return kind switch
            {
                ContentKind.Character => Characters.ConvertAll(progress, cancellation),
                ContentKind.Item => Items.ConvertAll(progress, cancellation),
                ContentKind.Scene => SceneObjects.ConvertAll(progress, cancellation),
                ContentKind.Table => Tables.ConvertAll(progress, cancellation),
                ContentKind.Map => Maps.ConvertAll(progress, cancellation),
                ContentKind.Cursor => Cursors.ConvertAll(progress, cancellation),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }
}
