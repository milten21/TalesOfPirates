using System;
using System.Linq;
using Godot;
using NUnit.Framework;
using Top.Client.UI.Cursors;

namespace Top.Client.Tests.UI.Cursors
{
    public class CursorEntriesTests
    {
        private static readonly CursorEntries Entries = CursorEntries.Read("""
            {
              "cursors": {
                "normal": { "image": "cursors/normal.png", "frame": [32, 32], "hotspot": [0, 0] },
                "pick": { "image": "cursors/pick.png", "frame": [32, 24], "hotspot": [5, 7] },
                "skill": { "image": "cursors/skill.png", "frame": [32, 32], "hotspot": [15, 15],
                           "steps": [0, 1, 2, 1], "ms": 100 },
                "search": { "image": "cursors/search.png", "frame": [32, 32], "hotspot": [4, 4],
                            "steps": [0, 1, 2], "ms": [100, 50, 200] },
                "wait": { "image": "cursors/wait.png", "frame": [32, 32], "hotspot": [16, 16], "ms": 100 },
                "repair": { "image": "cursors/repair.png", "frame": [32, 32], "hotspot": [[9, 20], [3, 4], [5, 6]],
                            "ms": 100 },
                "board": { "image": "cursors/board.png", "frame": [32, 32], "hotspot": [[1, 1], [2, 2]],
                           "steps": [0, 1, 0], "ms": [100, 100, 100] }
              }
            }
            """);

        [Test]
        public void A_declared_key_resolves_to_its_own_entry()
        {
            var entry = Entries.Resolve(CursorKey.Pick);

            Assert.That(entry?.Image, Is.EqualTo("cursors/pick.png"));
            Assert.That(entry?.Frame, Is.EqualTo(new Vector2I(32, 24)));
            Assert.That(entry?.Hotspots, Is.EqualTo(new[] { new Vector2I(5, 7) }));
        }

        [Test]
        public void A_key_that_the_theme_does_not_declare_resolves_to_normal()
        {
            Assert.That(Entries.Resolve(CursorKey.Attack)?.Image, Is.EqualTo("cursors/normal.png"));
        }

        [Test]
        public void A_key_that_the_theme_does_not_declare_resolves_to_its_fallback_first()
        {
            var fishing = new CursorKey("fishing", new CursorKey("rod", CursorKey.Pick));

            Assert.That(Entries.Resolve(fishing)?.Image, Is.EqualTo("cursors/pick.png"));
        }

        [Test]
        public void A_fallback_that_the_theme_does_not_declare_resolves_to_normal()
        {
            var fishing = new CursorKey("fishing", CursorKey.Attack);

            Assert.That(Entries.Resolve(fishing)?.Image, Is.EqualTo("cursors/normal.png"));
        }

        [Test]
        public void A_theme_without_normal_resolves_an_undeclared_key_to_nothing()
        {
            var entries = CursorEntries.Read("""{ "cursors": {} }""");

            Assert.That(entries.Resolve(CursorKey.Normal), Is.Null);
        }

        private static (int FrameIndex, Vector2I Hotspot, int MillisecondsToNextStep) PoseAt(CursorKey key,
            long elapsedMilliseconds, int frameCount = 3)
        {
            var pose = Entries.PoseAt(key, frameCount, elapsedMilliseconds)!.Value;

            return (pose.FrameIndex, pose.Hotspot, pose.MillisecondsToNextStep);
        }

        [Test]
        public void A_static_entry_shows_its_first_frame_at_any_time()
        {
            Assert.That(PoseAt(CursorKey.Pick, 0), Is.EqualTo((0, new Vector2I(5, 7), 0)));
            Assert.That(PoseAt(CursorKey.Pick, 123456), Is.EqualTo((0, new Vector2I(5, 7), 0)));
        }

        [Test]
        public void An_animated_entry_shows_the_frame_of_its_step()
        {
            Assert.That(PoseAt(CursorKey.Skill, 0).FrameIndex, Is.EqualTo(0));
            Assert.That(PoseAt(CursorKey.Skill, 99).FrameIndex, Is.EqualTo(0));
            Assert.That(PoseAt(CursorKey.Skill, 100).FrameIndex, Is.EqualTo(1));
            Assert.That(PoseAt(CursorKey.Skill, 200).FrameIndex, Is.EqualTo(2));
            Assert.That(PoseAt(CursorKey.Skill, 300).FrameIndex, Is.EqualTo(1));
        }

