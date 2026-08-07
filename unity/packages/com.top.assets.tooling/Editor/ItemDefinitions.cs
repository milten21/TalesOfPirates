using System;
using System.IO;
using Top.Assets.Conversion.Pipeline;
using Top.Engine;
using UnityEditor;
using UnityEngine;

namespace Top.Assets.Tooling
{
    /// <summary>
    /// The item asset gameplay reads: the row's identity plus the model each
    /// player framework wears or holds it as. Written in place so references
    /// to it survive reconversion.
    /// </summary>
    internal static class ItemDefinitions
    {
        internal static void Write(ConversionSettings settings, ItemResult item)
        {
            Write(item, module => LoadModule(settings, item.Wearable, module));
        }

        internal static void Write(ItemResult item, Func<string, GameObject> loadModule)
        {
            var models = new GameObject[ItemConverter.Models];
            var any = false;

            for (var model = 0; model < models.Length; model++)
            {
                if (item.Modules[model] == null)
                {
                    continue;
                }

                models[model] = loadModule(item.Modules[model]);
                any |= models[model] != null;
            }

            if (!any)
            {
                return;
            }

            var assetPath = ItemDefinition.AssetPath(item.Id);
            var definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
            var fresh = definition == null;

            if (fresh)
            {
                definition = ScriptableObject.CreateInstance<ItemDefinition>();
            }

            definition.id = item.Id;
            definition.itemName = item.Name;
            definition.models = models;

            if (fresh)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                AssetDatabase.CreateAsset(definition, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssetIfDirty(definition);
            }
        }

        /// <summary>
        /// A worn module is the skinned part itself, a held one the prefab
        /// scaffolded around it; either falls back to the other.
        /// </summary>
        private static GameObject LoadModule(ConversionSettings settings, bool wearable, string module)
        {
            var part = AssetDatabase.LoadAssetAtPath<GameObject>(
                ContentAssets.ModelPath(settings, ContentKind.Character, module));
            var held = AssetDatabase.LoadAssetAtPath<GameObject>(
                ContentAssets.PrefabPath(ContentKind.Item, module));

            if (wearable)
            {
                return part != null ? part : held;
            }

            return held != null ? held : part;
        }
    }
}
