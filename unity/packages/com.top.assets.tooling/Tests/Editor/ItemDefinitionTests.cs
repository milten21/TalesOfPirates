using System.IO;
using NUnit.Framework;
using Top.Assets.Conversion.Pipeline;
using Top.Engine;
using UnityEditor;
using UnityEngine;

namespace Top.Assets.Tooling.Tests
{
    public class ItemDefinitionTests
    {
        private const int TestId = 9999;
        private const string PartPrefabPath = "Assets/ItemDefinitionTests_part.prefab";

        private GameObject _part;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("part");
            _part = PrefabUtility.SaveAsPrefabAsset(go, PartPrefabPath);
            Object.DestroyImmediate(go);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(PartPrefabPath);
            AssetDatabase.DeleteAsset(ItemDefinition.AssetPath(TestId));
        }

        private static ItemResult Item(string module, string name = "Test Item")
        {
            var modules = new string[ItemConverter.Models];
            modules[0] = module;

            return new ItemResult(TestId, name, ConversionOutcome.Converted, wearable: true,
                modules, new ModelArtifact[ItemConverter.Models]);
        }

        [Test]
        public void WritesDefinitionReferencingResolvedModels()
        {
            ItemDefinitions.Write(Item("1234"), module => module == "1234" ? _part : null);

            var definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDefinition.AssetPath(TestId));
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.id, Is.EqualTo(TestId));
            Assert.That(definition.itemName, Is.EqualTo("Test Item"));
            Assert.That(definition.models[0], Is.EqualTo(_part));
            Assert.That(definition.models[1], Is.Null);
        }

        [Test]
        public void SkipsItemWithNothingResolved()
        {
            ItemDefinitions.Write(Item("1234"), module => null);

            Assert.That(File.Exists(ItemDefinition.AssetPath(TestId)), Is.False);
        }

        [Test]
        public void RegeneratesInPlaceKeepingGuid()
        {
            ItemDefinitions.Write(Item("1234"), module => _part);
            var guid = AssetDatabase.AssetPathToGUID(ItemDefinition.AssetPath(TestId));

            ItemDefinitions.Write(Item("1234", "Renamed"), module => _part);

            var definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDefinition.AssetPath(TestId));
            Assert.That(definition.itemName, Is.EqualTo("Renamed"));
            Assert.That(AssetDatabase.AssetPathToGUID(ItemDefinition.AssetPath(TestId)), Is.EqualTo(guid));
        }
    }
}
