using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Top.Conversion.Textures;
using Top.Legacy.MindPower;
using Top.Legacy.MindPower.Cursors;
using Top.Legacy.MindPower.Textures;
using Top.Logging;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// What converting one cursor came to.
    /// </summary>
    public class CursorResult(string key, ConversionOutcome outcome) : UnitResult(0, key, outcome)
    {
        public override IEnumerable<ModelArtifact> Artifacts => [];
    }

    /// <summary>
    /// Converts the original client's .ani cursors into one PNG sprite sheet for each
    /// cursor key, each unique frame once from left to right, with steps for
    /// the play order when the file has a seq chunk, the duration of each step
    /// in milliseconds and a hotspot for each frame when they differ, and
    /// writes cursors.json beside them in the shape of the cursors group of a
    /// theme. The output root is the theme folder, so image paths are relative
    /// to it.
    /// </summary>
    public class CursorConverter(ConversionSettings settings)
    {
        private const string Extension = ".ani";

        private const double MillisecondsPerJiffy = 1000.0 / 60;

        private static readonly IReadOnlyList<(string File, string Key)> Units =
        [
            ("normal", "normal"),
            ("mouseon", "active"),
            ("mouseclick", "click"),
            ("drag", "drag"),
            ("vertical", "vertical"),
            ("wait", "wait"),
            ("forbid", "forbid"),
            ("attack", "attack"),
            ("skill", "skill"),
            ("land", "land"),
            ("sail", "board"),
            ("pick", "pick"),
            ("camera", "camera"),
            ("chat", "chat"),
            ("mouseright", "player"),
            ("hide", "hide"),
            ("search", "search"),
            ("block", "block"),
            ("repair", "repair"),
            ("feed", "feed")
        ];

        public IEnumerable<CursorResult> ConvertAll(IProgress<ConversionProgress> progress = null,
            CancellationToken cancellation = default)
        {
            var entries = new List<(string Key, JObject Entry)>();

            foreach (var result in Batch.Run(Units,
                         unit => $"cursor {unit.File}",
                         unit => Convert(unit.File, unit.Key, entries),
                         unit => new CursorResult(unit.Key, ConversionOutcome.Failed),
                         progress, cancellation))
            {
                yield return result;
            }

            if (!cancellation.IsCancellationRequested)
            {
                WriteMetadata(entries);
            }
        }

        private CursorResult Convert(string file, string key, List<(string Key, JObject Entry)> entries)
        {
            var source = Path.Combine(settings.Source.Cursors, file + Extension);

            if (!File.Exists(source))
            {
                Log.Error($"no source file for cursor '{file}'");

                return new CursorResult(key, ConversionOutcome.Failed);
            }

            Log.Info($"converting cursor '{file}' as '{key}'");

            AnimatedCursor cursor;

            try
            {
                cursor = AnimatedCursorReader.Read(File.ReadAllBytes(source));
            }
            catch (InvalidDataException exception)
            {
                Log.Error($"cursor '{source}'", exception);

                return new CursorResult(key, ConversionOutcome.Failed);
            }

            var contentPath = OutputPaths.CursorContentPath(key);
            var (unique, steps) = FindUniqueFrames(cursor);

            entries.Add((key, CreateEntry(contentPath, unique, steps, cursor.StepDurations)));

            var path = settings.Output.At(contentPath);

            if (!settings.Overwrite && File.Exists(path))
            {
                return new CursorResult(key, ConversionOutcome.Skipped);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, PngWriter.Write(BuildSpriteSheet(unique)));

            return new CursorResult(key, ConversionOutcome.Converted);
        }

        private static (IReadOnlyList<CursorFrame> Unique, IReadOnlyList<int> Steps) FindUniqueFrames(
            AnimatedCursor cursor)
        {
            if (cursor.Steps == null)
            {
                return (cursor.Frames, null);
            }

            var unique = new List<CursorFrame>();
            var uniqueIndexes = new List<int>();

            foreach (var frame in cursor.Frames)
            {
                var index = unique.FindIndex(other => IsSameFrame(other, frame));

                if (index < 0)
                {
                    index = unique.Count;
                    unique.Add(frame);
                }

                uniqueIndexes.Add(index);
            }

            return (unique, cursor.Steps.Select(step => uniqueIndexes[step]).ToArray());
        }

        private static bool IsSameFrame(CursorFrame first, CursorFrame second)
        {
            return first.HotspotX == second.HotspotX && first.HotspotY == second.HotspotY &&
                   first.Image.Pixels.AsSpan().SequenceEqual(second.Image.Pixels);
        }

        private static JObject CreateEntry(string contentPath, IReadOnlyList<CursorFrame> frames,
            IReadOnlyList<int> steps, IReadOnlyList<int> stepDurations)
        {
            var first = frames[0];
            var entry = new JObject
            {
                ["image"] = contentPath,
                ["frame"] = new JArray(first.Image.Width, first.Image.Height),
                ["hotspot"] = CreateHotspot(frames)
            };

            if (steps is { Count: > 1 })
            {
                entry["steps"] = new JArray(steps);
            }

            if (stepDurations.Count > 1)
            {
                entry["ms"] = CreateDurations(stepDurations);
            }

            return entry;
        }

        private static JToken CreateHotspot(IReadOnlyList<CursorFrame> frames)
        {
            var hotspots = frames.Select(frame => new JArray(frame.HotspotX, frame.HotspotY)).ToArray();

            return hotspots.All(hotspot => JToken.DeepEquals(hotspot, hotspots[0]))
                ? hotspots[0]
                : new JArray(hotspots);
        }

        private static JToken CreateDurations(IReadOnlyList<int> stepDurations)
        {
            var milliseconds = stepDurations
                .Select(jiffies => (int)Math.Round(jiffies * MillisecondsPerJiffy, MidpointRounding.AwayFromZero))
                .ToArray();

            return milliseconds.All(duration => duration == milliseconds[0])
                ? new JValue(milliseconds[0])
                : new JArray(milliseconds);
        }

        private static RgbaImage BuildSpriteSheet(IReadOnlyList<CursorFrame> frames)
        {
            var width = frames[0].Image.Width;
            var height = frames[0].Image.Height;
            var stripWidth = width * frames.Count;
            var pixels = new Rgba32[stripWidth * height];

            for (var i = 0; i < frames.Count; i++)
            {
                for (var y = 0; y < height; y++)
                {
                    Array.Copy(frames[i].Image.Pixels, y * width, pixels, (y * stripWidth) + (i * width), width);
                }
            }

            return new RgbaImage(stripWidth, height, pixels);
        }

        private void WriteMetadata(IReadOnlyList<(string Key, JObject Entry)> entries)
        {
            var lines = entries.Select(entry =>
                $"    {JsonConvert.ToString(entry.Key)}: {entry.Entry.ToString(Formatting.None)}");
            var text = new StringBuilder()
                .Append("{\n  \"cursors\": {\n")
                .Append(string.Join(",\n", lines))
                .Append("\n  }\n}\n")
                .ToString();
            var path = settings.Output.CursorMetadata;

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
        }
    }
}
