using GLTFast.Schema;
using Newtonsoft.Json;
using NUnit.Framework;
using Top.Client.Models.Materials;
using Top.Contracts.Assets.Models.Materials;
using Material = GLTFast.Newtonsoft.Schema.Material;
using PbrMetallicRoughness = GLTFast.Newtonsoft.Schema.PbrMetallicRoughness;

namespace Top.Client.Models.Tests
{
    public class MaterialStateReaderTests
    {
        private static Material WithAlphaMode(MaterialBase.AlphaMode mode)
        {
            var material = new Material();

            material.SetAlphaMode(mode);

            return material;
        }

        [Test]
        public void Base_color_factor_alpha_becomes_opacity()
        {
            var state = MaterialStateReader.Read(new Material
            {
                pbrMetallicRoughness = new PbrMetallicRoughness
                {
                    baseColorFactor = new[] { 1f, 1f, 1f, 0.3f },
                },
            });

            Assert.That(state.Opacity, Is.EqualTo(0.3f).Within(1e-6f));
            Assert.That(state.OpacityDriven, Is.True);
        }

        [Test]
        public void Opaque_material_keeps_defaults()
        {
            var state = MaterialStateReader.Read(new Material());

            Assert.That(state.BlendEnabled, Is.False);
            Assert.That(state.AlphaTest, Is.False);
            Assert.That(state.ZWrite, Is.True);
            Assert.That(state.Cull, Is.EqualTo(FaceCulling.Back));
            Assert.That(state.OpacityDriven, Is.False);
        }

        [Test]
        public void Mask_maps_to_alpha_test()
        {
            var material = WithAlphaMode(MaterialBase.AlphaMode.Mask);

            material.alphaCutoff = 0.4f;

            var state = MaterialStateReader.Read(material);

            Assert.That(state.AlphaTest, Is.True);
            Assert.That(state.Cutoff, Is.EqualTo(0.4f).Within(1e-6f));
        }

        [Test]
        public void Mask_without_cutoff_uses_gltf_default()
        {
            var state = MaterialStateReader.Read(WithAlphaMode(MaterialBase.AlphaMode.Mask));

            Assert.That(state.Cutoff, Is.EqualTo(0.5f).Within(1e-6f));
        }

        [Test]
        public void Blend_maps_to_alpha_blend()
        {
            var state = MaterialStateReader.Read(WithAlphaMode(MaterialBase.AlphaMode.Blend));

            Assert.That(state.BlendEnabled, Is.True);
            Assert.That(state.SrcBlend, Is.EqualTo(BlendFactor.SrcAlpha));
            Assert.That(state.DstBlend, Is.EqualTo(BlendFactor.OneMinusSrcAlpha));
            Assert.That(state.ZWrite, Is.False);
        }

        [Test]
        public void Double_sided_disables_culling()
        {
            var state = MaterialStateReader.Read(new Material { doubleSided = true });

            Assert.That(state.Cull, Is.EqualTo(FaceCulling.None));
        }

        [Test]
        public void A_payload_on_the_extras_refines_what_the_standard_fields_resolved()
        {
            var material = JsonConvert.DeserializeObject<Material>(@"{
                ""alphaMode"": ""BLEND"",
                ""extras"": {
                    ""TOP_material"": {
                        ""renderState"": {
                            ""srcBlend"": ""One"",
                            ""dstBlend"": ""One"",
                            ""blendEnabled"": true,
                            ""transparency"": ""Additive""
                        }
                    }
                }
            }");

            var state = MaterialStateReader.Read(material);

            Assert.That(state.SrcBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.DstBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.BlendEnabled, Is.True);
            Assert.That(state.ZWrite, Is.True);
            Assert.That(state.Transparency, Is.EqualTo(TransparencyMode.Additive));
        }
    }
}
