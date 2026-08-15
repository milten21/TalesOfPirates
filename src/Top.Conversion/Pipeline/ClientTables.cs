using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Logging;
using Top.Legacy.Tables;
using Top.Legacy.Tables.Custom;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// The client tables the pipeline resolves units from, read once per run
    /// and only when a converter asks.
    /// </summary>
    public class ClientTables
    {
        private readonly Lazy<Table<CharacterInfoRecord>> _characters;
        private readonly Lazy<Table<ItemInfoRecord>> _items;
        private readonly Lazy<Table<SceneObjectInfoRecord>> _sceneObjects;
        private readonly Lazy<CharacterActionTable> _actions;
        private readonly Lazy<Table<TerrainInfoRecord>> _terrain;
        private readonly Lazy<Table<MapInfoRecord>> _maps;

        public ClientTables(ConversionSettings settings)
        {
            _characters = Deferred(() => ReadTable<CharacterInfoRecord>(settings.Source.Table("characterinfo.txt")));
            _items = Deferred(() => ReadTable<ItemInfoRecord>(settings.Source.Table("iteminfo.txt")));
            _sceneObjects = Deferred(() => ReadTable<SceneObjectInfoRecord>(settings.Source.Table("sceneobjinfo.txt")));
            _actions = Deferred(() => Read(settings.Source.CharacterAction, CharacterActionTable.Read));
            _terrain = Deferred(() => ReadTable<TerrainInfoRecord>(settings.Source.Table("terraininfo.txt")));
            _maps = Deferred(() => ReadTable<MapInfoRecord>(settings.Source.Table("mapinfo.txt")));
        }

        public ClientTables(Table<CharacterInfoRecord> characters, Table<ItemInfoRecord> items,
            Table<SceneObjectInfoRecord> sceneObjects, CharacterActionTable actions,
            Table<TerrainInfoRecord> terrain = null, Table<MapInfoRecord> maps = null)
        {
            _characters = Ready(characters);
            _items = Ready(items);
            _sceneObjects = Ready(sceneObjects);
            _actions = Ready(actions);
            _terrain = Ready(terrain);
            _maps = Ready(maps);
        }

        public Table<CharacterInfoRecord> Characters => _characters.Value;

        public Table<ItemInfoRecord> Items => _items.Value;

        public Table<SceneObjectInfoRecord> SceneObjects => _sceneObjects.Value;

        public CharacterActionTable Actions => _actions.Value;

        public Table<TerrainInfoRecord> Terrain => _terrain.Value;

        public Table<MapInfoRecord> Maps => _maps.Value;

        public IReadOnlyList<CharacterInfoRecord> CharactersOn(int model)
        {
            return Characters != null
                ? Characters.Where(record => record.Model == model).ToList()
                : Array.Empty<CharacterInfoRecord>();
        }

        public CharacterAction[] ActionsFor(IReadOnlyList<CharacterInfoRecord> rows, int model)
        {
            var actionId = rows[0].ActionId;

            foreach (var other in rows.Where(record => record.ActionId != actionId))
            {
                Log.Warning($"characterinfo {other.Id} '{other.Name}' plays " +
                            $"action set {other.ActionId} on model {model}, keeping {actionId}");
            }

            if (Actions == null)
            {
                return null;
            }

            Actions.TryGetActions(actionId, out var actions);

            return actions;
        }

        private static Lazy<T> Deferred<T>(Func<T> read) => new Lazy<T>(read);

        private static Lazy<T> Ready<T>(T table) => new Lazy<T>(() => table);

        private static Table<T> ReadTable<T>(string path) where T : TableRecord
        {
            return Read(path, TableFile.Read<T>);
        }

        private static T Read<T>(string path, Func<Stream, T> read) where T : class
        {
            if (!File.Exists(path))
            {
                Log.Warning($"missing '{path}'");

                return null;
            }

            using var stream = File.OpenRead(path);

            return read(stream);
        }
    }
}
