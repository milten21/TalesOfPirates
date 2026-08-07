using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Gltf;
using UnityEditor;
using UnityEngine;

namespace Top.Assets.Gltf.Editor.Tests
{
    public class GltfImporterTests
    {
        private const string TempDir = "Assets/TempGltfImporterTests";
        private const string GlbPath = TempDir + "/tri.glb";
        private const string MaterialPath = TempDir + "/tri.mat";

        [OneTimeSetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(TempDir);

            var builder = new GltfBuilder("tri");
            var material = builder.AddMaterial("tri_mat");
            var mesh = builder.AddMesh("tri_mesh");
            var positions = builder.Buffer.AddVec3(new[]
            {
                new System.Numerics.Vector3(1, 2, 3),
                new System.Numerics.Vector3(4, 5, 6),
                new System.Numerics.Vector3(7, 8, 9),
            }, withMinMax: true);
            mesh.AddPrimitive(new Dictionary<string, int> { ["POSITION"] = positions },
                builder.Buffer.AddIndices(new uint[] { 0, 1, 2 }), material);
            var node = builder.AddNode("tri").WithMesh(mesh).AsRoot();

            var input = builder.Buffer.AddScalars(new[] { 0f, 1f }, withMinMax: true);
            var output = builder.Buffer.AddVec4(new[]
            {
                new System.Numerics.Vector4(0, 0, 0, 1),
                new System.Numerics.Vector4(0, 0.7071f, 0, 0.7071f),
            });
            builder.AddAnimation("default").AddChannel(input, output, node, "rotation");

            var file = builder.Build();

            TestGlb.Write(file.Document, file.BinChunk, GlbPath);

            AssetDatabase.CreateAsset(new Material(Shader.Find("Top/Legacy")), MaterialPath);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempDir);
        }

        [Test]
        public void Registers_the_hierarchy_meshes_and_clips_as_sub_assets()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(GlbPath);

            Assert.That(AssetDatabase.LoadMainAssetAtPath(GlbPath), Is.TypeOf<GameObject>());
            Assert.That(assets.OfType<Mesh>().Single().name, Is.EqualTo("tri_mesh"));
            Assert.That(assets.OfType<AnimationClip>().Single().name, Is.EqualTo("default"));

            var root = (GameObject)AssetDatabase.LoadMainAssetAtPath(GlbPath);
            Assert.That(root.transform.GetChild(0).name, Is.EqualTo("tri"));
        }

        [Test]
        public void Renderer_binds_the_remapped_material()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            var importer = AssetImporter.GetAtPath(GlbPath);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "tri_mat"), material);
            AssetDatabase.WriteImportSettingsIfDirty(GlbPath);
            AssetDatabase.ImportAsset(GlbPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            var root = (GameObject)AssetDatabase.LoadMainAssetAtPath(GlbPath);

            Assert.That(root.GetComponentInChildren<MeshRenderer>().sharedMaterials, Is.EqualTo(new[] { material }));
        }
    }
}
