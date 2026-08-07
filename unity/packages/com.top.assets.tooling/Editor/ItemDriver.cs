using Top.Assets.Conversion.Pipeline;
using Top.Logging;

namespace Top.Assets.Tooling
{
    /// <summary>
    /// Realizes converted items: a prefab per module plus the definition
    /// asset that ties them to the iteminfo row.
    /// </summary>
    public static class ItemDriver
    {
        public static void Convert(int id)
        {
            var pipeline = ImporterSettings.OpenPipeline();

            if (pipeline == null)
            {
                return;
            }

            var result = pipeline.Items.Convert(id);

            Realize(pipeline, result);
            ConversionReport.Unit($"item {id}", result.Outcome);
        }

        internal static void ConvertAll(ConversionPipeline pipeline, EditorProgress progress)
        {
            ConversionReport.Batch("items", pipeline.Items.ConvertAll(progress, progress.Token),
                result => Realize(pipeline, result));
        }

        internal static void Realize(ConversionPipeline pipeline, ItemResult result)
        {
            ContentAssets.Import(pipeline.Settings, result.Artifacts);

            foreach (var artifact in result.Artifacts)
            {
                ContentAssets.Scaffold(pipeline.Settings, artifact);
            }

            ItemDefinitions.Write(pipeline.Settings, result);
        }
    }
}
