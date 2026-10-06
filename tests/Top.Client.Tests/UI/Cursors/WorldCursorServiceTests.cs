using Godot;
using NUnit.Framework;
using Top.Client.UI.Cursors;

namespace Top.Client.Tests.UI.Cursors
{
    public class WorldCursorServiceTests
    {
        private WorldCursorService _service = null!;

        [SetUp]
        public void CreateService()
        {
            _service = new WorldCursorService(new CursorSkin("res://"));
        }

        [Test]
        public void A_shown_cursor_applies_when_no_control_is_under_the_mouse()
        {
            _service.Show(WorldCursor.Attack);

            Assert.That(_service.Resolve(null, isWaiting: false), Is.SameAs(CursorKey.Attack));
        }

        [Test]
        public void A_shown_cursor_clears_after_the_frame()
        {
            _service.Show(WorldCursor.Attack);
            _service.EndFrame();

            Assert.That(_service.Resolve(null, isWaiting: false), Is.SameAs(CursorKey.Normal));
        }

        [Test]
        public void A_shown_cursor_does_not_apply_over_a_control()
        {
            _service.Show(WorldCursor.Attack);

            Assert.That(_service.Resolve(Input.CursorShape.Arrow, isWaiting: false), Is.SameAs(CursorKey.Normal));
        }

        [Test]
        public void A_held_cursor_stays_after_the_frame_and_over_a_control_until_release()
        {
            _service.Hold(WorldCursor.Camera);
            _service.EndFrame();

            Assert.That(_service.IsHolding, Is.True);
            Assert.That(_service.Resolve(Input.CursorShape.PointingHand, isWaiting: false),
                Is.SameAs(CursorKey.Camera));

            _service.Release();

            Assert.That(_service.IsHolding, Is.False);
            Assert.That(_service.Resolve(Input.CursorShape.PointingHand, isWaiting: false),
                Is.SameAs(CursorKey.Active));
        }

        [Test]
        public void A_held_cursor_wins_over_a_shown_cursor()
        {
            _service.Hold(WorldCursor.Camera);
            _service.Show(WorldCursor.Attack);

            Assert.That(_service.Resolve(null, isWaiting: false), Is.SameAs(CursorKey.Camera));
        }

        [Test]
        public void Wait_wins_over_a_shown_cursor()
        {
            _service.Show(WorldCursor.Attack);

            Assert.That(_service.Resolve(null, isWaiting: true), Is.SameAs(CursorKey.Wait));
        }

        [TestCase(WorldCursor.Attack, "attack")]
        [TestCase(WorldCursor.Skill, "skill")]
        [TestCase(WorldCursor.Land, "land")]
        [TestCase(WorldCursor.Board, "board")]
        [TestCase(WorldCursor.Pick, "pick")]
        [TestCase(WorldCursor.Camera, "camera")]
        [TestCase(WorldCursor.Chat, "chat")]
        [TestCase(WorldCursor.Player, "player")]
        [TestCase(WorldCursor.Hide, "hide")]
        [TestCase(WorldCursor.Search, "search")]
        [TestCase(WorldCursor.Block, "block")]
        [TestCase(WorldCursor.Repair, "repair")]
        [TestCase(WorldCursor.Feed, "feed")]
        [TestCase(WorldCursor.Forbid, "forbid")]
        [TestCase(WorldCursor.Active, "active")]
        public void Each_world_cursor_shows_its_key(WorldCursor cursor, string expectedKey)
        {
            _service.Show(cursor);

            Assert.That(_service.Resolve(null, isWaiting: false)?.Name, Is.EqualTo(expectedKey));
        }
    }
}
