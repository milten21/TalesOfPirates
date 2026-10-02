using Godot;
using NUnit.Framework;
using Top.Client.Features.Maps;
using Top.Contracts.Assets.Maps;

namespace Top.Client.Tests.Features.Maps;

public class ChunkStreamerTests
{
    private const int ChunkSize = 4;

    private const int MapSize = 16;

    private const float StreamingRadius = 1f;

    private static readonly Vector2 FirstCenter = new Vector2(4f, 4f);

    private static readonly Vector2I[] ChunksAroundFirstCenter =
        [new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(0, 1), new Vector2I(1, 1)];

    private static readonly Vector2 SecondCenter = new Vector2(14f, 14f);

    private static readonly Vector2I ChunkAroundSecondCenter = new Vector2I(3, 3);

    private RecordingChunkLoader _firstLoader;

    private ChunkStreamer _streamer;

    [SetUp]
    public void CreateStreamer()
    {
        _firstLoader = new RecordingChunkLoader();
        _streamer = new ChunkStreamer(new MapGrid(new MapFile(MapSize, MapSize, ChunkSize)), StreamingRadius,
            _firstLoader);
    }

    [Test]
    public void A_loader_added_while_chunks_are_loaded_receives_a_load_for_each_loaded_chunk()
    {
        _streamer.SetCenter(FirstCenter);

        var lateLoader = new RecordingChunkLoader();

        _streamer.AddLoader(lateLoader);

        Assert.That(lateLoader.Loads, Is.EquivalentTo(ChunksAroundFirstCenter));
        Assert.That(lateLoader.Unloads, Is.Empty);
    }

    [Test]
    public void A_loader_added_late_receives_the_loads_and_unloads_of_later_moves()
    {
        _streamer.SetCenter(FirstCenter);

        var lateLoader = new RecordingChunkLoader();

        _streamer.AddLoader(lateLoader);
        lateLoader.Forget();

        _streamer.SetCenter(SecondCenter);

        Assert.That(lateLoader.Unloads, Is.EquivalentTo(ChunksAroundFirstCenter));
        Assert.That(lateLoader.Loads, Is.EqualTo(new[] { ChunkAroundSecondCenter }));
    }

    [Test]
    public void A_removed_loader_receives_an_unload_for_each_loaded_chunk()
    {
        _streamer.SetCenter(FirstCenter);
        _firstLoader.Forget();

        _streamer.RemoveLoader(_firstLoader);

        Assert.That(_firstLoader.Unloads, Is.EquivalentTo(ChunksAroundFirstCenter));
        Assert.That(_firstLoader.Loads, Is.Empty);
    }

    [Test]
    public void A_removed_loader_receives_no_more_calls()
    {
        _streamer.SetCenter(FirstCenter);
        _streamer.RemoveLoader(_firstLoader);
        _firstLoader.Forget();

        _streamer.SetCenter(SecondCenter);
        _streamer.UnloadAll();

        Assert.That(_firstLoader.Loads, Is.Empty);
        Assert.That(_firstLoader.Unloads, Is.Empty);
    }
}
