using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Top.Content;
using Top.Contracts.Tables;
using Top.Contracts.Tables.Characters;
using Top.Contracts.Tables.Items;
using Top.Contracts.Tables.Rigs;
using Top.Contracts.Tables.Ships;
using Top.Contracts.Tables.World;

namespace Top.Client.Core;

public class TableReader(IContentSource contentSource)
{
    private readonly TableFormat _format = new TableFormat();

    private readonly Dictionary<string, object> _reads = new Dictionary<string, object>();

    public Task<SceneObjectTable> ReadSceneObjects(CancellationToken cancellationToken = default)
    {
        return Resolve<SceneObjectEntry, SceneObjectTable>(SceneObjectTable.Path,
            entries => new SceneObjectTable(entries), cancellationToken);
    }

    public Task<TerrainTable> ReadTerrains(CancellationToken cancellationToken = default)
    {
        return Resolve<TerrainEntry, TerrainTable>(TerrainTable.Path,
            entries => new TerrainTable(entries), cancellationToken);
    }

    public Task<MapTable> ReadMaps(CancellationToken cancellationToken = default)
    {
        return Resolve<MapEntry, MapTable>(MapTable.Path,
            entries => new MapTable(entries), cancellationToken);
    }

    public Task<AreaTable> ReadAreas(CancellationToken cancellationToken = default)
    {
        return Resolve<AreaEntry, AreaTable>(AreaTable.Path,
            entries => new AreaTable(entries), cancellationToken);
    }

    public Task<TipTable> ReadTips(CancellationToken cancellationToken = default)
    {
        return Resolve<TipEntry, TipTable>(TipTable.Path,
            entries => new TipTable(entries), cancellationToken, isOptional: true);
    }

    public Task<CharacterTable> ReadCharacters(CancellationToken cancellationToken = default)
    {
        return Resolve<CharacterEntry, CharacterTable>(CharacterTable.Path,
            entries => new CharacterTable(entries), cancellationToken);
    }

    public Task<JobTable> ReadJobs(CancellationToken cancellationToken = default)
    {
        return Resolve<JobEntry, JobTable>(JobTable.Path,
            entries => new JobTable(entries), cancellationToken);
    }

    public Task<PortraitCameraTable> ReadPortraitCameras(CancellationToken cancellationToken = default)
    {
        return Resolve<PortraitCameraEntry, PortraitCameraTable>(PortraitCameraTable.Path,
            entries => new PortraitCameraTable(entries), cancellationToken);
    }

    public Task<ItemTable> ReadItems(CancellationToken cancellationToken = default)
    {
        return Resolve<ItemEntry, ItemTable>(ItemTable.Path,
            entries => new ItemTable(entries), cancellationToken);
    }

    public Task<ItemTypeTable> ReadItemTypes(CancellationToken cancellationToken = default)
    {
        return Resolve<ItemTypeEntry, ItemTypeTable>(ItemTypeTable.Path,
            entries => new ItemTypeTable(entries), cancellationToken);
    }

    public Task<ItemPrefixTable> ReadItemPrefixes(CancellationToken cancellationToken = default)
    {
        return Resolve<ItemPrefixEntry, ItemPrefixTable>(ItemPrefixTable.Path,
            entries => new ItemPrefixTable(entries), cancellationToken);
    }

    public Task<GemTable> ReadGems(CancellationToken cancellationToken = default)
    {
        return Resolve<GemEntry, GemTable>(GemTable.Path,
            entries => new GemTable(entries), cancellationToken);
    }

    public Task<RigTable> ReadRigs(CancellationToken cancellationToken = default)
    {
        return Resolve<RigEntry, RigTable>(RigTable.Path,
            entries => new RigTable(entries), cancellationToken);
    }

    public Task<ActionSetTable> ReadActionSets(CancellationToken cancellationToken = default)
    {
        return Resolve<RigEntry, ActionSetTable>(ActionSetTable.Path,
            entries => new ActionSetTable(entries), cancellationToken);
    }

    public Task<ShipTable> ReadShips(CancellationToken cancellationToken = default)
    {
        return Resolve<ShipEntry, ShipTable>(ShipTable.Path,
            entries => new ShipTable(entries), cancellationToken);
    }

    public Task<ShipPartTable> ReadShipParts(CancellationToken cancellationToken = default)
    {
        return Resolve<ShipPartEntry, ShipPartTable>(ShipPartTable.Path,
            entries => new ShipPartTable(entries), cancellationToken);
    }

    private Task<TTable> Resolve<TEntry, TTable>(string path, Func<List<TEntry>, TTable> build,
        CancellationToken cancellationToken, bool isOptional = false)
        where TEntry : TableEntry where TTable : Table<TEntry>
    {
        if (_reads.TryGetValue(path, out var known) && !((Task)known).IsFaulted)
        {
            return ((Task<TTable>)known).WaitAsync(cancellationToken);
        }

        var reading = Read(path, build, isOptional);

        _reads[path] = reading;

        return reading.WaitAsync(cancellationToken);
    }

    private async Task<TTable> Read<TEntry, TTable>(string path, Func<List<TEntry>, TTable> build, bool isOptional)
        where TEntry : TableEntry where TTable : Table<TEntry>
    {
        if (isOptional && !contentSource.Exists(path))
        {
            return build([]);
        }

        using var stream = new MemoryStream(await contentSource.Read(path));

        return build(_format.Read<TEntry>(stream));
    }
}
