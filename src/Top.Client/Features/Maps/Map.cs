using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Top.Client.Core;
using Top.Client.Features.Maps.SceneObjects;
using Top.Client.Features.Maps.Terrain;
using Top.Client.Features.Maps.Water;
using Top.Contracts.Assets.Maps;
using Top.Contracts.Tables.World;
using Top.Logging;

namespace Top.Client.Features.Maps;

public partial class Map : Node3D
{
    private const ulong ModelCacheBudgetMicroseconds = 50000;

    private readonly List<int> _modelCacheQueue = [];

    private ServerContent _content = null!;

    private SceneObjectFactory? _modelCacheFactory;

    private TaskCompletionSource? _modelCacheWarmed;

    private ChunkStreamer? _chunkStreamer;

    [Export] public float StreamingRadius { get; set; } = 96f;

    [Export] public TerrainLoader Terrain { get; set; } = null!;

    [Export] public WaterLoader Water { get; set; } = null!;

    [Export] public SceneObjectLoader SceneObjects { get; set; } = null!;

    [Export] public DirectionalLight3D Sun { get; set; } = null!;

    public MapEntry Entry { get; private set; } = null!;

    public MapGrid Grid { get; private set; } = null!;

    public void Setup(ServerContent content)
    {
        _content = content;
    }

    public override void _Process(double delta)
    {
        AdvanceModelCache();
    }

    public async Task Load(Func<MapLookup, MapEntry> selectMap, Action<MapLoadStage>? showStage = null,
        Action<MapEntry, MapGrid>? showMap = null, CancellationToken cancellationToken = default)
    {
        await EnterStage(MapLoadStage.Tables, showStage, cancellationToken);

        var source = _content.ContentRoot.Source;
        var maps = await _content.TableReader.ReadMaps(cancellationToken);
        var mapEntry = selectMap(new MapLookup(maps, source));
        var terrains = await _content.TableReader.ReadTerrains(cancellationToken);
        var sceneObjects = await _content.TableReader.ReadSceneObjects(cancellationToken);

        await EnterStage(MapLoadStage.Materials, showStage, cancellationToken);

        var terrainMaterial = await TerrainMaterialReader.Read(source, terrains, cancellationToken);
        var waterMaterial = await WaterMaterialReader.Read(source, cancellationToken);

        await EnterStage(MapLoadStage.MapFile, showStage, cancellationToken);

        var mapGrid = await new MapReader(source).Read(mapEntry.MapPath, cancellationToken);

        showMap?.Invoke(mapEntry, mapGrid);

        await EnterStage(MapLoadStage.ModelCache, showStage, cancellationToken);

        var sceneObjectFactory = new SceneObjectFactory(sceneObjects, _content.ModelFactory);

        await WarmModelCache(mapGrid, sceneObjectFactory, cancellationToken);

        await EnterStage(MapLoadStage.TerrainAndWater, showStage, cancellationToken);

        Clear();

        Terrain.Initialize(mapGrid, terrainMaterial);
        Water.Initialize(mapGrid, waterMaterial);
        SceneObjects.Initialize(mapGrid, sceneObjectFactory);

        _chunkStreamer = new ChunkStreamer(mapGrid, StreamingRadius, Terrain, Water, SceneObjects);

        Entry = mapEntry;
        Grid = mapGrid;

        ApplySunlight(mapEntry);
    }

    public void Clear()
    {
        _chunkStreamer?.UnloadAll();
        _chunkStreamer = null;
    }

    public void AddChunkLoader(IChunkLoader chunkLoader)
    {
        _chunkStreamer?.AddLoader(chunkLoader);
    }

    public void RemoveChunkLoader(IChunkLoader chunkLoader)
    {
        _chunkStreamer?.RemoveLoader(chunkLoader);
    }

    public void SetStreamingCenter(Vector3 worldPosition)
    {
        _chunkStreamer?.SetCenter(MapSpace.ToMap(worldPosition));
    }

    private static HashSet<int> CollectModelIds(MapGrid mapGrid)
    {
        var ids = new HashSet<int>();

        for (var chunkY = 0; chunkY < mapGrid.ChunkCountY; chunkY++)
        {
            for (var chunkX = 0; chunkX < mapGrid.ChunkCountX; chunkX++)
            {
                foreach (var placement in mapGrid.PlacementsAt(chunkX, chunkY))
                {
                    if (placement.Kind == PlacementKind.Model)
                    {
                        ids.Add(placement.Id);
                    }
                }
            }
        }

        return ids;
    }

    private async Task EnterStage(MapLoadStage stage, Action<MapLoadStage>? showStage,
        CancellationToken cancellationToken)
    {
        showStage?.Invoke(stage);

        cancellationToken.ThrowIfCancellationRequested();

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task WarmModelCache(MapGrid mapGrid, SceneObjectFactory sceneObjectFactory,
        CancellationToken cancellationToken)
    {
        _modelCacheQueue.Clear();
        _modelCacheQueue.AddRange(CollectModelIds(mapGrid));
        _modelCacheFactory = sceneObjectFactory;

        var warmed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _modelCacheWarmed = warmed;

        try
        {
            using (cancellationToken.Register(() => warmed.TrySetCanceled(cancellationToken)))
            {
                await warmed.Task;
            }
        }
        finally
        {
            _modelCacheWarmed = null;
            _modelCacheFactory = null;
            _modelCacheQueue.Clear();
        }
    }

    private void AdvanceModelCache()
    {
        if (_modelCacheWarmed is not { Task.IsCompleted: false } warmed)
        {
            return;
        }

        var deadline = Time.GetTicksUsec() + ModelCacheBudgetMicroseconds;

        while (_modelCacheQueue.Count > 0 && Time.GetTicksUsec() < deadline)
        {
            PreloadSceneObject(_modelCacheQueue[^1]);

            _modelCacheQueue.RemoveAt(_modelCacheQueue.Count - 1);
        }

        if (_modelCacheQueue.Count == 0)
        {
            warmed.TrySetResult();
        }
    }

    private void PreloadSceneObject(int id)
    {
        try
        {
            _modelCacheFactory!.Preload(id);
        }
        catch (Exception exception)
        {
            Log.Error($"scene object {id} broke while loading", exception);
        }
    }

    private void ApplySunlight(MapEntry mapEntry)
    {
        if (Sun == null)
        {
            return;
        }

        var direction = MapSpace
            .ToWorld(mapEntry.LightDirection.X, mapEntry.LightDirection.Y, mapEntry.LightDirection.Z);

        if (!direction.IsZeroApprox() && !direction.Cross(Vector3.Up).IsZeroApprox())
        {
            Sun.GlobalBasis = Basis.LookingAt(direction);
        }

        Sun.LightColor = Color.Color8(mapEntry.LightColor.R, mapEntry.LightColor.G, mapEntry.LightColor.B);
    }
}
