using Godot;
using NUnit.Framework;
using Top.Client.UI.Cursors;

namespace Top.Client.Tests.UI.Cursors
{
    public class CursorKeyResolverTests
    {
        [TestCase(Input.CursorShape.Arrow, "normal")]
        [TestCase(Input.CursorShape.Cross, "normal")]
        [TestCase(Input.CursorShape.Help, "normal")]
        [TestCase(Input.CursorShape.PointingHand, "active")]
        [TestCase(Input.CursorShape.Busy, "wait")]
        [TestCase(Input.CursorShape.Wait, "wait")]
        [TestCase(Input.CursorShape.Drag, "drag")]
        [TestCase(Input.CursorShape.CanDrop, "drag")]
        [TestCase(Input.CursorShape.Move, "drag")]
        [TestCase(Input.CursorShape.Vsize, "vertical")]
        [TestCase(Input.CursorShape.Hsize, "drag")]
        [TestCase(Input.CursorShape.Bdiagsize, "drag")]
        [TestCase(Input.CursorShape.Fdiagsize, "drag")]
        [TestCase(Input.CursorShape.Forbidden, "forbid")]
        public void A_shape_in_the_map_gives_its_key(Input.CursorShape shape, string expectedKey)
        {
            Assert.That(CursorKeyResolver.KeyFor(shape)?.Name, Is.EqualTo(expectedKey));
        }

        [Test]
        public void The_text_shape_has_no_art()
        {
            Assert.That(CursorKeyResolver.KeyFor(Input.CursorShape.Ibeam), Is.Null);
        }

        [Test]
        public void A_shape_that_the_map_does_not_name_has_no_art()
        {
            Assert.That(CursorKeyResolver.KeyFor(Input.CursorShape.Vsplit), Is.Null);
            Assert.That(CursorKeyResolver.KeyFor(Input.CursorShape.Hsplit), Is.Null);
        }

        [Test]
        public void The_shape_of_the_control_under_the_mouse_gives_its_key()
        {
            Assert.That(CursorKeyResolver.Resolve(Input.CursorShape.PointingHand, isWaiting: false),
                Is.SameAs(CursorKey.Active));
        }

        [Test]
        public void The_control_under_the_mouse_wins_over_wait()
        {
            Assert.That(CursorKeyResolver.Resolve(Input.CursorShape.PointingHand, isWaiting: true),
                Is.SameAs(CursorKey.Active));
            Assert.That(CursorKeyResolver.Resolve(Input.CursorShape.Arrow, isWaiting: true),
                Is.SameAs(CursorKey.Normal));
        }

        [Test]
        public void The_text_control_under_the_mouse_keeps_the_operating_system_cursor_also_while_waiting()
        {
            Assert.That(CursorKeyResolver.Resolve(Input.CursorShape.Ibeam, isWaiting: false), Is.Null);
            Assert.That(CursorKeyResolver.Resolve(Input.CursorShape.Ibeam, isWaiting: true), Is.Null);
        }

        [Test]
        public void Wait_wins_when_no_control_is_under_the_mouse()
        {
            Assert.That(CursorKeyResolver.Resolve(null, isWaiting: true), Is.SameAs(CursorKey.Wait));
        }

        [Test]
        public void Normal_shows_when_no_control_is_under_the_mouse_and_there_is_no_wait()
        {
            Assert.That(CursorKeyResolver.Resolve(null, isWaiting: false), Is.SameAs(CursorKey.Normal));
        }

        [Test]
        public void A_held_action_wins_over_a_control_under_the_mouse()
        {
            Assert.That(CursorKeyResolver.Resolve(Input.CursorShape.PointingHand, isWaiting: false,
                heldAction: CursorKey.Camera), Is.SameAs(CursorKey.Camera));
        }

        [Test]
        public void A_held_action_wins_over_wait_and_over_a_world_hover()
        {
            Assert.That(CursorKeyResolver.Resolve(null, isWaiting: true, worldHover: CursorKey.Attack,
                heldAction: CursorKey.Camera), Is.SameAs(CursorKey.Camera));
        }

        [Test]
        public void A_held_action_wins_over_the_text_control()
        {
            Assert.That(CursorKeyResolver.Resolve(Input.CursorShape.Ibeam, isWaiting: false,
                heldAction: CursorKey.Camera), Is.SameAs(CursorKey.Camera));
        }

        [Test]
        public void Wait_wins_over_a_world_hover()
        {
            Assert.That(CursorKeyResolver.Resolve(null, isWaiting: true, worldHover: CursorKey.Attack),
                Is.SameAs(CursorKey.Wait));
        }

        [Test]
        public void A_world_hover_shows_when_no_control_is_under_the_mouse()
        {
            Assert.That(CursorKeyResolver.Resolve(null, isWaiting: false, worldHover: CursorKey.Attack),
                Is.SameAs(CursorKey.Attack));
        }

        [Test]
        public void A_world_hover_does_not_show_when_a_control_is_under_the_mouse()
        {
            Assert.That(CursorKeyResolver.Resolve(Input.CursorShape.Arrow, isWaiting: false,
                worldHover: CursorKey.Attack), Is.SameAs(CursorKey.Normal));
            Assert.That(CursorKeyResolver.Resolve(Input.CursorShape.PointingHand, isWaiting: false,
                worldHover: CursorKey.Attack), Is.SameAs(CursorKey.Active));
            Assert.That(CursorKeyResolver.Resolve(Input.CursorShape.Ibeam, isWaiting: false,
                worldHover: CursorKey.Attack), Is.Null);
        }
    }
}
