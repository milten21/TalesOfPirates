using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Top.Conversion.Pipeline;
using Top.Legacy.MindPower;

namespace Top.Conversion.Tests.Pipeline
{
    public class CursorConverterTests
    {
        private static readonly Rgba32 Red = new Rgba32(255, 0, 0, 255);

        private static readonly Rgba32 Green = new Rgba32(0, 255, 0, 255);

        private static readonly Rgba32 Blue = new Rgba32(0, 0, 255, 255);

        private FakeClient _client;
        private CaptureLog _log;

        [SetUp]
        public void SetUp()
        {
            _client = new FakeClient();
            _log = new CaptureLog();
        }

        [TearDown]
        public void TearDown()
        {
            _log.Dispose();
            _client.Dispose();
        }

        private void AddCursor(string fileName, byte[] bytes)
        {
            var path = Path.Combine(_client.ClientRoot, "cursor", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
        }

        private UnitResult[] Convert()
        {
            return new ConversionPipeline(_client.Settings()).ConvertAll(ContentKind.Cursor).ToArray();
        }

        private string StripPath(string key)
        {
            return Path.Combine(_client.OutputRoot, "cursors", key + ".png");
        }

        private JObject ReadCursors()
        {
            return (JObject)JObject.Parse(File.ReadAllText(Path.Combine(_client.OutputRoot, "cursors.json")))
                ["cursors"];
        }

        [Test]
        public void A_single_frame_cursor_becomes_a_strip_of_one_frame()
        {
            AddCursor("normal.ani", AnimatedCursorBytes.Write(new AnimatedCursorFrame(Red)));

            Convert();

            var strip = PngPixels.Read(StripPath("normal"));

            Assert.That((strip.Width, strip.Height), Is.EqualTo((AnimatedCursorBytes.FrameSize, AnimatedCursorBytes.FrameSize)));
            Assert.That(strip.At(0, 0), Is.EqualTo(Red));
            Assert.That(JToken.DeepEquals(ReadCursors()["normal"],
                JObject.Parse("""{ "image": "cursors/normal.png", "frame": [4, 4], "hotspot": [0, 0] }""")));
        }

        [Test]
        public void Each_frame_is_stored_once_from_left_to_right()
        {
            AddCursor("chat.ani", AnimatedCursorBytes.Write(new AnimatedCursorFrame(Red), new AnimatedCursorFrame(Green), new AnimatedCursorFrame(Blue)));

            Convert();

            var strip = PngPixels.Read(StripPath("chat"));

            Assert.That((strip.Width, strip.Height), Is.EqualTo((AnimatedCursorBytes.FrameSize * 3, AnimatedCursorBytes.FrameSize)));
            Assert.That(strip.At(0, 0), Is.EqualTo(Red));
            Assert.That(strip.At(AnimatedCursorBytes.FrameSize, 0), Is.EqualTo(Green));
            Assert.That(strip.At(AnimatedCursorBytes.FrameSize * 2, 0), Is.EqualTo(Blue));
            Assert.That(ReadCursors()["chat"]!["frame"]!.ToObject<int[]>(),
                Is.EqualTo(new[] { AnimatedCursorBytes.FrameSize, AnimatedCursorBytes.FrameSize }));
        }

        private static AnimatedCursorFrame[] RedGreenRed()
        {
            return [new AnimatedCursorFrame(Red), new AnimatedCursorFrame(Green), new AnimatedCursorFrame(Red)];
        }

        [Test]
        public void The_seq_chunk_becomes_the_steps_over_the_unique_frames()
        {
            AddCursor("mouseright.ani", AnimatedCursorBytes.WriteTimed(RedGreenRed(), steps: [0, 1, 2, 1]));

            Convert();

            var strip = PngPixels.Read(StripPath("player"));

            Assert.That(strip.Width, Is.EqualTo(AnimatedCursorBytes.FrameSize * 2));
            Assert.That(strip.At(AnimatedCursorBytes.FrameSize, 0), Is.EqualTo(Green));
            Assert.That(ReadCursors()["player"]!["steps"]!.ToObject<int[]>(), Is.EqualTo(new[] { 0, 1, 0, 1 }));
        }

        [Test]
        public void A_step_may_repeat_a_frame_that_has_no_twin()
        {
            AddCursor("chat.ani", AnimatedCursorBytes.WriteTimed(
                [new AnimatedCursorFrame(Red), new AnimatedCursorFrame(Green)], steps: [0, 1, 1]));

            Convert();

            Assert.That(ReadCursors()["chat"]!["steps"]!.ToObject<int[]>(), Is.EqualTo(new[] { 0, 1, 1 }));
        }

        [Test]
        public void Without_a_seq_chunk_every_frame_is_stored_and_no_steps_are_written()
        {
            AddCursor("mouseright.ani", AnimatedCursorBytes.Write(RedGreenRed()));

            Convert();

            Assert.That(PngPixels.Read(StripPath("player")).Width, Is.EqualTo(AnimatedCursorBytes.FrameSize * 3));
            Assert.That(ReadCursors()["player"]!["steps"], Is.Null);
        }

        [Test]
        public void The_rate_chunk_becomes_milliseconds_for_each_step()
        {
            AddCursor("search.ani", AnimatedCursorBytes.WriteTimed(
                [new AnimatedCursorFrame(Red), new AnimatedCursorFrame(Green), new AnimatedCursorFrame(Blue)],
                rates: [6, 3, 17]));

            Convert();

            Assert.That(ReadCursors()["search"]!["ms"]!.ToObject<int[]>(), Is.EqualTo(new[] { 100, 50, 283 }));
        }

        [Test]
        public void Steps_with_one_duration_get_one_value()
        {
            AddCursor("search.ani", AnimatedCursorBytes.WriteTimed(
                [new AnimatedCursorFrame(Red), new AnimatedCursorFrame(Green)], rates: [10, 10]));

            Convert();

            Assert.That(ReadCursors()["search"]!["ms"]!.Type, Is.EqualTo(JTokenType.Integer));
            Assert.That(ReadCursors()["search"]!["ms"]!.ToObject<int>(), Is.EqualTo(167));
        }

        [Test]
        public void Without_a_rate_chunk_the_default_rate_of_the_header_is_used()
        {
            AddCursor("search.ani", AnimatedCursorBytes.WriteTimed(
                [new AnimatedCursorFrame(Red), new AnimatedCursorFrame(Green)], defaultRate: 3));

            Convert();

            Assert.That(ReadCursors()["search"]!["ms"]!.ToObject<int>(), Is.EqualTo(50));
        }

        [Test]
        public void The_rate_chunk_follows_the_steps_and_not_the_frames()
        {
            AddCursor("search.ani", AnimatedCursorBytes.WriteTimed(
                [new AnimatedCursorFrame(Red), new AnimatedCursorFrame(Green)], steps: [0, 1, 0],
                rates: [6, 3, 12]));

            Convert();

            Assert.That(ReadCursors()["search"]!["ms"]!.ToObject<int[]>(), Is.EqualTo(new[] { 100, 50, 200 }));
        }

        [Test]
        public void A_cursor_of_one_step_has_no_steps_and_no_duration()
        {
            AddCursor("normal.ani", AnimatedCursorBytes.WriteTimed([new AnimatedCursorFrame(Red)], steps: [0],
                rates: [10]));

            Convert();

            Assert.That(ReadCursors()["normal"]!["steps"], Is.Null);
            Assert.That(ReadCursors()["normal"]!["ms"], Is.Null);
        }

        [Test]
        public void A_rate_chunk_that_does_not_match_the_steps_fails_the_cursor()
        {
            AddCursor("search.ani", AnimatedCursorBytes.WriteTimed(
                [new AnimatedCursorFrame(Red), new AnimatedCursorFrame(Green)], rates: [10]));

            var results = Convert();

            Assert.That(results.Single(result => result.Name == "search").Outcome,
                Is.EqualTo(ConversionOutcome.Failed));
        }

        [Test]
        public void A_seq_chunk_that_names_a_missing_frame_fails_the_cursor()
        {
            AddCursor("search.ani", AnimatedCursorBytes.WriteTimed([new AnimatedCursorFrame(Red)], steps: [0, 1]));

            var results = Convert();

            Assert.That(results.Single(result => result.Name == "search").Outcome,
                Is.EqualTo(ConversionOutcome.Failed));
        }

        [Test]
        public void The_hotspot_comes_from_the_cursor_frame()
        {
            AddCursor("drag.ani", AnimatedCursorBytes.Write(new AnimatedCursorFrame(Red, hotspotX: 2, hotspotY: 1)));

            Convert();

            Assert.That(ReadCursors()["drag"]!["hotspot"]!.ToObject<int[]>(), Is.EqualTo(new[] { 2, 1 }));
        }

        [Test]
        public void Frames_with_different_hotspots_give_a_hotspot_for_each_frame()
        {
            AddCursor("repair.ani", AnimatedCursorBytes.Write(new AnimatedCursorFrame(Red, hotspotX: 2, hotspotY: 1),
                new AnimatedCursorFrame(Green, hotspotX: 1, hotspotY: 3)));

            Convert();

            Assert.That(ReadCursors()["repair"]!["hotspot"]!.ToObject<int[][]>(),
                Is.EqualTo(new[] { new[] { 2, 1 }, new[] { 1, 3 } }));
        }

        [Test]
        public void Frames_with_the_same_hotspot_give_one_hotspot()
        {
            AddCursor("repair.ani", AnimatedCursorBytes.Write(new AnimatedCursorFrame(Red, hotspotX: 2, hotspotY: 1),
                new AnimatedCursorFrame(Green, hotspotX: 2, hotspotY: 1)));

            Convert();

            Assert.That(ReadCursors()["repair"]!["hotspot"]!.ToObject<int[]>(), Is.EqualTo(new[] { 2, 1 }));
        }

        [Test]
        public void Frames_that_differ_only_in_hotspot_are_two_frames()
        {
            AddCursor("repair.ani", AnimatedCursorBytes.WriteTimed(
                [new AnimatedCursorFrame(Red, hotspotX: 2), new AnimatedCursorFrame(Red, hotspotX: 1)],
                steps: [0, 1]));

            Convert();

            Assert.That(PngPixels.Read(StripPath("repair")).Width, Is.EqualTo(AnimatedCursorBytes.FrameSize * 2));
        }

        [Test]
        public void A_plain_icon_file_converts_with_the_hotspot_that_windows_reads_from_its_fields()
        {
            AddCursor("block.ani", AnimatedCursorBytes.WriteIcon(new AnimatedCursorFrame(Green, hotspotX: 1,
                hotspotY: 3, type: AnimatedCursorFrame.IconType)));

            var results = Convert();

            Assert.That(results.Single(result => result.Name == "block").Outcome,
                Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(PngPixels.Read(StripPath("block")).At(0, 0), Is.EqualTo(Green));
            Assert.That(JToken.DeepEquals(ReadCursors()["block"],
                JObject.Parse("""{ "image": "cursors/block.png", "frame": [4, 4], "hotspot": [1, 3] }""")));
        }

        [Test]
        public void An_icon_frame_in_an_animated_cursor_has_the_hotspot_of_its_fields()
        {
            AddCursor("wait.ani", AnimatedCursorBytes.Write(new AnimatedCursorFrame(Red, hotspotX: 1, hotspotY: 2,
                type: AnimatedCursorFrame.IconType)));

            Convert();

            Assert.That(ReadCursors()["wait"]!["hotspot"]!.ToObject<int[]>(), Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void Pixels_under_the_mask_are_transparent()
        {
            var frame = new AnimatedCursorFrame(Red);

            frame.Transparent.Add((1, 2));

            AddCursor("normal.ani", AnimatedCursorBytes.Write(frame));

            Convert();

            var strip = PngPixels.Read(StripPath("normal"));

            Assert.That(strip.At(1, 2).A, Is.EqualTo(0));
            Assert.That(strip.At(2, 1).A, Is.EqualTo(255));
        }

        [Test]
        public void Frames_with_an_alpha_channel_keep_their_alpha()
        {
            var frame = new AnimatedCursorFrame(new Rgba32(255, 0, 0, 128), bitsPerPixel: 32);

            frame.Transparent.Add((3, 3));

            AddCursor("wait.ani", AnimatedCursorBytes.Write(frame));

            Convert();

            var strip = PngPixels.Read(StripPath("wait"));

            Assert.That(strip.At(0, 0), Is.EqualTo(new Rgba32(255, 0, 0, 128)));
            Assert.That(strip.At(3, 3).A, Is.EqualTo(0));
        }

        [Test]
        public void File_names_become_cursor_keys()
        {
            AddCursor("mouseright.ani", AnimatedCursorBytes.Write(new AnimatedCursorFrame(Red)));
            AddCursor("sail.ani", AnimatedCursorBytes.Write(new AnimatedCursorFrame(Green)));

            Convert();

            Assert.That(File.Exists(StripPath("player")), Is.True);
            Assert.That(File.Exists(StripPath("board")), Is.True);
            Assert.That(File.Exists(StripPath("mouseright")), Is.False);
            Assert.That(ReadCursors().Properties().Select(property => property.Name),
                Is.EqualTo(new[] { "board", "player" }));
            Assert.That(ReadCursors()["player"]!["image"]!.ToString(), Is.EqualTo("cursors/player.png"));
        }

        [Test]
        public void A_file_that_is_not_an_animated_cursor_fails_and_the_others_still_convert()
        {
            AddCursor("normal.ani", AnimatedCursorBytes.Write(new AnimatedCursorFrame(Red)));
            AddCursor("camera.ani", [1, 2, 3, 4, 5, 6, 7, 8]);

            var results = Convert();

            Assert.That(results.Single(result => result.Name == "camera").Outcome,
                Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(results.Single(result => result.Name == "normal").Outcome,
                Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(ReadCursors().Properties().Select(property => property.Name), Is.EqualTo(new[] { "normal" }));
        }

        [Test]
        public void The_cursor_kind_converts_only_on_request()
        {
            AddCursor("normal.ani", AnimatedCursorBytes.Write(new AnimatedCursorFrame(Red)));

            var pipeline = new ConversionPipeline(_client.Settings());

            foreach (var kind in ContentKind.Defaults)
            {
                pipeline.ConvertAll(kind).ToArray();
            }

            Assert.That(File.Exists(StripPath("normal")), Is.False);
            Assert.That(ContentKind.All, Does.Contain(ContentKind.Cursor));

            pipeline.ConvertAll(ContentKind.Cursor).ToArray();

            Assert.That(File.Exists(StripPath("normal")), Is.True);
        }

        /// <summary>
        /// Reads the PNG files that the converter writes: 8-bit RGBA with filter 0.
        /// </summary>
        private class PngPixels(int width, int height, byte[] scanlines)
        {
            public int Width { get; } = width;

            public int Height { get; } = height;

            public Rgba32 At(int x, int y)
            {
                var offset = (y * ((Width * 4) + 1)) + 1 + (x * 4);

                return new Rgba32(scanlines[offset], scanlines[offset + 1], scanlines[offset + 2],
                    scanlines[offset + 3]);
            }

            public static PngPixels Read(string path)
            {
                var png = File.ReadAllBytes(path);
                var idat = IndexOf(png, "IDAT");
                var length = BigEndian(png, idat - 4);

                using var deflated = new MemoryStream(png, idat + 6, length - 6);
                using var inflate = new DeflateStream(deflated, CompressionMode.Decompress);
                using var raw = new MemoryStream();

                inflate.CopyTo(raw);

                return new PngPixels(BigEndian(png, 16), BigEndian(png, 20), raw.ToArray());
            }

            private static int BigEndian(byte[] bytes, int offset)
            {
                return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
            }

            private static int IndexOf(byte[] bytes, string chunk)
            {
                for (var i = 0; i + 4 <= bytes.Length; i++)
                {
                    if (bytes[i] == chunk[0] && bytes[i + 1] == chunk[1] && bytes[i + 2] == chunk[2] &&
                        bytes[i + 3] == chunk[3])
                    {
                        return i;
                    }
                }

                return -1;
            }
        }
    }
}
