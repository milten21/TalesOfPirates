using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Logging;
using Top.Legacy.Tables;
using Top.Legacy.Tables.Custom;
using Top.Legacy.Tables.Records;
using BoatSlots = Top.Contracts.Tables.Characters.BoatSlots;
using TowerSlots = Top.Contracts.Tables.Characters.TowerSlots;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// The client tables the pipeline resolves units from, read once per run
    /// and only when a converter asks.
    /// </summary>
    public class ClientTables
    {
        private const int BodyLinkCount = 5;

        private const string NoModule = "0";

        private static readonly IReadOnlyCollection<int> NoBodyTypes = Array.Empty<int>();

        private readonly Lazy<Table<CharacterInfoRecord>> _characters;
        private readonly Lazy<Table<ItemInfoRecord>> _items;
        private readonly Lazy<Table<SceneObjectInfoRecord>> _sceneObjects;
        private readonly Lazy<CharacterActionTable> _actions;
        private readonly Lazy<Table<TerrainInfoRecord>> _terrain;
        private readonly Lazy<Table<MapInfoRecord>> _maps;
        private readonly Lazy<Table<ShipInfoRecord>> _ships;
        private readonly Lazy<Table<ShipItemInfoRecord>> _shipParts;
        private readonly Lazy<Table<AreaRecord>> _areas;
        private readonly Lazy<TipsList> _tips;
        private readonly Lazy<ClientAttributeScript> _clientAttributes;
        private readonly Lazy<Table<StoneInfoRecord>> _stones;
        private readonly Lazy<Table<ItemPreRecord>> _itemPrefixes;
        private readonly Lazy<GemHintScript> _gemHints;
        private readonly Lazy<Dictionary<string, SortedSet<int>>> _bodyTypesByModule;

        public ClientTables(ConversionSettings settings)
        {
            _characters = Deferred(() => ReadTable<CharacterInfoRecord>(settings.Source.Table("characterinfo.txt")));
            _items = Deferred(() => ReadTable<ItemInfoRecord>(settings.Source.Table("iteminfo.txt")));
            _sceneObjects = Deferred(() => ReadTable<SceneObjectInfoRecord>(settings.Source.Table("sceneobjinfo.txt")));
            _actions = Deferred(() => Read(settings.Source.CharacterAction, CharacterActionTable.Read));
            _terrain = Deferred(() => ReadTable<TerrainInfoRecord>(settings.Source.Table("terraininfo.txt")));
            _maps = Deferred(() => ReadTable<MapInfoRecord>(settings.Source.Table("mapinfo.txt")));
            _ships = Deferred(() => ReadTable<ShipInfoRecord>(settings.Source.Table("shipinfo.txt")));
            _shipParts = Deferred(() => ReadTable<ShipItemInfoRecord>(settings.Source.Table("shipiteminfo.txt")));
            _areas = Deferred(() => ReadTable<AreaRecord>(settings.Source.Table("areaset.txt")));
            _tips = Deferred(() => Read(settings.Source.Tips, TipsList.Read));
            _clientAttributes = Deferred(() => Read(settings.Source.ClientAttributes, ClientAttributeScript.Read));
            _stones = Deferred(() => ReadTable<StoneInfoRecord>(settings.Source.Table("stoneinfo.txt")));
            _itemPrefixes = Deferred(() => ReadTable<ItemPreRecord>(settings.Source.Table("itempre.txt")));
            _gemHints = Deferred(() => Read(settings.Source.GemHints, GemHintScript.Read));
            _bodyTypesByModule = Deferred(IndexBodyTypesByModule);
        }

        public ClientTables(Table<CharacterInfoRecord> characters, Table<ItemInfoRecord> items,
            Table<SceneObjectInfoRecord> sceneObjects, CharacterActionTable actions,
            Table<TerrainInfoRecord> terrain = null, Table<MapInfoRecord> maps = null,
            Table<ShipInfoRecord> ships = null, Table<ShipItemInfoRecord> shipParts = null,
            Table<AreaRecord> areas = null, TipsList tips = null, ClientAttributeScript clientAttributes = null,
            Table<StoneInfoRecord> stones = null, Table<ItemPreRecord> itemPrefixes = null,
            GemHintScript gemHints = null)
        {
            _characters = Ready(characters);
            _items = Ready(items);
            _sceneObjects = Ready(sceneObjects);
            _actions = Ready(actions);
            _terrain = Ready(terrain);
            _maps = Ready(maps);
            _ships = Ready(ships);
            _shipParts = Ready(shipParts);
            _areas = Ready(areas);
            _tips = Ready(tips);
            _clientAttributes = Ready(clientAttributes);
            _stones = Ready(stones);
            _itemPrefixes = Ready(itemPrefixes);
            _gemHints = Ready(gemHints);
            _bodyTypesByModule = Deferred(IndexBodyTypesByModule);
        }

        public Table<CharacterInfoRecord> Characters => _characters.Value;

        public Table<ItemInfoRecord> Items => _items.Value;

        public Table<SceneObjectInfoRecord> SceneObjects => _sceneObjects.Value;

        public CharacterActionTable Actions => _actions.Value;

        public Table<TerrainInfoRecord> Terrain => _terrain.Value;

        public Table<MapInfoRecord> Maps => _maps.Value;

        public Table<ShipInfoRecord> Ships => _ships.Value;

        public Table<ShipItemInfoRecord> ShipParts => _shipParts.Value;

        public Table<AreaRecord> Areas => _areas.Value;

        public TipsList Tips => _tips.Value;

        public ClientAttributeScript ClientAttributes => _clientAttributes.Value;

        public Table<StoneInfoRecord> Stones => _stones.Value;

        public Table<ItemPreRecord> ItemPrefixes => _itemPrefixes.Value;

        public GemHintScript GemHints => _gemHints.Value;

        public IReadOnlyList<CharacterInfoRecord> CharactersOn(int model)
        {
            return Characters != null
                ? Characters.Where(record => record.Model == model).ToList()
                : Array.Empty<CharacterInfoRecord>();
        }

        public IReadOnlyCollection<int> FindBodyTypesWearing(string module)
        {
            return _bodyTypesByModule.Value.TryGetValue(module, out var bodyTypes) ? bodyTypes : NoBodyTypes;
        }

        public static int[] SelectDefaultItems(CharacterInfoRecord row)
        {
            var items = new int[BodyLinkCount];

            row.Parts?.Take(BodyLinkCount).ToArray().CopyTo(items, 0);

            return items;
        }

        public string[] SelectMachineModules(CharacterInfoRecord row)
        {
            return row.ModalType == CharacterModalType.Boat ? SelectBoatModules(row) : SelectTowerModules(row);
        }

        public IEnumerable<(int Slot, string Module)> SelectShipModules(CharacterInfoRecord row)
        {
            foreach (var ship in Ships?.Where(ship => ship.CharacterId == row.Id) ?? [])
            {
                yield return (BoatSlots.Hull, FindShipModule(ship.HullId));

                foreach (var engineId in ship.EngineOptions ?? [])
                {
                    yield return (BoatSlots.Engine, FindShipModule(engineId));

                    var propellers = FindShipPart(engineId)?.Motors ?? [];

                    for (var propeller = 0; propeller < propellers.Length; propeller++)
                    {
                        yield return (BoatSlots.FirstPropeller + propeller, FindShipModule(propellers[propeller]));
                    }
                }

                foreach (var bowId in ship.HeadOptions ?? [])
                {
                    yield return (BoatSlots.Bow, FindShipModule(bowId));
                }
            }
        }

        public static string FindShipModule(ShipItemInfoRecord part)
        {
            return part?.ModelId > 0 ? $"{part.ModelId:D10}" : null;
        }

        private string FindShipModule(int partId)
        {
            return FindShipModule(FindShipPart(partId));
        }

        private ShipItemInfoRecord FindShipPart(int partId)
        {
            return ShipParts != null && ShipParts.TryGetById(partId, out var part) ? part : null;
        }

        private string[] SelectTowerModules(CharacterInfoRecord row)
        {
            var modules = new string[TowerSlots.Count];

            for (var slot = 0; slot < modules.Length; slot++)
            {
                modules[slot] = FindModelModule(FindMachineItem(row, slot));
            }

            return modules;
        }

        private string[] SelectBoatModules(CharacterInfoRecord row)
        {
            var modules = new string[BoatSlots.Count];
            var hull = FindMachineItem(row, BoatSlots.Hull);
            var engine = FindMachineItem(row, BoatSlots.Engine);

            modules[BoatSlots.Hull] = FindModelModule(hull);
            modules[BoatSlots.Bow] = FindModelModule(FindMachineItem(row, BoatSlots.Bow));
            modules[BoatSlots.Engine] = FindModelModule(engine);
            modules[BoatSlots.Flag] = FindModelModule(FindItem(row, hull?.ShipFlag ?? 0));

            for (var propeller = 0; propeller < BoatSlots.PropellerCount; propeller++)
            {
                var itemId = int.TryParse(engine?.Modules?.ElementAtOrDefault(propeller + 1), out var id) ? id : 0;

                modules[BoatSlots.FirstPropeller + propeller] = FindModelModule(FindItem(row, itemId));
            }

            return modules;
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

        private static string FindModelModule(ItemInfoRecord item)
        {
            return item?.Modules?.FirstOrDefault() is { } module && module != NoModule ? module : null;
        }

        private ItemInfoRecord FindMachineItem(CharacterInfoRecord row, int slot)
        {
            return FindItem(row, row.Parts?.ElementAtOrDefault(slot) ?? 0);
        }

        private ItemInfoRecord FindItem(CharacterInfoRecord row, int itemId)
        {
            if (itemId == 0)
            {
                return null;
            }

            if (Items != null && Items.TryGetById(itemId, out var item))
            {
                return item;
            }

            Log.Warning($"characterinfo {row.Id}: unknown item {itemId}");

            return null;
        }

        private Dictionary<string, SortedSet<int>> IndexBodyTypesByModule()
        {
            var index = new Dictionary<string, SortedSet<int>>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in Items?.Where(item => ItemModules.IsWearable(item.Type)) ?? [])
            {
                for (var bodyType = 0; bodyType < ItemConverter.Models; bodyType++)
                {
                    if (!ItemModules.CanEquip(item, bodyType) || !ItemModules.TryGetModule(item, bodyType, out var module))
                    {
                        continue;
                    }

                    if (!index.TryGetValue(module, out var bodyTypes))
                    {
                        bodyTypes = new SortedSet<int>();
                        index[module] = bodyTypes;
                    }

                    bodyTypes.Add(bodyType);
                }
            }

            return index;
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
