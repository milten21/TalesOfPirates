using System.Numerics;
using NUnit.Framework;
using Top.Client.Features.Characters;
using Top.Client.Messages.Play.Data;

namespace Top.Client.Tests.Features.Characters
{
    public class NameplateRuleTests
    {
        private const uint PlayerWorldId = 1;

        private const uint NpcWorldId = 2952800458;

        private const uint LeaderWorldId = 4;

        private const uint OtherWorldId = 7;

        private static readonly CharacterState Player = CreateCharacter(PlayerWorldId, CharacterControlType.Player);

        [Test]
        public void An_npc_is_named_with_no_key_held()
        {
            var rule = new NameplateRule(Player, false, false);
            var npc = CreateCharacter(NpcWorldId, CharacterControlType.Npc);

            Assert.That(rule.ShowsName(npc), Is.True);
        }

        [Test]
        public void The_players_own_character_is_named_and_barred_with_no_key_held()
        {
            var rule = new NameplateRule(Player, false, false);

            Assert.That(rule.ShowsName(Player), Is.True);
            Assert.That(rule.ShowsHealth(Player), Is.True);
        }

        [TestCase(CharacterControlType.Player)]
        [TestCase(CharacterControlType.Monster)]
        public void Another_character_is_not_named_with_no_key_held(CharacterControlType controlType)
        {
            var rule = new NameplateRule(Player, false, false);
            var other = CreateCharacter(OtherWorldId, controlType);

            Assert.That(rule.ShowsName(other), Is.False);
            Assert.That(rule.ShowsHealth(other), Is.False);
        }

        [TestCase(CharacterControlType.Player)]
        [TestCase(CharacterControlType.Npc)]
        [TestCase(CharacterControlType.Monster)]
        public void Every_character_is_named_with_the_show_all_key_held(CharacterControlType controlType)
        {
            var rule = new NameplateRule(Player, true, false);
            var other = CreateCharacter(OtherWorldId, controlType);

            Assert.That(rule.ShowsName(other), Is.True);
            Assert.That(rule.ShowsName(Player), Is.True);
        }

        [TestCase(CharacterControlType.Player, true)]
        [TestCase(CharacterControlType.Npc, false)]
        [TestCase(CharacterControlType.Monster, true)]
        public void Every_character_but_an_npc_is_barred_with_the_show_all_key_held(
            CharacterControlType controlType, bool isBarred)
        {
            var rule = new NameplateRule(Player, true, false);
            var other = CreateCharacter(OtherWorldId, controlType);

            Assert.That(rule.ShowsHealth(other), Is.EqualTo(isBarred));
        }

        [Test]
        public void A_team_leader_is_named_and_barred_with_the_show_team_key_held()
        {
            var rule = new NameplateRule(Player, false, true);
            var leader = CreateCharacter(LeaderWorldId, CharacterControlType.Player, LeaderWorldId);

            Assert.That(rule.ShowsName(leader), Is.True);
            Assert.That(rule.ShowsHealth(leader), Is.True);
        }

        [Test]
        public void Nobody_but_a_team_leader_is_named_with_the_show_team_key_held()
        {
            var rule = new NameplateRule(Player, false, true);
            var member = CreateCharacter(5, CharacterControlType.Player, LeaderWorldId);
            var npc = CreateCharacter(NpcWorldId, CharacterControlType.Npc);

            Assert.That(rule.ShowsName(member), Is.False);
            Assert.That(rule.ShowsName(npc), Is.False);
            Assert.That(rule.ShowsName(Player), Is.False);
            Assert.That(rule.ShowsHealth(Player), Is.False);
        }

        [Test]
        public void A_team_mate_is_named_and_barred_while_the_player_is_in_a_team()
        {
            var player = CreateCharacter(PlayerWorldId, CharacterControlType.Player, LeaderWorldId);
            var rule = new NameplateRule(player, false, false);
            var teamMate = CreateCharacter(5, CharacterControlType.Player, LeaderWorldId);

            Assert.That(rule.ShowsName(teamMate), Is.True);
            Assert.That(rule.ShowsHealth(teamMate), Is.True);
        }

        [Test]
        public void Only_an_npc_outside_the_team_is_named_while_the_player_is_in_a_team()
        {
            var player = CreateCharacter(PlayerWorldId, CharacterControlType.Player, LeaderWorldId);
            var rule = new NameplateRule(player, false, false);
            var stranger = CreateCharacter(6, CharacterControlType.Player, 6);
            var npc = CreateCharacter(NpcWorldId, CharacterControlType.Npc);

            Assert.That(rule.ShowsName(stranger), Is.False);
            Assert.That(rule.ShowsHealth(stranger), Is.False);
            Assert.That(rule.ShowsName(npc), Is.True);
            Assert.That(rule.ShowsHealth(npc), Is.False);
        }

        private static CharacterState CreateCharacter(uint worldId, CharacterControlType controlType,
            uint teamLeaderId = 0)
        {
            return new CharacterState(worldId, controlType, 1, $"Character {worldId}", Vector2.Zero, 0, 0,
                teamLeaderId, 0, string.Empty, string.Empty, 0, 1, 100, 100, 20, 20, 0, 0, 0, 5, 0, 0f, null, null);
        }
    }
}
