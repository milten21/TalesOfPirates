using Top.Assets.Conversion.Pipeline;
using Top.Engine;
using Top.Logging;
using UnityEngine;

namespace Top.Assets.Tooling
{
    /// <summary>
    /// Realizes converted scene objects: a prefab per model, marked with the
    /// sceneobjinfo row a placement refers to.
    /// </summary>
    public static class SceneObjectDriver
    {
        public static void Convert(int id)
        {
            var pipeline = ImporterSettings.OpenPipeline();

            if (pipeline == null)
            {
                return;
            }

            var result = pipeline.SceneObjects.Convert(id);

            ContentAssets.Select(Realize(pipeline, result));
            ConversionReport.Unit($"scene object {id}", result.Outcome);
        }

        internal static void ConvertAll(ConversionPipeline pipeline, EditorProgress progress)
        {
            ConversionReport.Batch("scene objects", pipeline.SceneObjects.ConvertAll(progress, progress.Token),
                result => Realize(pipeline, result));
        }

        private static string Realize(ConversionPipeline pipeline, SceneObjectResult result)
        {
            if (result.Model == null)
            {
                return null;
            }

            ContentAssets.Import(pipeline.Settings, result.Artifacts);

            return ContentAssets.Scaffold(pipeline.Settings, result.Model, root => Mark(root, result.Id));
        }

        internal static void Mark(GameObject root, int typeId)
        {
            root.AddComponent<SceneObject>().typeId = typeId;
        }
    }
}