        [Test]
        public void An_animated_entry_gives_the_time_that_is_left_in_its_step()
        {
            Assert.That(PoseAt(CursorKey.Skill, 0).MillisecondsToNextStep, Is.EqualTo(100));
            Assert.That(PoseAt(CursorKey.Skill, 30).MillisecondsToNextStep, Is.EqualTo(70));
            Assert.That(PoseAt(CursorKey.Skill, 399).MillisecondsToNextStep, Is.EqualTo(1));
        }

        [Test]
        public void An_animated_entry_loops_at_the_end()
        {
            Assert.That(PoseAt(CursorKey.Skill, 400).FrameIndex, Is.EqualTo(0));
            Assert.That(PoseAt(CursorKey.Skill, 500).FrameIndex, Is.EqualTo(1));
            Assert.That(PoseAt(CursorKey.Skill, 4_000_300).FrameIndex, Is.EqualTo(1));
        }

        [Test]
        public void Each_step_has_its_own_duration_when_the_entry_lists_them()
        {
            Assert.That(PoseAt(CursorKey.Search, 99).FrameIndex, Is.EqualTo(0));
            Assert.That(PoseAt(CursorKey.Search, 100).FrameIndex, Is.EqualTo(1));
            Assert.That(PoseAt(CursorKey.Search, 149).FrameIndex, Is.EqualTo(1));
            Assert.That(PoseAt(CursorKey.Search, 150).FrameIndex, Is.EqualTo(2));
            Assert.That(PoseAt(CursorKey.Search, 349).FrameIndex, Is.EqualTo(2));
            Assert.That(PoseAt(CursorKey.Search, 350).FrameIndex, Is.EqualTo(0));
            Assert.That(PoseAt(CursorKey.Search, 120).MillisecondsToNextStep, Is.EqualTo(30));
        }

        [Test]
        public void Without_steps_the_frames_show_in_order_and_loop()
        {
            Assert.That(PoseAt(CursorKey.Wait, 0).FrameIndex, Is.EqualTo(0));
            Assert.That(PoseAt(CursorKey.Wait, 150).FrameIndex, Is.EqualTo(1));
            Assert.That(PoseAt(CursorKey.Wait, 250).FrameIndex, Is.EqualTo(2));
            Assert.That(PoseAt(CursorKey.Wait, 300).FrameIndex, Is.EqualTo(0));
            Assert.That(PoseAt(CursorKey.Wait, 0, frameCount: 11).FrameIndex, Is.EqualTo(0));
            Assert.That(PoseAt(CursorKey.Wait, 1050, frameCount: 11).FrameIndex, Is.EqualTo(10));
            Assert.That(PoseAt(CursorKey.Wait, 1100, frameCount: 11).FrameIndex, Is.EqualTo(0));
        }

        [Test]
        public void A_list_hotspot_follows_the_frame_that_shows()
        {
            Assert.That(PoseAt(CursorKey.Repair, 0).Hotspot, Is.EqualTo(new Vector2I(9, 20)));
            Assert.That(PoseAt(CursorKey.Repair, 100).Hotspot, Is.EqualTo(new Vector2I(3, 4)));
            Assert.That(PoseAt(CursorKey.Repair, 200).Hotspot, Is.EqualTo(new Vector2I(5, 6)));
        }

        [Test]
        public void A_list_hotspot_is_for_each_frame_and_not_for_each_step()
        {
            Assert.That(PoseAt(CursorKey.Board, 0).Hotspot, Is.EqualTo(new Vector2I(1, 1)));
            Assert.That(PoseAt(CursorKey.Board, 100).Hotspot, Is.EqualTo(new Vector2I(2, 2)));
            Assert.That(PoseAt(CursorKey.Board, 200).Hotspot, Is.EqualTo(new Vector2I(1, 1)));
        }

        [Test]
        public void A_single_hotspot_is_the_hotspot_of_each_frame()
        {
            Assert.That(PoseAt(CursorKey.Skill, 100).Hotspot, Is.EqualTo(new Vector2I(15, 15)));
        }

