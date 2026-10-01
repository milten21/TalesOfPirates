using NUnit.Framework;
using Top.Conversion.Pipeline;
using Top.Conversion.Pipeline.Maps;

namespace Top.Conversion.Tests.Pipeline
{
    public class ConversionSettingsTests
    {
        private static readonly ConversionSettings Settings =
            new ConversionSettings("C:/client/assets", "C:/project/Assets/Content");

        [Test]
        public void The_map_chunk_size_defaults_to_the_map_builder_default()
        {
            Assert.That(Settings.MapChunkSize, Is.EqualTo(MapBuilder.DefaultChunkSize));
        }

        [Test]
        public void The_map_chunk_size_can_be_set_explicitly()
        {
            var settings = new ConversionSettings("C:/client/assets", "C:/project/Assets/Content", mapChunkSize: 32);

            Assert.That(settings.MapChunkSize, Is.EqualTo(32));
        }

        private static string Slashed(string path) => path.Replace('\\', '/');

        [Test]
        public void Client_roots_follow_the_client_root()
        {
            Assert.That(Slashed(Settings.Source.Models), Is.EqualTo("C:/client/assets/model"));
            Assert.That(Slashed(Settings.Source.Textures), Is.EqualTo("C:/client/assets/texture"));
            Assert.That(Slashed(Settings.Source.Animations), Is.EqualTo("C:/client/assets/animation"));
        }

        [Test]
        public void Client_files_sit_under_their_root()
        {
            Assert.That(Slashed(Settings.Source.Model("scene", "stone01.lgo")),
                Is.EqualTo("C:/client/assets/model/scene/stone01.lgo"));
            Assert.That(Slashed(Settings.Source.Skeleton(724)),
                Is.EqualTo("C:/client/assets/animation/0724.lab"));
            Assert.That(Slashed(Settings.Source.Skeleton("0724")),
                Is.EqualTo("C:/client/assets/animation/0724.lab"));
            Assert.That(Slashed(Settings.Source.Table("iteminfo.txt")),
                Is.EqualTo("C:/client/assets/scripts/table/iteminfo.txt"));
            Assert.That(Slashed(Settings.Source.CharacterAction),
                Is.EqualTo("C:/client/assets/scripts/txt/CharacterAction.tx"));
        }

        [Test]
        public void Client_texture_folders_are_lowercase()
        {
            Assert.That(Slashed(Settings.Source.TextureDir("Scene")),
                Is.EqualTo("C:/client/assets/texture/scene"));
        }

        [Test]
        public void Converted_models_sit_under_their_kind_named_after_what_they_came_from()
        {
            Assert.That(Slashed(Settings.Output.Model(ContentKind.Scene, "stone01")),
                Is.EqualTo("C:/project/Assets/Content/models/scene/stone01.glb"));
            Assert.That(Slashed(Settings.Output.Model(ContentKind.Item, "01010021")),
                Is.EqualTo("C:/project/Assets/Content/models/item/01010021.glb"));
        }

        [Test]
        public void Rigs_sit_apart_from_the_models()
        {
            Assert.That(Slashed(Settings.Output.Rig("0001")),
                Is.EqualTo("C:/project/Assets/Content/rigs/0001.glb"));
        }

        [Test]
        public void Textures_are_shared_by_every_model_of_a_kind()
        {
            Assert.That(Slashed(Settings.Output.TextureDir(ContentKind.Item)),
                Is.EqualTo("C:/project/Assets/Content/textures/item"));
        }

        [Test]
        public void Every_written_segment_is_lowercase()
        {
            Assert.That(Slashed(Settings.Output.Model("Scene", "MY_BD001")),
                Is.EqualTo("C:/project/Assets/Content/models/scene/my_bd001.glb"));
            Assert.That(Slashed(Settings.Output.Rig("Hero")),
                Is.EqualTo("C:/project/Assets/Content/rigs/hero.glb"));
            Assert.That(Slashed(Settings.Output.TextureDir("Scene")),
                Is.EqualTo("C:/project/Assets/Content/textures/scene"));
        }
    }
}
