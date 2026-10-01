using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Top.Conversion.Pipeline;
using Top.Legacy.Tables;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Tests.Pipeline
{
    public class SceneObjectConverterTests
    {
        private FakeClient _client;
        private CaptureLog _log;

        [SetUp]
        public void SetUp()
        {
            _client = new FakeClient();
            _log = new CaptureLog();
        }

        [TearDown]
        public void TearDown()
        {
            _log.Dispose();
            _client.Dispose();
        }

        private static SceneObjectInfoRecord Row(int id, string fileName)
        {
            return new SceneObjectInfoRecord { Id = id, Name = fileName };
        }

        private SceneObjectConverter Converter(params SceneObjectInfoRecord[] rows)
        {
            var settings = _client.Settings();
            var tables = new ClientTables(null, null,
                new Table<SceneObjectInfoRecord>([..rows]), null);

            return new SceneObjectConverter(settings, tables, new ModelConverter(settings, tables));
        }

        [Test]
        public void Converts_the_model_the_row_names_and_keeps_the_row_id()
        {
            _client.AddModel("scene", "lgo/stone01.lgo");

            var result = Converter(Row(42, "stone01.lgo")).Convert(42);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(result.Id, Is.EqualTo(42), "what a placement in a map refers to");
            Assert.That(result.Model.Kind, Is.EqualTo("scene"));
            Assert.That(result.Model.ModelPath, Is.EqualTo(_client.Converted("scene", "stone01")));
        }

        [Test]
        public void A_row_with_no_model_file_fails()
        {
            var result = Converter(Row(42, string.Empty)).Convert(42);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(result.Artifacts, Is.Empty);
            Assert.That(_log.Errors, Has.Some.Contains("no sceneobjinfo row 42"));
        }

        [Test]
        public void An_unknown_id_fails()
        {
            Assert.That(Converter().Convert(42).Outcome, Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(_log.Errors, Has.Some.Contains("no sceneobjinfo row 42"));
        }

        [Test]
        public void A_row_whose_model_file_is_gone_fails()
        {
            var result = Converter(Row(42, "stone01.lgo")).Convert(42);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(_log.Errors, Has.Some.Contains("stone01.lgo"));
        }

        [Test]
        public void Rows_sharing_a_model_file_convert_once()
        {
            _client.AddModel("scene", "lgo/stone01.lgo");

            var results = Converter(Row(42, "stone01.lgo"), Row(43, "stone01.lgo"))
                .ConvertAll()
                .ToList();

            Assert.That(results.Select(result => result.Outcome),
                Is.EqualTo(new[] { ConversionOutcome.Converted, ConversionOutcome.Skipped }));
        }

        [Test]
        public void A_batch_reports_the_unit_it_is_about_to_convert()
        {
            _client.AddModel("scene", "lgo/stone01.lgo");
            var progress = new RecordedProgress();

            Converter(Row(42, "stone01.lgo"), Row(43, "stone01.lgo"))
                .ConvertAll(progress)
                .ToList();

            Assert.That(progress.Steps.Select(step => step.Unit),
                Is.EqualTo(new[] { "scene object 42", "scene object 43" }));
            Assert.That(progress.Steps[0].Count, Is.EqualTo(2));
            Assert.That(progress.Steps[1].Fraction, Is.EqualTo(0.5f));
        }

        [Test]
        public void A_canceled_batch_stops_between_units()
        {
            _client.AddModel("scene", "lgo/stone01.lgo");

            using var cancellation = new CancellationTokenSource();
            var results = new List<SceneObjectResult>();

            foreach (var result in Converter(Row(42, "stone01.lgo"), Row(43, "stone01.lgo"))
                         .ConvertAll(cancellation: cancellation.Token))
            {
                results.Add(result);
                cancellation.Cancel();
            }

            Assert.That(results, Has.Count.EqualTo(1));
        }
    }
}
