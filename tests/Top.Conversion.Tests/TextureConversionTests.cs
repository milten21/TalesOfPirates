using System.IO;
using NUnit.Framework;
using Top.Conversion.Textures;
using Top.Legacy.MindPower;
using Top.Legacy.MindPower.Geometry;
using Top.Legacy.MindPower.Textures;

namespace Top.Conversion.Tests
{
    public class TextureConversionTests
    {
        private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

        [Test]
        public void Encodes_the_decoded_source_at_its_own_size()
        {
            // The fixture is obfuscated, so this also covers the decode the
            // readers do before sniffing the format.
            var source = File.ReadAllBytes(Fixtures.Path("dds/010022.dds"));
            var expected = TextureReader.Read(source);

            var png = TextureConversion.ToPng(source, new TextureStage());

            Assert.That(png[..8], Is.EqualTo(PngSignature));
            Assert.That(ReadInt32BE(png, 16), Is.EqualTo(expected.Width));
            Assert.That(ReadInt32BE(png, 20), Is.EqualTo(expected.Height));
        }

        [Test]
        public void Color_keyed_stages_encode_differently()
        {
            var source = File.ReadAllBytes(Fixtures.Path("dds/010022.dds"));
            var first = TextureReader.Read(source).Pixels[0];

            var plain = TextureConversion.ToPng(source, new TextureStage());
            var keyed = TextureConversion.ToPng(source, new TextureStage
            {
                ColorKeyType = ColorKeyType.Specific,
                ColorKey = first,
            });

            Assert.That(keyed, Is.Not.EqualTo(plain),
                "the stage's color key reaches the encoded output");
        }

        [Test]
        public void First_pixel_keying_matches_keying_on_that_pixel()
        {
            var source = File.ReadAllBytes(Fixtures.Path("dds/010022.dds"));
            var first = TextureReader.Read(source).Pixels[0];

            var byValue = TextureConversion.ToPng(source, new TextureStage
            {
                ColorKeyType = ColorKeyType.Specific,
                ColorKey = first,
            });
            var byPosition = TextureConversion.ToPng(source, new TextureStage
            {
                ColorKeyType = ColorKeyType.FirstPixel,
            });

            Assert.That(byPosition, Is.EqualTo(byValue));
        }

        [Test]
        public void A_png_source_passes_through_unchanged()
        {
            var png = TextureConversion.ToPng(File.ReadAllBytes(Fixtures.Path("dds/010022.dds")));

            Assert.That(TextureConversion.ToPng(png), Is.EqualTo(png));
        }

        [Test]
        public void Undecodable_sources_throw()
        {
            var source = File.ReadAllBytes(Fixtures.Path("obj/teampk.obj"));

            Assert.That(() => TextureConversion.ToPng(source, new TextureStage()),
                Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void Color_keying_makes_a_texture_translucent()
        {
            var source = File.ReadAllBytes(Fixtures.Path("dds/010022.dds"));

            Assert.That(
                TextureConversion.FindHighestTranslucentAlpha(
                    TextureConversion.Decode(source, new TextureStage
                    {
                        ColorKeyType = ColorKeyType.FirstPixel,
                    })),
                Is.EqualTo((byte)0), "the keyed color becomes a hole, whatever the source alpha was");
        }

        [Test]
        public void An_image_that_is_opaque_everywhere_has_no_translucent_texels()
        {
            var image = new RgbaImage(2, 1, [new Rgba32(1, 2, 3, 255), new Rgba32(4, 5, 6, 255)]);

            Assert.That(TextureConversion.FindHighestTranslucentAlpha(image), Is.Null);

            image.Pixels[1] = new Rgba32(4, 5, 6, 254);

            Assert.That(TextureConversion.FindHighestTranslucentAlpha(image), Is.EqualTo((byte)254),
                "one texel below full alpha is enough");
        }

        [Test]
        public void The_highest_translucent_alpha_is_reported()
        {
            var image = new RgbaImage(3, 1,
                [new Rgba32(1, 2, 3, 0), new Rgba32(4, 5, 6, 200), new Rgba32(7, 8, 9, 255)]);

            Assert.That(TextureConversion.FindHighestTranslucentAlpha(image), Is.EqualTo((byte)200));
        }

        [Test]
        public void Decoding_stops_before_dilation()
        {
            var source = File.ReadAllBytes(Fixtures.Path("dds/010022.dds"));
            var stage = new TextureStage { ColorKeyType = ColorKeyType.FirstPixel };
            var decoded = TextureConversion.Decode(source, stage);

            Assert.That(decoded.Pixels[0], Is.EqualTo(new Rgba32(0, 0, 0, 0)),
                "dilation would have bled a neighbour's color into the hole");
        }

        [Test]
        public void A_named_texture_is_found_under_any_of_its_containers()
        {
            var dir = Path.GetDirectoryName(Fixtures.Path("dds/010022.dds"));

            Assert.That(Path.GetFileName(TextureConversion.FindSource(dir, "010022.bmp")),
                Is.EqualTo("010022.dds"), "the original names a texture without its container");
            Assert.That(TextureConversion.FindSource(dir, "nothing.bmp"), Is.Null);
        }

        private static int ReadInt32BE(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16)
                | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }
    }
}
