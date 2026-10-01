using Godot;
using NUnit.Framework;
using Top.Client.Features.Maps;
using Top.Contracts.Assets.Maps;
using Top.Contracts.Tables.World;

namespace Top.Client.Tests.Features.Maps
{
    public class DestinationLookupTests
    {
        private const int ChunkSize = 4;

        private const ushort LandRegion = 0x1;

        private const ushort NotFightRegion = 0x2;

        private const ushort PlayerKillingRegion = 0x4;

        private MapFile _mapFile;
        private DestinationLookup _lookup;

        [SetUp]
        public void SetUp()
        {
            _mapFile = new MapFile(ChunkSize, ChunkSize, ChunkSize);
            _mapFile.Chunks[0, 0] = new MapChunk(ChunkSize);

            var map = new MapEntry { Id = 1, Name = "garner", DisplayName = "Ascaron" };
            var areas = new AreaTable([
                new AreaEntry { Id = 1, Name = "Argent City", IsCity = true },
                new AreaEntry { Id = 2, Name = "Andes Forest Haven" },
                new AreaEntry { Id = 3 }
            ]);

            _lookup = new DestinationLookup(map, areas, new MapGrid(_mapFile));
        }

        [Test]
        public void A_city_area_in_a_safe_tile_is_a_safe_city()
        {
            SetTile(1, 2, area: 1, LandRegion | NotFightRegion);

            var destination = _lookup.FindAt(new Vector2(1.5f, 2.5f));

            Assert.That(destination, Is.EqualTo(new Destination("Argent City", ZoneType.City, ZoneRule.Safe)));
        }

        [Test]
        public void A_field_area_in_a_player_killing_tile_is_a_player_killing_field()
        {
            SetTile(1, 2, area: 2, LandRegion | PlayerKillingRegion);

            var destination = _lookup.FindAt(new Vector2(1.5f, 2.5f));

            Assert.That(destination,
                Is.EqualTo(new Destination("Andes Forest Haven", ZoneType.Field, ZoneRule.PlayerKilling)));
        }

        [Test]
        public void A_field_area_in_a_tile_with_neither_flag_is_a_battle_field()
        {
            SetTile(1, 2, area: 2, LandRegion);

            var destination = _lookup.FindAt(new Vector2(1.5f, 2.5f));

            Assert.That(destination, Is.EqualTo(new Destination("Andes Forest Haven", ZoneType.Field, ZoneRule.Battle)));
        }

        [Test]
        public void A_tile_with_no_area_is_titled_after_the_map_and_has_no_type()
        {
            SetTile(1, 2, area: 0, LandRegion | NotFightRegion);

            var destination = _lookup.FindAt(new Vector2(1.5f, 2.5f));

            Assert.That(destination, Is.EqualTo(new Destination("Ascaron", null, ZoneRule.Safe)));
        }

        [Test]
        public void A_tile_with_an_area_the_table_does_not_know_is_titled_after_the_map_and_has_no_type()
        {
            SetTile(1, 2, area: 9, LandRegion);

            var destination = _lookup.FindAt(new Vector2(1.5f, 2.5f));

            Assert.That(destination, Is.EqualTo(new Destination("Ascaron", null, ZoneRule.Battle)));
        }

        [Test]
        public void An_area_with_no_name_is_titled_after_the_map_and_keeps_its_type()
        {
            SetTile(1, 2, area: 3, LandRegion);

            var destination = _lookup.FindAt(new Vector2(1.5f, 2.5f));

            Assert.That(destination, Is.EqualTo(new Destination("Ascaron", ZoneType.Field, ZoneRule.Battle)));
        }

        [Test]
        public void A_tile_with_both_the_not_fight_and_the_player_killing_flags_is_safe()
        {
            SetTile(1, 2, area: 2, LandRegion | NotFightRegion | PlayerKillingRegion);

            var destination = _lookup.FindAt(new Vector2(1.5f, 2.5f));

            Assert.That(destination.ZoneRule, Is.EqualTo(ZoneRule.Safe));
        }

        [Test]
        public void The_tile_is_the_one_under_the_point()
        {
            SetTile(1, 2, area: 1, NotFightRegion);
            SetTile(2, 2, area: 2, PlayerKillingRegion);

            Assert.That(_lookup.FindAt(new Vector2(1.99f, 2f)).Title, Is.EqualTo("Argent City"));
            Assert.That(_lookup.FindAt(new Vector2(2f, 2f)).Title, Is.EqualTo("Andes Forest Haven"));
        }

        private void SetTile(int tileX, int tileY, byte area, int region)
        {
            _mapFile.Chunks[0, 0].Tiles[(tileY * ChunkSize) + tileX] = new MapTile
            {
                Island = area,
                Region = (ushort)region,
            };
        }
    }
}
