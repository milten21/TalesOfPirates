using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NUnit.Framework;
using Top.Client.Core;
using Top.Client.Features.MainMenu.Servers;

namespace Top.Client.Tests.Core
{
    public class JsonFileTests
    {
        private string _path;

        [SetUp]
        public void CreateTempPath()
        {
            _path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        [TearDown]
        public void RemoveTempFile()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }

        [Test]
        public void What_is_written_comes_back_unchanged()
        {
            var written = new List<ServerEntry>
            {
                new ServerEntry { Name = "Home", Host = "127.0.0.1", Port = 1973 },
                new ServerEntry { Name = "Live", Host = "top.example.com", Port = 2000 }
            };

            JsonFile.Write(_path, written);

            List<ServerEntry> read = JsonFile.Read<List<ServerEntry>>(_path);

            Assert.That(read, Has.Count.EqualTo(2));
            Assert.That(read[0].Name, Is.EqualTo("Home"));
            Assert.That(read[0].Host, Is.EqualTo("127.0.0.1"));
            Assert.That(read[0].Port, Is.EqualTo(1973));
            Assert.That(read[1].Name, Is.EqualTo("Live"));
            Assert.That(read[1].Host, Is.EqualTo("top.example.com"));
            Assert.That(read[1].Port, Is.EqualTo(2000));
        }

        [Test]
        public void An_empty_list_comes_back_empty()
        {
            JsonFile.Write(_path, new List<ServerEntry>());

            Assert.That(JsonFile.Read<List<ServerEntry>>(_path), Is.Empty);
        }

        [Test]
        public void A_file_that_is_not_there_says_so()
        {
            Assert.That(() => JsonFile.Read<List<ServerEntry>>(_path), Throws.InstanceOf<FileNotFoundException>());
        }

        [Test]
        public void A_file_that_does_not_parse_says_so()
        {
            File.WriteAllText(_path, "{ this is not json");

            Assert.That(() => JsonFile.Read<List<ServerEntry>>(_path), Throws.InstanceOf<JsonException>());
        }

        [Test]
        public void A_write_replaces_what_was_there()
        {
            JsonFile.Write(_path, new List<ServerEntry> { new ServerEntry { Name = "First", Host = "a", Port = 1 } });
            JsonFile.Write(_path, new List<ServerEntry> { new ServerEntry { Name = "Second", Host = "b", Port = 2 } });

            List<ServerEntry> read = JsonFile.Read<List<ServerEntry>>(_path);

            Assert.That(read, Has.Count.EqualTo(1));
            Assert.That(read[0].Name, Is.EqualTo("Second"));
        }
    }
}
