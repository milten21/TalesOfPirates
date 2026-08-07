using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Assets.Conversion.Pipeline;
using Top.Assets.Gltf.Editor;
using Top.Logging;
using UnityEditor;
using UnityEngine;

namespace Top.Assets.Tooling
{
    /// <summary>
    /// Turns what the pipeline wrote into project assets: the import the new
    /// files need, then the prefab the scaffold builds from the glTF alone.
    /// </summary>
    internal static class ContentAssets
    {
        internal const string Root = "Assets/Content";

        internal static string PathOf(ConversionSettings settings, string absolute)
        {
            return Root + "/" + Path.GetRelativePath(settings.OutputRoot, absolute).Replace('\\', '/');
        }

        internal static string ModelPath(ConversionSettings settings, string kind, string name)
        {
            return PathOf(settings, settings.Output.Model(kind, name));
        }

        internal static string PrefabPath(string kind, string name)
        {
            return $"{Root}/{kind}/Prefabs/{name}.prefab";
        }

        internal static string PrefabPath(ModelArtifact artifact)
        {
            return PrefabPath(artifact.Kind, artifact.Name);
        }

        internal static GameObject Load(ConversionSettings settings, ModelArtifact artifact)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(PathOf(settings, artifact.ModelPath));
        }

        /// <summary>
        /// Brings what a run wrote into the project. Nothing written, nothing
        /// to import: a batch of skipped units never touches the database.
        /// Converted textures carry alpha the importer has to be told about.
        /// </summary>
        internal static void Import(ConversionSettings settings, IEnumerable<ModelArtifact> artifacts)
        {
            var written = artifacts
                .Where(artifact => artifact.Outcome == ConversionOutcome.Converted)
                .ToList();

            if (written.Count == 0)
            {
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (var png in written.SelectMany(artifact => artifact.TexturePaths))
            {
                if (AssetImporter.GetAtPath(PathOf(settings, png)) is TextureImporter importer &&
                    !importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
        }

        /// <summary>
        /// Scaffolds a prefab from one converted model, or returns null when
        /// there is nothing to do: a prefab is already there and either the
        /// model behind it did not change or it is not being replaced.
        /// </summary>
        internal static string Scaffold(ConversionSettings settings, ModelArtifact artifact,
            Action<GameObject> decorate = null)
        {
            var prefabPath = PrefabPath(artifact);

            if (File.Exists(prefabPath) &&
                (!settings.Overwrite || artifact.Outcome != ConversionOutcome.Converted))
            {
                return null;
            }

            var modelAssetPath = PathOf(settings, artifact.ModelPath);

            if (AssetDatabase.LoadMainAssetAtPath(modelAssetPath) == null)
            {
                // Output kept from an earlier run that this project never
                // imported - the scaffold reads the imported model, not the file.
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            Log.Info($"scaffolded {prefabPath}");

            return prefabPath;
        }

        internal static void Select(string assetPath)
        {
            if (assetPath != null)
            {
                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
            }
        }
    }
}
