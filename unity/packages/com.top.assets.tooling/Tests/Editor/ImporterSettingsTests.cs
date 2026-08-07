using System.IO;
using NUnit.Framework;

namespace Top.Assets.Tooling.Tests
{
    public class ImporterSettingsTests
    {
        private string _saved;

        [SetUp]
        public void SetUp()
        {
            _saved = ImporterSettings.instance.RawClientRoot;
        }

        [TearDown]
        public void TearDown()
        {
            ImporterSettings.instance.ClientRoot = _saved;
        }

        [Test]
        public void PipelineSettingsCarryTheConfiguredRoots()
        {
            ImporterSettings.instance.ClientRoot = "C:/client/assets";
            ImporterSettings.instance.Overwrite = true;

            var settings = ImporterSettings.instance.Conversion();

            Assert.That(settings.ClientRoot, Is.EqualTo("C:/client/assets"));
            Assert.That(settings.OutputRoot, Is.EqualTo(Path.GetFullPath("Assets/Content")));
            Assert.That(settings.Overwrite, Is.True);
            Assert.That(settings.Source.Models.Replace('\\', '/'), Is.EqualTo("C:/client/assets/model"));
        }

        [Test]
        public void MissingRootIsInvalid()
        {
            ImporterSettings.instance.ClientRoot = "C:/no/such/folder";

            Assert.That(ImporterSettings.instance.IsValid, Is.False);
        }
    }
}
