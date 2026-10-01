using NUnit.Framework;
using Top.Client.Features.Maps;
using Top.Content;
using Top.Contracts.Tables.World;

namespace Top.Client.Tests.Features.Maps
{
    public class MapLookupTests
    {
        private MemoryContentSource _content;
        private MapLookup _lookup;

        [SetUp]
        public void SetUp()
        {
            _content = new MemoryContentSource();
            _content.Add("maps/garner.map", [1]);

            _lookup = new MapLookup(new MapTable([
                new MapEntry { Id = 1, Name = "garner", MapPath = "maps/garner.map", DisplayName = "Ascaron" },
                new MapEntry { Id = 6, Name = "secretgarden", MapPath = "maps/secretgarden.map" }
            ]), _content);
        }

        [Test]
        public void A_name_the_map_table_knows_with_its_file_converted_is_found()
        {
            Assert.That(_lookup.FindByName("garner").DisplayName, Is.EqualTo("Ascaron"));
        }

        [Test]
        public void A_name_is_matched_with_its_case()
        {
            Assert.Throws<MapNotFoundException>(() => _lookup.FindByName("Garner"));
        }

        [Test]
        public void A_name_the_map_table_does_not_know_is_not_found()
        {
            Assert.Throws<MapNotFoundException>(() => _lookup.FindByName("chaosicicle"));
        }

        [Test]
        public void A_name_the_map_table_knows_whose_file_was_never_converted_is_missing_its_file()
        {
            var missing = Assert.Throws<MapFileNotFoundException>(() => _lookup.FindByName("secretgarden"));

            Assert.That(missing.Map.Id, Is.EqualTo(6));
        }

        [Test]
        public void An_id_is_found_or_fails_the_same_ways_a_name_is()
        {
            Assert.That(_lookup.FindById(1).Name, Is.EqualTo("garner"));
            Assert.Throws<MapNotFoundException>(() => _lookup.FindById(99));
            Assert.Throws<MapFileNotFoundException>(() => _lookup.FindById(6));
        }
    }
}
