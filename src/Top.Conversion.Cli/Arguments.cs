using System;
using System.Collections.Generic;
using System.Linq;
using Top.Conversion.Pipeline;

namespace Top.Conversion.Cli
{
    /// <summary>
    /// Encapsulates command line arguments for the Top.Conversion.Cli application.
    /// </summary>
    internal class Arguments
    {
        public const string DefaultSource = "reference/assets";

        public const string DefaultOutput = "artifacts/content";

        private const int WaterQuadTiles = 4;

        public static readonly string Usage = $"""
            usage: Top.Conversion.Cli [--source <dir>] [--out <dir>] [--kinds <list>] [--chunk-size <n>]

              --source <dir>    original client root, default {DefaultSource}
              --out <dir>       converted tree root, default {DefaultOutput}
              --kinds <list>    comma separated, any of {string.Join(", ", ContentKind.All)}.
                                default {string.Join(", ", ContentKind.Defaults)}. {ContentKind.Cursor} is
                                opt-in: it runs only when named, with --out set to the client's base
                                theme folder
              --chunk-size <n>  map chunk size in tiles, default {Pipeline.Maps.MapBuilder.DefaultChunkSize}
              --help, -h        this text

            Relative paths resolve against the current directory. A run overwrites what is
            already in the tree - delete the tree root first for a clean one.

            """;

        private Arguments(string source, string output, IReadOnlyList<string> kinds, int mapChunkSize)
        {
            Source = source;
            Output = output;
            Kinds = kinds;
            MapChunkSize = mapChunkSize;
        }

        public string Source { get; }

        public string Output { get; }

        public IReadOnlyList<string> Kinds { get; }

        public int MapChunkSize { get; }

        public static bool TryParse(IReadOnlyList<string> args, out Arguments arguments, out string error)
        {
            var source = DefaultSource;
            var output = DefaultOutput;
            var kinds = ContentKind.Defaults;
            var mapChunkSize = Pipeline.Maps.MapBuilder.DefaultChunkSize;

            arguments = null;
            error = null;

            for (var i = 0; i < args.Count; i++)
            {
                var name = args[i];

                switch (name)
                {
                    case "--source":
                        if (!TryValue(args, name, ref i, out source, out error))
                        {
                            return false;
                        }

                        break;

                    case "--out":
                        if (!TryValue(args, name, ref i, out output, out error))
                        {
                            return false;
                        }

                        break;

                    case "--kinds":
                        if (!TryValue(args, name, ref i, out var list, out error) ||
                            !TryKinds(list, out kinds, out error))
                        {
                            return false;
                        }

                        break;

                    case "--chunk-size":
                        if (!TryValue(args, name, ref i, out var chunkSizeText, out error) ||
                            !TryChunkSize(chunkSizeText, out mapChunkSize, out error))
                        {
                            return false;
                        }

                        break;

                    default:
                        error = $"unknown argument '{name}'";

                        return false;
                }
            }

            arguments = new Arguments(source, output, kinds, mapChunkSize);

            return true;
        }

        private static bool TryValue(IReadOnlyList<string> args, string name, ref int i, out string value,
            out string error)
        {
            if (i + 1 >= args.Count)
            {
                value = null;
                error = $"{name} needs a value";

                return false;
            }

            value = args[++i];
            error = null;

            return true;
        }

        private static bool TryKinds(string list, out IReadOnlyList<string> kinds, out string error)
        {
            var named = list.Split(',')
                .Select(kind => kind.Trim().ToLowerInvariant())
                .Where(kind => kind.Length > 0)
                .Distinct()
                .ToList();

            kinds = named;
            error = null;

            if (named.Count == 0)
            {
                error = "--kinds needs at least one kind";

                return false;
            }

            var unknown = named.FirstOrDefault(kind => !ContentKind.All.Contains(kind, StringComparer.Ordinal));

            if (unknown == null)
            {
                return true;
            }

            error = $"unknown kind '{unknown}', expected any of {string.Join(", ", ContentKind.All)}";

            return false;
        }

        private static bool TryChunkSize(string text, out int chunkSize, out string error)
        {
            if (!int.TryParse(text, out chunkSize) || chunkSize <= 0)
            {
                error = $"--chunk-size needs a positive whole number, got '{text}'";

                return false;
            }

            if (chunkSize % WaterQuadTiles != 0)
            {
                error = $"--chunk-size must divide by {WaterQuadTiles} for the water mesh to build, got '{text}'";

                return false;
            }

            error = null;

            return true;
        }
    }
}
