using System;
using NUnit.Framework;
using Top.Client.Features.Camera;

namespace Top.Client.Tests.Features.Camera
{
    public class FollowCameraPoseTests
    {
        private const float Precision = 1e-3f;

        [Test]
        public void A_new_camera_is_zoomed_fully_out()
        {
            var pose = CreatePose();

            Assert.That(pose.Distance, Is.EqualTo(35f).Within(Precision));
            Assert.That(pose.Height, Is.EqualTo(50f).Within(Precision));
            Assert.That(pose.Fov, Is.EqualTo(32f).Within(Precision));
        }

        [Test]
        public void A_new_camera_looks_from_a_fixed_world_yaw()
        {
            var pose = CreatePose();

            Assert.That(pose.EyeOffset.X, Is.EqualTo(0f).Within(Precision));
            Assert.That(pose.EyeOffset.Z, Is.EqualTo(20.07f).Within(0.01f));
        }

        [Test]
        public void Zooming_fully_in_reaches_the_start_of_every_range()
        {
            var pose = CreatePose();

            pose.ZoomBy(-1f);
            Settle(pose);

            Assert.That(pose.Distance, Is.EqualTo(50f).Within(Precision));
            Assert.That(pose.Height, Is.EqualTo(10f).Within(Precision));
            Assert.That(pose.Fov, Is.EqualTo(12f).Within(Precision));
        }

        [Test]
        public void Halfway_through_the_zoom_every_value_is_halfway()
        {
            var pose = CreatePose();

            pose.ZoomBy(-0.5f);
            Settle(pose);

            Assert.That(pose.Distance, Is.EqualTo(42.5f).Within(Precision));
            Assert.That(pose.Height, Is.EqualTo(30f).Within(Precision));
            Assert.That(pose.Fov, Is.EqualTo(22f).Within(Precision));
        }

        [Test]
        public void The_zoom_cannot_leave_its_range()
        {
            var pose = CreatePose();

            pose.ZoomBy(1f);
            Settle(pose);

            Assert.That(pose.Zoom, Is.EqualTo(1f).Within(Precision));

            pose.ZoomBy(-3f);
            Settle(pose);

            Assert.That(pose.Zoom, Is.EqualTo(0f).Within(Precision));
        }

        [TestCase(0f, 68.1f)]
        [TestCase(-1f, 19.2f)]
        public void The_shortening_gives_the_original_pitch(float zoomBy, float degrees)
        {
            var pose = CreatePose();

            pose.ZoomBy(zoomBy);
            Settle(pose);

            Assert.That(MeasurePitch(pose), Is.EqualTo(degrees).Within(0.05f));
        }

        [Test]
        public void Turning_moves_the_eye_round_the_target_at_once()
        {
            var pose = CreatePose();

            pose.YawBy(float.DegreesToRadians(90f));

            Assert.That(pose.EyeOffset.X, Is.EqualTo(20.07f).Within(0.01f));
            Assert.That(pose.EyeOffset.Y, Is.EqualTo(50f).Within(Precision));
            Assert.That(pose.EyeOffset.Z, Is.EqualTo(0f).Within(Precision));
        }

        [Test]
        public void A_zoom_step_starts_at_once_and_glides_in()
        {
            var pose = CreatePose();

            pose.ZoomBy(-0.5f);
            pose.Advance(1f / 60f);

            Assert.That(pose.Zoom, Is.LessThan(1f).And.GreaterThan(0.5f));
        }

        [Test]
        public void A_second_zoom_step_adds_to_the_first()
        {
            var pose = CreatePose();

            pose.ZoomBy(-0.25f);
            pose.Advance(0.1f);
            pose.ZoomBy(-0.25f);
            Settle(pose);

            Assert.That(pose.Zoom, Is.EqualTo(0.5f).Within(Precision));
        }

        [Test]
        public void The_zoom_travel_does_not_depend_on_the_frame_rate()
        {
            var slow = CreatePose();
            var fast = CreatePose();

            slow.ZoomBy(-0.5f);
            fast.ZoomBy(-0.5f);

            for (var frame = 0; frame < 6; frame++)
            {
                slow.Advance(1f / 30f);
            }

            for (var frame = 0; frame < 12; frame++)
            {
                fast.Advance(1f / 60f);
            }

            Assert.That(slow.Zoom, Is.LessThan(1f).And.GreaterThan(0.5f));
            Assert.That(fast.Zoom, Is.EqualTo(slow.Zoom).Within(1e-5f));
        }

        [Test]
        public void Recentring_turns_back_and_zooms_fully_out()
        {
            var pose = CreatePose();

            pose.YawBy(1f);
            pose.ZoomBy(-1f);
            Settle(pose);

            pose.Recentre(-0.6f);
            Settle(pose);

            Assert.That(pose.Yaw, Is.EqualTo(-0.6f).Within(Precision));
            Assert.That(pose.Zoom, Is.EqualTo(1f).Within(Precision));
        }

