using System;
using System.IO;
using NUnit.Framework;
using Top.Assets.Conversion.Pipeline;

namespace Top.Assets.Tooling.Tests
{
    public class ContentAssetsTests
    {
        private static readonly ConversionSettings Settings =
            new ConversionSettings("C:/client/assets", Path.GetFullPath("Assets/Content"));

        private static ModelArtifact Artifact(string kind, string name)
        {
            return new ModelArtifact(name, kind, Settings.Output.Model(kind, name),
                Array.Empty<string>(), ConversionOutcome.Converted);
        }

        [Test]
        public void WhatThePipelineWroteMapsBackToAnAssetPath()
        {
            var artifact = Artifact(ContentKind.Scene, "stone01");

            Assert.That(ContentAssets.PathOf(Settings, artifact.ModelPath),
                Is.EqualTo("Assets/Content/Scene/Models/stone01/stone01.glb"));
        }

        [Test]
        public void TexturesMapBackTheSameWay()
        {
            var png = Path.Combine(Settings.Output.TextureDir(ContentKind.Item), "010022.png");

            Assert.That(ContentAssets.PathOf(Settings, png),
                Is.EqualTo("Assets/Content/Textures/Item/010022.png"));
        }

        [Test]
        public void EveryArtifactKnowsWhereItsPrefabGoes()
        {
            Assert.That(ContentAssets.PrefabPath(Artifact(ContentKind.Item, "01010021")),
                Is.EqualTo("Assets/Content/Item/Prefabs/01010021.prefab"));
            Assert.That(ContentAssets.PrefabPath(Artifact(ContentKind.Character, "0724")),
                Is.EqualTo("Assets/Content/Character/Prefabs/0724.prefab"));
        }

        [Test]
        public void ModelPathsResolveToTheConvertedGlb()
        {
            Assert.That(ContentAssets.ModelPath(Settings, ContentKind.Character, "0001000000"),
                Is.EqualTo("Assets/Content/Character/Models/0001000000/0001000000.glb"));
        }
    }
}
