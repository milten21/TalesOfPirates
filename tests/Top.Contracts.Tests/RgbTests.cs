using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;
using Top.Contracts.Tables;

namespace Top.Contracts.Tests
{
    public class RgbTests
    {
        private class ColorEntry : TableEntry
        {
            [JsonProperty("value")] public Rgb Value;
        }

        private static string Write(Rgb value)
        {
            using var stream = new MemoryStream();

            new TableFormat().Write(stream, new[] { new ColorEntry { Id = 1, Value = value } });

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static Rgb Read(string json)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            return new TableFormat().Read<ColorEntry>(stream).Single().Value;
        }

        [Test]
        public void The_three_channels_write_as_lowercase_members()
        {
            var json = Write(new Rgb(255, 128, 0));

            Assert.That(json, Does.Contain("\"r\": 255"));
            Assert.That(json, Does.Contain("\"g\": 128"));
            Assert.That(json, Does.Contain("\"b\": 0"));
        }

        [Test]
        public void A_written_color_reads_back_the_same()
        {
            Assert.That(Read(Write(new Rgb(255, 128, 0))), Is.EqualTo(new Rgb(255, 128, 0)));
        }

        [Test]
        public void An_absent_color_reads_as_black()
        {
            Assert.That(Read("""[ { "id": 1 } ]"""), Is.EqualTo(new Rgb(0, 0, 0)));
        }
    }
}