        [Test]
        public void Recentring_turns_the_short_way_round()
        {
            var pose = CreatePose();
            var start = float.DegreesToRadians(170f);

            pose.YawBy(start);
            pose.Recentre(float.DegreesToRadians(-35f));
            pose.Advance(1f / 60f);

            Assert.That(pose.Yaw, Is.GreaterThan(start), "170 to -35 is 155 degrees through 180, not 205 through 0");

            Settle(pose);

            Assert.That(pose.EyeOffset.X, Is.EqualTo(-11.51f).Within(0.01f));
            Assert.That(pose.EyeOffset.Z, Is.EqualTo(16.44f).Within(0.01f));
        }

        [Test]
        public void Turning_during_a_recentre_cancels_its_turn_but_not_its_zoom()
        {
            var pose = CreatePose();

            pose.ZoomBy(-1f);
            pose.YawBy(1f);
            Settle(pose);

            pose.Recentre(0f);
            pose.Advance(0.1f);

            var turned = pose.Yaw;

            pose.YawBy(0.2f);
            Settle(pose);

            Assert.That(pose.Yaw, Is.EqualTo(turned + 0.2f).Within(Precision));
            Assert.That(pose.Zoom, Is.EqualTo(1f).Within(Precision));
        }

        [Test]
        public void A_new_camera_shows_the_default_view()
        {
            Assert.That(CreatePose().IsDefaultView, Is.True);
        }

        [Test]
        public void Turning_leaves_the_default_view()
        {
            var pose = CreatePose();

            pose.YawBy(0.1f);

            Assert.That(pose.IsDefaultView, Is.False);
        }

        [Test]
        public void Zooming_in_leaves_the_default_view()
        {
            var pose = CreatePose();

            pose.ZoomBy(-0.5f);
            Settle(pose);

            Assert.That(pose.IsDefaultView, Is.False);
        }

        [Test]
        public void A_settled_recentre_makes_its_yaw_the_default()
        {
            var pose = CreatePose();

            pose.YawBy(1f);
            pose.Recentre(-0.6f);
            Settle(pose);

            Assert.That(pose.IsDefaultView, Is.True);

            pose.YawBy(0.6f);

            Assert.That(pose.IsDefaultView, Is.False, "the arrival yaw is no longer the default");
        }

        [Test]
        public void Scrolling_past_an_end_is_not_banked()
        {
            var pose = CreatePose();

            pose.ZoomBy(2f);
            pose.ZoomBy(-0.5f);
            Settle(pose);

            Assert.That(pose.Zoom, Is.EqualTo(0.5f).Within(Precision),
                "a zoom out at the wide end must not have to be scrolled back first");
        }

        [Test]
        public void Placing_the_focus_moves_it_at_once()
        {
            var pose = CreatePose();

            pose.PlaceFocus(4f);

            Assert.That(pose.FocusHeight, Is.EqualTo(4f).Within(Precision));
        }

        [Test]
        public void A_height_change_inside_the_dead_zone_does_not_move_the_focus()
        {
            var pose = CreatePose();

            pose.PlaceFocus(2f);
            pose.TrackFocus(2.4f);
            Settle(pose);
            pose.TrackFocus(1.6f);
            Settle(pose);

            Assert.That(pose.FocusHeight, Is.EqualTo(2f).Within(Precision));
        }

        [Test]
        public void The_focus_stays_the_dead_zone_behind_a_climb()
        {
            var pose = CreatePose();

            pose.PlaceFocus(0f);
            pose.TrackFocus(3f);
            Settle(pose);

            Assert.That(pose.FocusHeight, Is.EqualTo(2.5f).Within(Precision));
        }

        [Test]
        public void A_height_step_glides_the_focus_in()
        {
            var pose = CreatePose();

            pose.PlaceFocus(0f);
            pose.TrackFocus(3f);
            pose.Advance(1f / 60f);

            Assert.That(pose.FocusHeight, Is.GreaterThan(0f).And.LessThan(0.5f));
        }

        private static void Settle(FollowCameraPose pose)
        {
            pose.Advance(10f);
        }

        private static float MeasurePitch(FollowCameraPose pose)
        {
            var offset = pose.EyeOffset;
            var across = MathF.Sqrt((offset.X * offset.X) + (offset.Z * offset.Z));

            return float.RadiansToDegrees(MathF.Atan2(offset.Y, across));
        }

        private static FollowCameraPose CreatePose()
        {
            return new FollowCameraPose(
                new CameraRange(50f, 35f),
                new CameraRange(10f, 50f),
                new CameraRange(12f, 32f),
                0.5735f,
                2.5f,
                0.5f,
                3f);
        }
    }
}