        [Test]
        public void A_key_that_the_theme_does_not_declare_animates_as_normal()
        {
            Assert.That(PoseAt(CursorKey.Attack, 500), Is.EqualTo((0, new Vector2I(0, 0), 0)));
        }

        [Test]
        public void A_theme_without_normal_has_no_pose_for_an_undeclared_key()
        {
            var entries = CursorEntries.Read("""{ "cursors": {} }""");

            Assert.That(entries.PoseAt(CursorKey.Attack, 1, 0), Is.Null);
        }

        [TestCase(0, 1)]
        [TestCase(600, 1)]
        [TestCase(1079, 1)]
        [TestCase(1080, 1)]
        [TestCase(1440, 1)]
        [TestCase(2159, 1)]
        [TestCase(2160, 2)]
        [TestCase(3240, 3)]
        public void The_scale_factor_is_the_whole_number_of_1080_pixels_in_the_window_height(int windowHeight,
            int expectedFactor)
        {
            Assert.That(CursorEntries.ScaleFactorFor(windowHeight), Is.EqualTo(expectedFactor));
        }

        [Test]
        public void A_set_without_a_scale_factor_does_not_scale_the_hotspots()
        {
            Assert.That(Entries.ScaleFactor, Is.EqualTo(1));
            Assert.That(PoseAt(CursorKey.Pick, 0).Hotspot, Is.EqualTo(new Vector2I(5, 7)));
        }

        [Test]
        public void The_hotspot_of_a_single_value_is_scaled_by_the_scale_factor()
        {
            var entries = ReadScaled(2);

            Assert.That(entries.PoseAt(CursorKey.Pick, 1, 0)!.Value.Hotspot, Is.EqualTo(new Vector2I(10, 14)));
        }

        [Test]
        public void The_hotspot_of_each_frame_is_scaled_by_the_scale_factor()
        {
            var entries = ReadScaled(3);

            Assert.That(entries.PoseAt(CursorKey.Repair, 3, 0)!.Value.Hotspot, Is.EqualTo(new Vector2I(27, 60)));
            Assert.That(entries.PoseAt(CursorKey.Repair, 3, 100)!.Value.Hotspot, Is.EqualTo(new Vector2I(9, 12)));
        }

        [Test]
        public void The_scale_factor_does_not_change_the_frame_size_or_the_hotspots_of_the_entry()
        {
            var entry = ReadScaled(2).Resolve(CursorKey.Pick);

            Assert.That(entry?.Frame, Is.EqualTo(new Vector2I(32, 24)));
            Assert.That(entry?.Hotspots, Is.EqualTo(new[] { new Vector2I(5, 7) }));
        }

        [Test]
        public void A_set_keeps_its_scale_factor()
        {
            Assert.That(ReadScaled(4).ScaleFactor, Is.EqualTo(4));
        }

        private static CursorEntries ReadScaled(int scaleFactor)
        {
            return CursorEntries.Read("""
                {
                  "cursors": {
                    "pick": { "image": "cursors/pick.png", "frame": [32, 24], "hotspot": [5, 7] },
                    "repair": { "image": "cursors/repair.png", "frame": [32, 32], "hotspot": [[9, 20], [3, 4], [5, 6]],
                                "ms": 100 }
                  }
                }
                """, scaleFactor);
        }

        [Test]
        public void Durations_that_do_not_match_the_steps_do_not_read()
        {
            Assert.Throws<InvalidOperationException>(() => CursorEntries.Read("""
                { "cursors": { "normal": { "image": "n.png", "frame": [1, 1], "hotspot": [0, 0],
                                           "steps": [0, 1], "ms": [100, 100, 100] } } }
                """));
        }

        [Test]
        public void The_game_owns_twenty_keys_with_no_fallback_now()
        {
            Assert.That(CursorKey.All.Select(key => key.Name), Is.EqualTo(new[]
            {
                "normal", "active", "click", "drag", "vertical", "wait", "forbid", "attack", "skill", "land",
                "board", "pick", "camera", "chat", "player", "hide", "search", "block", "repair", "feed"
            }));
            Assert.That(CursorKey.All.Select(key => key.Fallback), Is.All.Null);
        }
    }
}
