using System;
using System.IO;
using NUnit.Framework;
using Top.Conversion.Textures;
using Top.Legacy.MindPower.Geometry;

namespace Top.Conversion.Tests
{
    public class TextureTranslucencyTests
    {
        [Test]
        public void A_keyed_stage_reports_translucent()
        {
            Assert.That(
                new TextureTranslucency().IsTranslucent(TextureDir(), new TextureStage
                {
                    FileName = "010022.dds",
                    ColorKeyType = ColorKeyType.FirstPixel,
                }),
                Is.True);
        }

        [Test]
        public void A_stage_with_no_texture_reports_opaque()
        {
            var translucency = new TextureTranslucency();

            Assert.That(translucency.IsTranslucent(TextureDir(), null), Is.False);
            Assert.That(translucency.IsTranslucent(TextureDir(), new TextureStage()), Is.False);
            Assert.That(translucency.IsTranslucent(TextureDir(), new TextureStage { FileName = "missing.dds" }),
                Is.False, "a texture nobody can find cannot be shown to need blending");
        }

        [Test]
        public void An_unreadable_texture_reports_opaque()
        {
            Assert.That(
                new TextureTranslucency().IsTranslucent(ObjectDir(), new TextureStage
                {
                    FileName = "teampk.obj",
                }),
                Is.False);
        }

        [Test]
        public void The_answer_for_one_file_is_reused()
        {
            var translucency = new TextureTranslucency();
            var stage = new TextureStage
            {
                FileName = "010022.dds",
                ColorKeyType = ColorKeyType.FirstPixel,
            };

            Assert.That(translucency.IsTranslucent(TextureDir(), stage), Is.True);

            stage.ColorKeyType = ColorKeyType.None;

            Assert.That(translucency.IsTranslucent(TextureDir(), stage), Is.True,
                "one PNG is written per file name, so a later model reads the answer that PNG matches");
        }

        [Test]
        public void One_file_name_is_answered_per_texture_folder()
        {
            var translucency = new TextureTranslucency();
            var stage = new TextureStage
            {
                FileName = "010022.dds",
                ColorKeyType = ColorKeyType.FirstPixel,
            };

            Assert.That(translucency.IsTranslucent(TextureDir(), stage), Is.True);
            Assert.That(translucency.IsTranslucent(ObjectDir(), stage), Is.False,
                "another folder writes its own PNG under the same file name");
        }

        [Test]
        public void A_keyed_hole_does_not_survive_an_alpha_test()
        {
            var translucency = new TextureTranslucency();
            var stage = new TextureStage
            {
                FileName = "010022.dds",
                ColorKeyType = ColorKeyType.FirstPixel,
            };

            Assert.That(translucency.IsTranslucent(TextureDir(), stage, 130f / 255f), Is.False,
                "the draw discards the hole, so every texel it keeps is opaque");
            Assert.That(translucency.IsTranslucent(TextureDir(), stage), Is.True,
                "without the test the hole shows through");
        }

        [Test]
        public void A_texel_at_the_cutoff_survives_an_alpha_test()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            try
            {
                WriteTexture(Path.Combine(dir, "edge.tga"), 130);

                var translucency = new TextureTranslucency();
                var stage = new TextureStage { FileName = "edge.tga" };

                Assert.That(translucency.IsTranslucent(dir, stage, 130f / 255f), Is.True,
                    "the draw keeps a texel at the cutoff, so the blend shows on it");
                Assert.That(translucency.IsTranslucent(dir, stage, 131f / 255f), Is.False);
            }
            finally
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
        }

        [Test]
        public void No_search_folder_reports_opaque()
        {
            Assert.That(
                new TextureTranslucency().IsTranslucent(null, new TextureStage
                {
                    FileName = "010022.dds",
                }),
                Is.False);
        }

        private static void WriteTexture(string path, byte alpha)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            // Uncompressed 32-bit TGA, two BGRA texels: one opaque, one at the given alpha.
            byte[] header = [0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 1, 0, 32, 8];
            byte[] texels = [10, 20, 30, 255, 40, 50, 60, alpha];

            File.WriteAllBytes(path, [.. header, .. texels]);
        }

        private static string TextureDir()
        {
            return Path.GetDirectoryName(Fixtures.Path("dds/010022.dds"));
        }

        private static string ObjectDir()
        {
            return Path.GetDirectoryName(Fixtures.Path("obj/teampk.obj"));
        }
    }
}
