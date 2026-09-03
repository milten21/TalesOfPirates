using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Top.Content.Tests
{
    public abstract class ContentSourceTests
    {
        protected abstract IContentSource Create(params string[] paths);

        protected static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

        [Test]
        public async Task A_read_gives_back_the_bytes_behind_the_path()
        {
            var content = Create("models/item/0002090001.glb", "rigs/0001.glb");

            Assert.That(await content.Read("models/item/0002090001.glb"),
                Is.EqualTo(Bytes("models/item/0002090001.glb")));
        }

        [Test]
        public async Task A_read_finds_the_file_whatever_case_it_asks_in()
        {
            // Scene models reference textures in a case the file on disk disagrees
            // with, so exact-name lookup is not an option.
            var content = Create("textures/scene/A_Tree.PNG");

            Assert.That(await content.Read("textures/scene/a_tree.png"),
                Is.EqualTo(Bytes("textures/scene/A_Tree.PNG")));
            Assert.That(await content.Read("TEXTURES/SCENE/A_TREE.PNG"),
                Is.EqualTo(Bytes("textures/scene/A_Tree.PNG")));
        }

        [Test]
        public void A_read_of_a_path_that_is_not_held_throws()
        {
            var content = Create("models/item/0002090001.glb");

            Assert.That(() => content.Read("models/item/nothing.glb"),
                Throws.InstanceOf<FileNotFoundException>());
            Assert.That(() => content.Read("models/nowhere/nothing.glb"),
                Throws.InstanceOf<FileNotFoundException>());
        }

        [Test]
        public void A_read_of_a_missing_path_faults_its_task_rather_than_the_call()
        {
            var content = Create("models/item/0002090001.glb");

            var read = content.Read("models/item/nothing.glb");

            Assert.That(async () => await read, Throws.InstanceOf<FileNotFoundException>());
        }

        [Test]
        public async Task A_read_hands_out_bytes_the_next_read_does_not_share()
        {
            var content = Create("models/item/0002090001.glb");

            var first = await content.Read("models/item/0002090001.glb");
            first[0] = 0;

            Assert.That(await content.Read("models/item/0002090001.glb"),
                Is.EqualTo(Bytes("models/item/0002090001.glb")));
        }

        [Test]
        public void Exists_tells_a_held_path_from_a_missing_one()
        {
            var content = Create("models/scene/my_bd001.glb");

            Assert.That(content.Exists("models/scene/my_bd001.glb"), Is.True);
            Assert.That(content.Exists("models/scene/my_bd002.glb"), Is.False);
            Assert.That(content.Exists("models/scene"), Is.False);
        }

        [Test]
        public void Exists_ignores_case_the_way_a_read_does()
        {
            var content = Create("textures/scene/A_Tree.PNG");

            Assert.That(content.Exists("textures/scene/a_tree.png"), Is.True);
        }

        [Test]
        public void List_gives_every_path_under_the_prefix_in_order()
        {
            var content = Create(
                "models/scene/my_bd001.glb",
                "models/character/0000000000.glb",
                "models/item/0002090001.glb",
                "rigs/0001.glb");

            Assert.That(content.List("models"), Is.EqualTo(new[]
            {
                "models/character/0000000000.glb",
                "models/item/0002090001.glb",
                "models/scene/my_bd001.glb"
            }));
        }

        [Test]
        public void List_takes_everything_on_an_empty_prefix()
        {
            var content = Create("rigs/0001.glb", "models/item/0002090001.glb");

            Assert.That(content.List(string.Empty), Is.EqualTo(new[]
            {
                "models/item/0002090001.glb",
                "rigs/0001.glb"
            }));
        }

        [Test]
        public void List_matches_whole_segments()
        {
            var content = Create("textures/scene/010297.png", "textures/scenery/tree.png");

            Assert.That(content.List("textures/scene"), Is.EqualTo(new[] { "textures/scene/010297.png" }));
            Assert.That(content.List("textures/scene/"), Is.EqualTo(new[] { "textures/scene/010297.png" }));
        }

        [Test]
        public void Listed_paths_come_back_lowercase()
        {
            var content = Create("textures/Scene/A_Tree.PNG");

            Assert.That(content.List("textures"), Is.EqualTo(new[] { "textures/scene/a_tree.png" }));
        }

        [Test]
        public void List_ignores_the_case_of_the_prefix()
        {
            var content = Create("textures/scene/010297.png");

            Assert.That(content.List("TEXTURES/Scene"), Is.EqualTo(new[] { "textures/scene/010297.png" }));
        }

        [Test]
        public void List_of_a_prefix_that_is_not_held_is_empty()
        {
            var content = Create("models/item/0002090001.glb");

            Assert.That(content.List("maps"), Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("/models/item/0002090001.glb")]
        [TestCase("models\\item\\0002090001.glb")]
        [TestCase("models/../../elsewhere.glb")]
        [TestCase("models/./item/0002090001.glb")]
        [TestCase("models//item/0002090001.glb")]
        [TestCase("models/item/")]
        public void A_path_that_breaks_the_rules_is_refused(string path)
        {
            var content = Create("models/item/0002090001.glb");

            Assert.That(() => content.Read(path), Throws.InstanceOf<ArgumentException>());
            Assert.That(() => content.Exists(path), Throws.InstanceOf<ArgumentException>());
        }

        [TestCase(null)]
        [TestCase("/models")]
        [TestCase("models\\item")]
        [TestCase("models/..")]
        [TestCase("models//item")]
        public void A_prefix_that_breaks_the_rules_is_refused(string prefix)
        {
            var content = Create("models/item/0002090001.glb");

            Assert.That(() => content.List(prefix), Throws.InstanceOf<ArgumentException>());
        }
    }
}
