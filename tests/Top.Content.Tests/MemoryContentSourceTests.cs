using System;
using NUnit.Framework;

namespace Top.Content.Tests
{
    public class MemoryContentSourceTests : ContentSourceTests
    {
        protected override IContentSource Create(params string[] paths)
        {
            var content = new MemoryContentSource();

            foreach (var path in paths)
            {
                content.Add(path, Bytes(path));
            }

            return content;
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("/models/item/0002090001.glb")]
        [TestCase("models\\item\\0002090001.glb")]
        [TestCase("models/../elsewhere.glb")]
        public void A_write_to_a_path_that_breaks_the_rules_is_refused(string path)
        {
            var content = new MemoryContentSource();

            Assert.That(() => content.Add(path, Bytes("content")), Throws.InstanceOf<ArgumentException>());
        }
    }
}
