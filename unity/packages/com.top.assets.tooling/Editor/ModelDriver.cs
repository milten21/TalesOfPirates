using System;
using System.IO;
using Top.Assets.Conversion.Pipeline;
using Top.Logging;
using UnityEngine;

namespace Top.Assets.Tooling
{
    /// <summary>
    /// Realizes a model picked as a file rather than named by a table row.
    /// The content family comes from the client folder it sits in; a scene
    /// model still gets the marker its row would have given it.
    /// </summary>
    public static class ModelDriver
    {
        public static void Convert(string modelPath)
        {
            var pipeline = ImporterSettings.OpenPipeline();

            if (pipeline == null)
            {
                return;
            }

            var artifact = pipeline.Models.Convert(modelPath);

            if (artifact == null)
            {
                return;
            }

            ContentAssets.Import(pipeline.Settings, new[] { artifact });
            ContentAssets.Select(ContentAssets.Scaffold(pipeline.Settings, artifact,
                Marker(pipeline, modelPath, artifact)));

            ConversionReport.Unit(artifact.Name, artifact.Outcome);
        }

        private static Action<GameObject> Marker(ConversionPipeline pipeline, string modelPath,
            ModelArtifact artifact)
        {
            if (artifact.Kind != ContentKind.Scene)
            {
                return null;
            }

            var typeId = pipeline.SceneObjects.TypeIdOf(Path.GetFileName(modelPath));

            return root => SceneObjectDriver.Mark(root, typeId);
        }
    }
}
