using NUnit.Framework;
using Top.Contracts.Tables.Rigs;

namespace Top.Contracts.Tables.Tests
{
    public class RigEntryTests
    {
        private static readonly RigEntry MainCharacterRig = new RigEntry
        {
            Id = 0,
            Path = "rigs/0000.glb",
            Clips = new[]
            {
                new RigClip { Name = "0000_04_waiting2", File = "rigs/0000.glb", Animation = "0000_04_waiting2" },
                new RigClip { Name = "0000_112", File = "rigs/0000.glb", Animation = "0000_112" },
            },
            Actions = new[]
            {
                new RigAction { Id = 4, Clip = "0000_04_waiting2" },
                new RigAction { Id = 4, Stance = Stance.TwoHandSword, Clip = "0000_112" },
            },
        };

        [Test]
        public void An_action_plays_the_clip_of_its_stance()
        {
            Assert.That(MainCharacterRig.FindClip(4, Stance.TwoHandSword).Animation, Is.EqualTo("0000_112"));
        }

        [Test]
        public void A_stance_with_no_row_of_its_own_plays_the_stance_less_clip()
        {
            Assert.That(MainCharacterRig.FindClip(4, Stance.Dagger).Animation, Is.EqualTo("0000_04_waiting2"));
        }

        [Test]
        public void An_action_with_neither_a_row_for_the_stance_nor_a_stance_less_row_plays_nothing()
        {
            var dualSwordsOnly = new RigEntry
            {
                Id = 0,
                Clips = new[] { new RigClip { Name = "0000_202", File = "rigs/0000.glb", Animation = "0000_202" } },
                Actions = new[] { new RigAction { Id = 40, Stance = Stance.DualSwords, Clip = "0000_202" } },
            };

            Assert.That(dualSwordsOnly.FindClip(40, Stance.Fists), Is.Null);
        }

        [Test]
        public void An_action_the_entry_does_not_declare_plays_nothing()
        {
            Assert.That(MainCharacterRig.FindClip(24, Stance.Fists), Is.Null);
        }

        [Test]
        public void An_entry_without_clips_or_actions_plays_nothing()
        {
            var bare = new RigEntry { Id = 5, Clips = null, Actions = null };

            Assert.That(bare.FindClip(1, Stance.Fists), Is.Null);
        }
    }
}
