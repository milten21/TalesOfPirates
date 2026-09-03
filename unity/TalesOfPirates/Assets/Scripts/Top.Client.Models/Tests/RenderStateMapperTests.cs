using NUnit.Framework;
using Top.Client.Models.Materials;
using Top.Contracts.Assets.Models.Materials;
using UnityEngine;
using UnityEngine.Rendering;

namespace Top.Client.Models.Tests
{
    public class RenderStateMapperTests
    {
        [Test]
        public void Translates_blend_factors_and_cull_modes()
        {
            Assert.That(RenderStateMapper.ToUnity(BlendFactor.Zero), Is.EqualTo(BlendMode.Zero));
            Assert.That(RenderStateMapper.ToUnity(BlendFactor.One), Is.EqualTo(BlendMode.One));
            Assert.That(RenderStateMapper.ToUnity(BlendFactor.SrcColor), Is.EqualTo(BlendMode.SrcColor));
            Assert.That(RenderStateMapper.ToUnity(BlendFactor.OneMinusSrcColor),
                Is.EqualTo(BlendMode.OneMinusSrcColor));
            Assert.That(RenderStateMapper.ToUnity(BlendFactor.SrcAlpha), Is.EqualTo(BlendMode.SrcAlpha));
            Assert.That(RenderStateMapper.ToUnity(BlendFactor.OneMinusSrcAlpha),
                Is.EqualTo(BlendMode.OneMinusSrcAlpha));
            Assert.That(RenderStateMapper.ToUnity(BlendFactor.DstAlpha), Is.EqualTo(BlendMode.DstAlpha));
            Assert.That(RenderStateMapper.ToUnity(BlendFactor.OneMinusDstAlpha),
                Is.EqualTo(BlendMode.OneMinusDstAlpha));
            Assert.That(RenderStateMapper.ToUnity(BlendFactor.DstColor), Is.EqualTo(BlendMode.DstColor));
            Assert.That(RenderStateMapper.ToUnity(BlendFactor.OneMinusDstColor),
                Is.EqualTo(BlendMode.OneMinusDstColor));

            Assert.That(RenderStateMapper.ToUnity(FaceCulling.None), Is.EqualTo(CullMode.Off));
            Assert.That(RenderStateMapper.ToUnity(FaceCulling.Front), Is.EqualTo(CullMode.Front));
            Assert.That(RenderStateMapper.ToUnity(FaceCulling.Back), Is.EqualTo(CullMode.Back));
        }

        [Test]
        public void Render_queue_follows_blend_and_alpha_test()
        {
            Assert.That(Create(new RenderState()).renderQueue, Is.EqualTo(2000));
            Assert.That(Create(new RenderState { AlphaTest = true }).renderQueue, Is.EqualTo(2450));
            Assert.That(Create(new RenderState { BlendEnabled = true }).renderQueue, Is.EqualTo(3000));
            Assert.That(Create(new RenderState { Opacity = 0.5f }).renderQueue, Is.EqualTo(3000));
        }

        [Test]
        public void Blended_materials_opt_out_of_shadows()
        {
            var blended = Create(new RenderState { BlendEnabled = true });
            Assert.That(blended.IsKeywordEnabled("_RECEIVE_SHADOWS_OFF"), Is.True);
            Assert.That(blended.IsKeywordEnabled("_CAST_SHADOWS_OFF"), Is.True);

            var opaque = Create(new RenderState());
            Assert.That(opaque.IsKeywordEnabled("_RECEIVE_SHADOWS_OFF"), Is.False);
            Assert.That(opaque.IsKeywordEnabled("_CAST_SHADOWS_OFF"), Is.False);
        }

        [Test]
        public void The_shader_reads_the_effective_blend_and_depth_write()
        {
            var material = Create(new RenderState { Opacity = 0.5f });

            Assert.That(material.GetFloat("_SrcBlend"), Is.EqualTo(5f), "SrcAlpha, not the One the field holds");
            Assert.That(material.GetFloat("_DstBlend"), Is.EqualTo(10f), "OneMinusSrcAlpha, not the field's Zero");
            Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(0f), "opacity outranks the field");
            Assert.That(material.GetFloat("_Opacity"), Is.EqualTo(0.5f));
        }

        [Test]
        public void Lighting_state_maps_to_the_unlit_toggle()
        {
            var unlit = Create(new RenderState { Lit = false });

            Assert.That(unlit.IsKeywordEnabled("_UNLIT_ON"), Is.True);
            Assert.That(unlit.GetFloat("_Unlit"), Is.EqualTo(1f));

            var lit = Create(new RenderState());

            Assert.That(lit.IsKeywordEnabled("_UNLIT_ON"), Is.False);
        }

        private static Material Create(RenderState state)
        {
            return RenderStateMapper.CreateMaterial(state, null, Shader.Find("Top/Model"));
        }
    }
}
