using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Top.Gltf;
using UnityEditor;
using UnityEngine;

namespace Top.Assets.Gltf.Editor.Tests
{
    public class GltfMaterialExtractorTests
    {
        private const string TempDir = "Assets/TempGltfExtractTests";
        private const string GlbPath = TempDir + "/box.glb";
        private const string MaterialsDir = TempDir + "/Materials";

        [OneTimeSetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(TempDir);

            var doc = new GltfDocument
            {
                Scene = 0,
                Scenes = new List<GltfScene> { new GltfScene { Nodes = new List<int> { 0 } } },
                Nodes = new List<GltfNode> { new GltfNode { Name = "box", Mesh = 0 } },
                Meshes = new List<GltfMesh> { new GltfMesh { Name = "box_mesh" } },
                Materials = new List<GltfMaterial>
                {
                    new GltfMaterial
                    {
                        Name = "wood",
                        AlphaMode = "MASK",
                        AlphaCutoff = 0.4f,
                        DoubleSided = true,
                        PbrMetallicRoughness = new GltfPbrMetallicRoughness
                        {
                            BaseColorTexture = new GltfTextureInfo { Index = 0 },
                        },
                    },
                    new GltfMaterial { Name = "glass", AlphaMode = "BLEND" },
                    new GltfMaterial { Name = "steel" },
                },
                Textures = new List<GltfTexture> { new GltfTexture { Source = 0 } },
            };

            var builder = new BufferBuilder(doc);
            var positions = builder.AddVec3(new[]
            {
                new System.Numerics.Vector3(0, 0, 0),
                new System.Numerics.Vector3(1, 0, 0),
                new System.Numerics.Vector3(0, 1, 0),
            }, withMinMax: true);
            var indices = builder.AddIndices(new uint[] { 0, 1, 2 });

            doc.Meshes[0].Primitives.Add(new GltfPrimitive
            {
                Attributes = { ["POSITION"] = positions },
                Indices = indices,
                Material = 0,
            });
            doc.Meshes[0].Primitives.Add(new GltfPrimitive
            {
                Attributes = { ["POSITION"] = positions },
                Indices = indices,
                Material = 1,
            });
            doc.Images = new List<GltfImage>
            {
                new GltfImage
                {
                    MimeType = "image/png",
                    BufferView = builder.AddBytes(MakePngBytes()),
                },
            };

            TestGlb.Write(doc, builder.Finish(), GlbPath);

            var existingSteel = new Material(Shader.Find("Top/Legacy"));
            existingSteel.name = "steel";
            AssetDatabase.CreateAsset(existingSteel, TempDir + "/steel.mat");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempDir);
        }

        [Test]
        public void Extracts_seed_materials_and_remaps()
        {
            GltfMaterialExtractor.Extract(GlbPath, MaterialsDir);

            var wood = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/wood.mat");
            var glass = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/glass.mat");
            Assert.That(wood, Is.Not.Null);
            Assert.That(glass, Is.Not.Null);
            Assert.That(wood.shader.name, Is.EqualTo("Top/Legacy"));
            Assert.That(wood.GetFloat("_Cutoff"), Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(wood.GetFloat("_Cull"), Is.EqualTo((float)UnityEngine.Rendering.CullMode.Off));
            Assert.That(wood.renderQueue, Is.EqualTo(2450));
            Assert.That(wood.mainTexture, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(wood.mainTexture),
                Is.EqualTo(TempDir + "/Textures/box_image_0.png"));
            Assert.That(glass.GetFloat("_SrcBlend"), Is.EqualTo((float)UnityEngine.Rendering.BlendMode.SrcAlpha));
            Assert.That(glass.GetFloat("_DstBlend"),
                Is.EqualTo((float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            Assert.That(glass.GetFloat("_ZWrite"), Is.EqualTo(0f));
            Assert.That(glass.renderQueue, Is.EqualTo(3000));

            var textureImporter = (TextureImporter)AssetImporter.GetAtPath(TempDir + "/Textures/box_image_0.png");
            Assert.That(textureImporter.alphaIsTransparency, Is.True);

            Assert.That(AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/steel.mat"),
                Is.Null, "same-named project materials are reused, not re-seeded");
            var map = AssetImporter.GetAtPath(GlbPath).GetExternalObjectMap();
            Assert.That(map[new AssetImporter.SourceAssetIdentifier(typeof(Material), "steel")],
                Is.EqualTo(AssetDatabase.LoadAssetAtPath<Material>(TempDir + "/steel.mat")));

            var root = AssetDatabase.LoadMainAssetAtPath(GlbPath) as GameObject;
            var renderer = root.GetComponentInChildren<MeshRenderer>();
            Assert.That(renderer.sharedMaterials, Is.EqualTo(new[] { wood, glass }));

            glass.SetFloat("_ZWrite", 1f);
            GltfMaterialExtractor.Extract(GlbPath, MaterialsDir);
            Assert.That(glass.GetFloat("_ZWrite"), Is.EqualTo(1f), "existing materials are reused, not overwritten");
        }

        [Test]
        public void Reuse_can_be_disabled()
        {
            var dir = TempDir + "/NoReuse";
            GltfMaterialExtractor.Extract(GlbPath, dir, reuseProjectMaterials: false);

            Assert.That(AssetDatabase.LoadAssetAtPath<Material>(dir + "/steel.mat"),
                Is.Not.Null, "a seed is created instead of reusing the project material");
        }

        [Test]
        public void Reuse_only_extraction_creates_no_materials_folder()
        {
            var dir = TempDir + "/ReuseOnly";

            Directory.CreateDirectory(dir);

            var doc = new GltfDocument
            {
                Materials = new List<GltfMaterial> { new GltfMaterial { Name = "steel" } },
            };

            var builder = new BufferBuilder(doc);
            builder.AddScalars(new[] { 0f }, withMinMax: false);

            var glbPath = dir + "/reuse.glb";

            TestGlb.Write(doc, builder.Finish(), glbPath);

            GltfMaterialExtractor.Extract(glbPath, dir + "/Materials");

            Assert.That(Directory.Exists(dir + "/Materials"), Is.False);
        }

        private static byte[] MakePngBytes()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            texture.SetPixels(new[] { Color.red, Color.green, Color.blue, Color.white });
            texture.Apply();

            var bytes = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            return bytes;
        }
    }
}
