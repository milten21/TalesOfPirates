using System;
using System.IO;
using NUnit.Framework;

namespace Top.Content.Tests
{
    public class FolderContentSourceTests : ContentSourceTests
    {
        private string _root;

        [SetUp]
        public void MakeRoot()
        {
            _root = Path.Combine(Path.GetTempPath(), "top-folder-content-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void DropRoot()
        {
            Directory.Delete(_root, true);
        }

        protected override IContentSource Create(params string[] paths)
        {
            foreach (var path in paths)
            {
                var file = Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllBytes(file, Bytes(path));
            }

            return new FolderContentSource(_root);
        }
    }
}
