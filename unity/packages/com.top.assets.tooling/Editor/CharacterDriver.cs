using System.Collections.Generic;
using System.IO;
using Top.Assets.Conversion.Pipeline;
using Top.Engine;
using Top.Logging;
using UnityEditor;
using UnityEngine;

namespace Top.Assets.Tooling
{
    /// <summary>
    /// Realizes converted characters. A monster is one scaffolded prefab; a
    /// player character is its rig with the skinned parts of its equipment
    /// bound onto it.
    /// </summary>
    public static class CharacterDriver
    {
        public static void Convert(int id)
        {
            var pipeline = ImporterSettings.OpenPipeline();

            if (pipeline == null)
            {
                return;
            }

            var result = pipeline.Characters.Convert(id);

            ContentAssets.Select(Realize(pipeline, result));
            ConversionReport.Unit($"character {id}", result.Outcome);
        }

        /// <summary>
        /// Converts every character on one skeleton - what picking a .lab
        /// file or naming a model id asks for.
        /// </summary>
        public static void ConvertModel(int model)
        {
            var pipeline = ImporterSettings.OpenPipeline();

            if (pipeline == null)
            {
                return;
            }

            using var progress = new EditorProgress();

            ConversionReport.Batch($"model {model:D4}",
                pipeline.Characters.ConvertModel(model, progress, progress.Token),
                result => Realize(pipeline, result));
        }

        public static void ConvertModel(string labPath)
        {
            var name = Path.GetFileNameWithoutExtension(labPath);

            if (int.TryParse(name, out var model))
            {
                ConvertModel(model);

                return;
            }

            Log.Error($"'{name}' is not a skeleton model id");
        }

        public static void ConvertRig(string labPath)
        {
            var pipeline = ImporterSettings.OpenPipeline();

            if (pipeline == null)
            {
                return;
            }

            var rig = pipeline.Rigs.Convert(labPath);

            if (rig == null)
            {
                return;
            }

            ContentAssets.Import(pipeline.Settings, new[] { rig });
            ContentAssets.Select(ContentAssets.PathOf(pipeline.Settings, rig.ModelPath));
            ConversionReport.Unit($"rig {rig.Name}", rig.Outcome);
        }

        internal static void ConvertAll(ConversionPipeline pipeline, EditorProgress progress)
        {
            ConversionReport.Batch("characters", pipeline.Characters.ConvertAll(progress, progress.Token),
                result => Realize(pipeline, result));
        }

        private static string Realize(ConversionPipeline pipeline, CharacterResult result)
        {
            ContentAssets.Import(pipeline.Settings, result.Artifacts);

            if (result.Model != null)
            {
                return ContentAssets.Scaffold(pipeline.Settings, result.Model);
            }

            if (result.Rig == null)
            {
                return null;
            }

            foreach (var part in result.Parts)
            {
                ItemDriver.Realize(pipeline, part);
            }

            return Compose(pipeline, result);
        }

        private static string Compose(ConversionPipeline pipeline, CharacterResult result)
        {
            var prefabPath = ContentAssets.PrefabPath(ContentKind.Character, result.Name);

            if (File.Exists(prefabPath) && (!pipeline.Settings.Overwrite ||
                                            result.Outcome != ConversionOutcome.Converted))
            {
                return null;
            }

            var rig = ContentAssets.Load(pipeline.Settings, result.Rig);

            if (rig == null)
            {
                Log.Error($"no rig at '{ContentAssets.PathOf(pipeline.Settings, result.Rig.ModelPath)}'");

                return null;
            }

            var parts = SkinnedParts(pipeline, result);

            if (parts.Count == 0)
            {
                Log.Error($"no part of character {result.Id} loaded");

                return null;
            }

            var root = (GameObject)PrefabUtility.InstantiatePrefab(rig);

            try
            {
                var bones = SkinnedPartBinder.MapBones(root.transform);

                foreach (var source in parts)
                {
                    SkinnedPartBinder.Bind(source, root.transform, bones);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            Log.Info($"composed {prefabPath}");

            return prefabPath;
        }

        private static List<SkinnedMeshRenderer> SkinnedParts(ConversionPipeline pipeline,
            CharacterResult result)
        {
            var parts = new List<SkinnedMeshRenderer>();

            foreach (var part in result.Parts)
            {
                foreach (var artifact in part.Artifacts)
                {
                    var model = ContentAssets.Load(pipeline.Settings, artifact);

                    if (model == null)
                    {
                        continue;
                    }

                    var skinned = model.GetComponentInChildren<SkinnedMeshRenderer>(true);

                    if (skinned == null)
                    {
                        Log.Warning($"part '{artifact.Name}' has no skinned mesh");

                        continue;
                    }

                    parts.Add(skinned);
                }
            }

            return parts;
        }
    }
}
