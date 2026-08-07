using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Top.Assets.Contract.Models;
using Top.Assets.Contract.Models.Extras;
using Top.Assets.Contract.Models.Materials;
using Top.Gltf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Top.Assets.Gltf.Editor.Tests
{
    public class GltfScaffolderTests
    {
        private const string TempDir = "Assets/TempGltfScaffoldTests";
        private const string GlbPath = TempDir + "/hut.glb";
        private const string PrefabPath = TempDir + "/hut.prefab";
        private const string MaterialsDir = TempDir + "/Materials";
        private const string TracksDir = TempDir + "/Tracks";

        [OneTimeSetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(TempDir);
            WritePng(TempDir + "/hut_a.png");
            WritePng(TempDir + "/hut_b.png");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var builder = new GltfBuilder("hut");
            var a = builder.AddTexture("hut_a.png");
            var b = builder.AddTexture("hut_b.png");

            var wall = builder.AddMaterial(Naming.Material("hut", 0, 0))
                .WithTexture(a)
                .WithAlphaBlend();

            new MaterialExtras
            {
                RenderState = new RenderStateExtras
                {
                    SrcBlend = BlendFactor.One,
                    DstBlend = BlendFactor.One,
                    BlendEnabled = true,
                    ZWrite = true,
                    Cull = FaceCulling.Front,
                    Lit = false,
                    Transparency = TransparencyMode.Additive,
                },
                UvAnimation = new UvAnimationExtras
                {
                    Frames = new[] { new[] { 1f, 0f, 0f, 1f, 0.5f, 0.25f } },
                },
            }.Write(wall);

            var glass = builder.AddMaterial(Naming.Material("hut", 0, 1))
                .WithTexture(b)
                .WithAlphaMask(0.4f);

            new MaterialExtras
            {
                OpacityAnimation = new OpacityAnimationExtras
                {
                    KeyFrames = new[] { 0, 10 },
                    Values = new[] { 1f, 0f },
                },
                Flipbook = new FlipbookExtras { Frames = new[] { a.Index, b.Index } },
            }.Write(glass);

            var mesh = builder.AddMesh("geom_0");

            Triangle(builder, mesh, wall);
            Triangle(builder, mesh, glass);

            var shell = builder.AddMesh(Naming.LitShell("geom_0"));

            Triangle(builder, shell, builder.AddMaterial(Naming.Material("hut", 0, 2)));

            var helper = builder.AddMesh("helper_block_0");

            Triangle(builder, helper, null);

            builder
                .AddNode("geom_0")
                .WithMesh(mesh)
                .AddChild(builder.AddNode(Naming.LitShell("geom_0")).WithMesh(shell))
                .AsRoot();
            builder.AddNode("helper_block_0").WithMesh(helper).AsRoot();

            var file = builder.Build();

            TestGlb.Write(file.Document, file.BinChunk, GlbPath);

            GltfScaffolder.Scaffold(GlbPath, PrefabPath, overwrite: true, root => root.AddComponent<BoxCollider>());
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempDir);
        }

        [Test]
        public void Extras_win_over_the_blend_the_standard_fields_could_only_approximate()
        {
            var wall = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/hut_0_0.mat");

            Assert.That(wall, Is.Not.Null);
            Assert.That(wall.GetFloat("_SrcBlend"), Is.EqualTo((float)BlendMode.One));
            Assert.That(wall.GetFloat("_DstBlend"), Is.EqualTo((float)BlendMode.One));
            Assert.That(wall.GetFloat("_ZWrite"), Is.EqualTo(1f));
            Assert.That(wall.GetFloat("_Cull"), Is.EqualTo((float)CullMode.Front));
            Assert.That(wall.GetFloat("_Unlit"), Is.EqualTo(1f));
            Assert.That(wall.GetFloat("_UvAnimated"), Is.EqualTo(1f));
            Assert.That(AssetDatabase.GetAssetPath(wall.mainTexture), Is.EqualTo(TempDir + "/hut_a.png"));
        }

        [Test]
        public void An_alpha_tested_material_asks_its_texture_to_keep_coverage()
        {
            var glass = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/hut_0_1.mat");
            var importer = (TextureImporter)AssetImporter.GetAtPath(TempDir + "/hut_b.png");

            Assert.That(glass.GetFloat("_Cutoff"), Is.EqualTo(0.4f).Within(1e-4f));
            Assert.That(importer.mipMapsPreserveCoverage, Is.True);
            Assert.That(importer.alphaTestReferenceValue, Is.EqualTo(0.4f).Within(1e-4f));
        }

        [Test]
        public void Every_track_section_lands_as_an_asset()
        {
            var uv = AssetDatabase.LoadAssetAtPath<UvAnimationTrack>(TracksDir + "/hut_uv_0_0.asset");
            var opacity = AssetDatabase.LoadAssetAtPath<OpacityAnimationTrack>(
                TracksDir + "/hut_opacity_0_1.asset");
            var flipbook = AssetDatabase.LoadAssetAtPath<TextureImageTrack>(
                TracksDir + "/hut_teximg_0_1.asset");

            Assert.That(uv.frames[0].m20, Is.EqualTo(0.5f));
            Assert.That(uv.frames[0].m21, Is.EqualTo(0.25f));
            Assert.That(opacity.keyFrames, Is.EqualTo(new[] { 0, 10 }));
            Assert.That(opacity.values, Is.EqualTo(new[] { 1f, 0f }));
            Assert.That(AssetDatabase.GetAssetPath(flipbook.frames[0]), Is.EqualTo(TempDir + "/hut_a.png"));
            Assert.That(AssetDatabase.GetAssetPath(flipbook.frames[1]), Is.EqualTo(TempDir + "/hut_b.png"));
        }

        [Test]
        public void Components_land_on_the_node_and_slot_that_render_the_material()
        {
            var geometry = Prefab().transform.Find("geom_0");

            Assert.That(geometry.GetComponent<UvMatrixAnimation>().materialIndex, Is.Zero);
            Assert.That(geometry.GetComponent<OpacityAnimation>().materialIndex, Is.EqualTo(1));
            Assert.That(geometry.GetComponent<TextureImageAnimation>().materialIndex, Is.EqualTo(1));
            Assert.That(geometry.GetComponent<UvMatrixAnimation>().track.frames[0].m20, Is.EqualTo(0.5f));
        }

        [Test]
        public void Helpers_collide_and_lit_shells_stay_off()
        {
            var prefab = Prefab();

            Assert.That(prefab.transform.Find("helper_block_0").GetComponent<MeshCollider>(),
                Is.Not.Null);
            Assert.That(prefab.transform.Find("geom_0/geom_0_lit").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void The_caller_gets_the_root_before_it_is_written()
        {
            Assert.That(Prefab().GetComponent<BoxCollider>(), Is.Not.Null);
        }

        [Test]
        public void Rescaffolding_leaves_edited_assets_alone_until_asked_to_overwrite()
        {
            var wall = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/hut_0_0.mat");
            var uv = AssetDatabase.LoadAssetAtPath<UvAnimationTrack>(TracksDir + "/hut_uv_0_0.asset");

            wall.SetFloat("_Cull", (float)CullMode.Off);
            uv.frames = new[] { Matrix4x4.zero };

            GltfScaffolder.Scaffold(GlbPath, PrefabPath, overwrite: false);

            Assert.That(wall.GetFloat("_Cull"), Is.EqualTo((float)CullMode.Off));
            Assert.That(uv.frames[0], Is.EqualTo(Matrix4x4.zero));

            GltfScaffolder.Scaffold(GlbPath, PrefabPath, overwrite: true,
                root => root.AddComponent<BoxCollider>());

            Assert.That(wall.GetFloat("_Cull"), Is.EqualTo((float)CullMode.Front));
            Assert.That(uv.frames[0].m20, Is.EqualTo(0.5f));
        }

        [Test]
        public void A_document_carrying_no_extras_still_scaffolds()
        {
            var directory = TempDir + "/Vanilla";

            Directory.CreateDirectory(directory);

            var builder = new GltfBuilder("shed");
            var mesh = builder.AddMesh("geom_0");

            Triangle(builder, mesh, builder.AddMaterial("shed_0_0").WithAlphaBlend());
            builder.AddNode("geom_0").WithMesh(mesh).AsRoot();

            var file = builder.Build();

            TestGlb.Write(file.Document, file.BinChunk, directory + "/shed.glb");

            GltfScaffolder.Scaffold(directory + "/shed.glb", directory + "/shed.prefab", overwrite: true);

            var material = AssetDatabase.LoadAssetAtPath<Material>(directory + "/Materials/shed_0_0.mat");

            Assert.That(material.GetFloat("_SrcBlend"), Is.EqualTo((float)BlendMode.SrcAlpha));
            Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(0f));
            Assert.That(Directory.Exists(directory + "/Tracks"), Is.False);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(directory + "/shed.prefab"),
                Is.Not.Null);
        }

        private static GameObject Prefab()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        private static void Triangle(GltfBuilder builder, GltfMeshRef mesh, GltfMaterialRef material)
        {
            var positions = builder.Buffer.AddVec3(new[]
            {
                new System.Numerics.Vector3(0, 0, 0),
                new System.Numerics.Vector3(1, 0, 0),
                new System.Numerics.Vector3(0, 1, 0),
            }, withMinMax: true);

            mesh.AddPrimitive(
                new Dictionary<string, int> { ["POSITION"] = positions },
                builder.Buffer.AddIndices(new uint[] { 0, 1, 2 }),
                material);
        }

        private static void WritePng(string assetPath)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);

            texture.SetPixels(new[] { Color.red, Color.green, Color.blue, Color.white });
            texture.Apply();

            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
    }
}
