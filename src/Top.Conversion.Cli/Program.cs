using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Conversion.Pipeline;
using Top.Logging;

namespace Top.Conversion.Cli
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    internal static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.Write(Arguments.Usage);

                return 0;
            }

            if (!Arguments.TryParse(args, out var arguments, out var error))
            {
                Console.Error.WriteLine(error);
                Console.Error.WriteLine();
                Console.Error.Write(Arguments.Usage);

                return 2;
            }

            if (!Directory.Exists(arguments.Source))
            {
                Console.Error.WriteLine($"no client root at '{Path.GetFullPath(arguments.Source)}'");

                return 2;
            }

            Log.Writer = new ConsoleLog();

            return Report(Convert(arguments));
        }

        private static IReadOnlyList<KindRun> Convert(Arguments arguments)
        {
            var settings = new ConversionSettings(arguments.Source, arguments.Output, overwrite: true,
                arguments.MapChunkSize);
            var pipeline = new ConversionPipeline(settings);
            var runs = new List<KindRun>();

            Console.WriteLine($"from {Path.GetFullPath(arguments.Source)}");
            Console.WriteLine($"to   {Path.GetFullPath(arguments.Output)}");

            foreach (var kind in arguments.Kinds)
            {
                var run = new KindRun(kind);

                runs.Add(run);

                Console.WriteLine();

                try
                {
                    foreach (var result in Units(kind, pipeline, run))
                    {
                        run.Add(result);
                    }
                }
                catch (Exception exception)
                {
                    Log.Error($"{kind} stopped", exception);

                    run.Trouble = "stopped early";
                }

                if (run.Total == 0 && run.Trouble == null)
                {
                    Log.Error($"no {kind} units to convert - " +
                              $"is '{Path.GetFullPath(arguments.Source)}' the client root?");

                    run.Trouble = "no units";
                }

                Console.WriteLine(run.Summary);
            }

            return runs;
        }

        private static IEnumerable<UnitResult> Units(string kind, ConversionPipeline pipeline,
            IProgress<ConversionProgress> progress)
        {
            return kind switch
            {
                ContentKind.Character => pipeline.Characters.ConvertAll(progress),
                ContentKind.Item => pipeline.Items.ConvertAll(progress),
                ContentKind.Scene => pipeline.SceneObjects.ConvertAll(progress),
                ContentKind.Table => pipeline.Tables.ConvertAll(progress),
                ContentKind.Map => pipeline.Maps.ConvertAll(progress),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        private static int Report(IReadOnlyList<KindRun> runs)
        {
            var failed = runs.Sum(run => run.Failed);

            Console.WriteLine();
            Console.WriteLine("summary");

            foreach (var run in runs)
            {
                Console.WriteLine($"  {run.Summary}");
            }

            if (failed > 0)
            {
                Console.WriteLine($"  {failed} failed in all");
            }

            return failed > 0 || runs.Any(run => run.Trouble != null) ? 1 : 0;
        }
    }
}
