using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Top.Client.Rendering.Models.Materials;
using Top.Contracts.Assets.Models.Extras;
using Top.Contracts.Assets.Models.Materials;

namespace Top.Client.Tests.Rendering.Models.Materials
{
    public class RenderStateReaderTests
    {
        [Test]
        public void A_material_with_no_members_reads_as_the_vanilla_state()
        {
            var state = RenderStateReader.Read(new JObject());

            Assert.That(state.Cull, Is.EqualTo(FaceCulling.Back));
            Assert.That(state.AlphaTest, Is.False);
            Assert.That(state.BlendEnabled, Is.False);
            Assert.That(state.ZWrite, Is.True);
            Assert.That(state.Opacity, Is.EqualTo(1f));
        }

        [Test]
        public void A_double_sided_material_culls_no_face()
        {
            var state = RenderStateReader.Read(new JObject { ["doubleSided"] = true });

            Assert.That(state.Cull, Is.EqualTo(FaceCulling.None));
        }

        [Test]
        public void A_masked_material_turns_the_alpha_test_on_with_its_cutoff()
        {
            var material = new JObject { ["alphaMode"] = "MASK", ["alphaCutoff"] = 0.25 };

            var state = RenderStateReader.Read(material);

            Assert.That(state.AlphaTest, Is.True);
            Assert.That(state.Cutoff, Is.EqualTo(0.25f));
        }

        [Test]
        public void A_masked_material_with_no_cutoff_takes_the_standard_default()
        {
            var state = RenderStateReader.Read(new JObject { ["alphaMode"] = "MASK" });

            Assert.That(state.AlphaTest, Is.True);
            Assert.That(state.Cutoff, Is.EqualTo(0.5f));
        }

        [Test]
        public void A_blended_material_blends_source_alpha_and_writes_no_depth()
        {
            var state = RenderStateReader.Read(new JObject { ["alphaMode"] = "BLEND" });

            Assert.That(state.BlendEnabled, Is.True);
            Assert.That(state.SrcBlend, Is.EqualTo(BlendFactor.SrcAlpha));
            Assert.That(state.DstBlend, Is.EqualTo(BlendFactor.OneMinusSrcAlpha));
            Assert.That(state.ZWrite, Is.False);
        }

        [Test]
        public void An_unknown_alpha_mode_leaves_the_vanilla_state()
        {
            var state = RenderStateReader.Read(new JObject { ["alphaMode"] = "SHATTER" });

            Assert.That(state.AlphaTest, Is.False);
            Assert.That(state.BlendEnabled, Is.False);
            Assert.That(state.ZWrite, Is.True);
        }

        [Test]
        public void The_base_color_alpha_becomes_the_opacity()
        {
            var material = new JObject
            {
                ["pbrMetallicRoughness"] = new JObject
                {
                    ["baseColorFactor"] = new JArray(1.0, 1.0, 1.0, 0.4),
                },
            };

            var state = RenderStateReader.Read(material);

            Assert.That(state.Opacity, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void A_base_color_of_fewer_than_four_components_leaves_the_opacity_alone()
        {
            var material = new JObject
            {
                ["pbrMetallicRoughness"] = new JObject
                {
                    ["baseColorFactor"] = new JArray(1.0, 1.0, 1.0),
                },
            };

            var state = RenderStateReader.Read(material);

            Assert.That(state.Opacity, Is.EqualTo(1f));
        }

        [Test]
        public void The_extras_refine_the_state_the_standard_fields_gave()
        {
            var material = new JObject
            {
                ["alphaMode"] = "BLEND",
                ["extras"] = Carrying(new MaterialExtras
                {
                    RenderState = new RenderStateExtras
                    {
                        SrcBlend = BlendFactor.SrcAlpha,
                        DstBlend = BlendFactor.One,
                        BlendEnabled = true,
                        ZWrite = false,
                        Lit = false,
                        Transparency = TransparencyMode.Additive,
                    },
                }),
            };

            var state = RenderStateReader.Read(material);

            Assert.That(state.DstBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.Lit, Is.False);
            Assert.That(state.Transparency, Is.EqualTo(TransparencyMode.Additive));
        }

        [Test]
        public void A_material_carrying_no_extras_reads_as_absent()
        {
            Assert.That(RenderStateReader.TryReadExtras(new JObject(), out var extras), Is.False);
            Assert.That(extras, Is.Null);
        }

        [Test]
        public void Extras_that_are_not_an_object_read_as_absent()
        {
            var material = new JObject { ["extras"] = "TOP_material" };

            Assert.That(RenderStateReader.TryReadExtras(material, out _), Is.False);
        }

        [Test]
        public void Extras_from_another_tool_read_as_absent()
        {
            var material = new JObject { ["extras"] = new JObject { ["blender"] = "custom" } };

            Assert.That(RenderStateReader.TryReadExtras(material, out _), Is.False);
        }

        [Test]
        public void The_uv_animation_in_the_extras_is_reachable_through_the_reader()
        {
            var material = new JObject
            {
                ["extras"] = Carrying(new MaterialExtras
                {
                    UvAnimation = new UvAnimationExtras { FramesPerSecond = 12f },
                }),
            };

            Assert.That(RenderStateReader.TryReadExtras(material, out var extras), Is.True);
            Assert.That(extras.UvAnimation.FramesPerSecond, Is.EqualTo(12f));
        }

        private static JObject Carrying(MaterialExtras extras)
        {
            return new JObject { [MaterialExtras.Key] = extras.ToJson() };
        }
    }
}
