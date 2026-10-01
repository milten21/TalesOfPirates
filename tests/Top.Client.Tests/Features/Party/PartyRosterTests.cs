using System.Linq;
using NUnit.Framework;
using Top.Client.Features.Party;
using Top.Client.Messages.Party.Data;
using Top.Client.Messages.Party.Events;

namespace Top.Client.Tests.Features.Party;

public class PartyRosterTests
{
    private const uint PlayerId = 12;

    private static readonly PartyMember Player = new PartyMember(PlayerId, "T1");

    private static readonly PartyMember Anne = new PartyMember(4, "Anne");

    private static readonly PartyMember Mary = new PartyMember(7, "Mary");

    private static readonly PartyMember Jack = new PartyMember(9, "Jack");

    private static readonly PartyMemberVitals AnneVitals = new PartyMemberVitals(3, 80, 109, 20, 38, 1, null);

    private static readonly PartyMemberVitals AnneHurtVitals = new PartyMemberVitals(3, 40, 109, 20, 38, 1, null);

    private PartyRoster _party;

    [SetUp]
    public void CreateParty()
    {
        _party = new PartyRoster(PlayerId);
    }

    [Test]
    public void A_new_roster_has_no_party()
    {
        Assert.That(_party.Members, Is.Empty);
        Assert.That(_party.IsPlayerLeader, Is.False);
    }

    [Test]
    public void The_members_are_the_list_without_the_player_in_list_order_and_the_first_entry_leads()
    {
        Join(Anne, Player, Mary);

        Assert.That(_party.Members.Select(member => member.Name), Is.EqualTo(new[] { "Anne", "Mary" }));
        Assert.That(_party.Members.Select(member => member.IsLeader), Is.EqualTo(new[] { true, false }));
        Assert.That(_party.IsPlayerLeader, Is.False);
    }

    [Test]
    public void The_player_leads_when_the_player_is_the_first_entry()
    {
        Join(Player, Anne);

        Assert.That(_party.IsPlayerLeader, Is.True);
        Assert.That(_party.Members.Single().IsLeader, Is.False);
    }

    [Test]
    public void The_next_member_leads_after_the_leader_leaves()
    {
        Join(Anne, Player, Mary);

        Change(PartyChangeKind.Left, "Anne", Player, Mary);

        Assert.That(_party.IsPlayerLeader, Is.True);
        Assert.That(_party.Members.Select(member => member.Name), Is.EqualTo(new[] { "Mary" }));
        Assert.That(_party.Members.Single().IsLeader, Is.False);
    }

    [Test]
    public void A_list_without_the_player_is_no_party()
    {
        Join(Anne, Mary);

        Assert.That(_party.Members, Is.Empty);
        Assert.That(_party.IsPlayerLeader, Is.False);
    }

    [Test]
    public void A_new_member_has_a_name_and_no_vitals()
    {
        Join(Anne, Player);

        Assert.That(_party.Members.Single(), Is.EqualTo(new PartyRosterMember(4, "Anne", true, null)));
    }

    [Test]
    public void Vitals_for_a_member_are_kept_on_the_member()
    {
        Join(Anne, Player);

        _party.ChangeVitals(new PartyMemberVitalsChangedEvent(Anne.CharacterId, AnneVitals));

        Assert.That(_party.Members.Single().Vitals, Is.EqualTo(AnneVitals));
    }

    [Test]
    public void New_vitals_replace_the_old_vitals()
    {
        Join(Anne, Player);
        _party.ChangeVitals(new PartyMemberVitalsChangedEvent(Anne.CharacterId, AnneVitals));

        _party.ChangeVitals(new PartyMemberVitalsChangedEvent(Anne.CharacterId, AnneHurtVitals));

        Assert.That(_party.Members.Single().Vitals, Is.EqualTo(AnneHurtVitals));
    }

    [Test]
    public void Vitals_for_an_unknown_id_are_ignored()
    {
        Join(Anne, Player);

        _party.ChangeVitals(new PartyMemberVitalsChangedEvent(Mary.CharacterId, AnneVitals));

        Assert.That(_party.Members.Single().Vitals, Is.Null);
    }

    [Test]
    public void Vitals_with_no_party_are_ignored()
    {
        _party.ChangeVitals(new PartyMemberVitalsChangedEvent(Anne.CharacterId, AnneVitals));

        Assert.That(_party.Members, Is.Empty);
    }

    [Test]
    public void A_member_that_stays_keeps_its_vitals_when_another_joins()
    {
        Join(Anne, Player);
        _party.ChangeVitals(new PartyMemberVitalsChangedEvent(Anne.CharacterId, AnneVitals));

        Join(Anne, Player, Mary);

        Assert.That(_party.Members.Select(member => member.Vitals), Is.EqualTo(new[] { AnneVitals, null }));
    }

    [Test]
    public void A_member_that_leaves_goes_away_and_the_others_keep_their_vitals()
    {
        Join(Anne, Player, Mary, Jack);
        _party.ChangeVitals(new PartyMemberVitalsChangedEvent(Anne.CharacterId, AnneVitals));

        Change(PartyChangeKind.Left, "Mary", Anne, Player, Jack);

        Assert.That(_party.Members.Select(member => member.Name), Is.EqualTo(new[] { "Anne", "Jack" }));
        Assert.That(_party.Members[0].Vitals, Is.EqualTo(AnneVitals));
    }

    [Test]
    public void A_kicked_member_goes_away()
    {
        Join(Player, Anne, Mary);

        Change(PartyChangeKind.Kicked, "Anne", Player, Mary);

        Assert.That(_party.Members.Select(member => member.Name), Is.EqualTo(new[] { "Mary" }));
    }

    [Test]
    public void The_end_of_the_party_removes_every_member()
    {
        Join(Anne, Player);

        Change(PartyChangeKind.Left, "Anne");

        Assert.That(_party.Members, Is.Empty);
        Assert.That(_party.IsPlayerLeader, Is.False);
    }

    [Test]
    public void The_player_leaving_ends_the_party_for_the_player()
    {
        Join(Anne, Player, Mary);

        Change(PartyChangeKind.Left, "T1", Anne, Mary);

        Assert.That(_party.Members, Is.Empty);
    }

    [Test]
    public void The_player_kicked_ends_the_party_for_the_player()
    {
        Join(Anne, Player, Mary);

        Change(PartyChangeKind.Kicked, "T1", Anne, Mary);

        Assert.That(_party.Members, Is.Empty);
    }

    [Test]
    public void The_party_stays_while_only_vitals_arrive_as_after_a_map_change()
    {
        Join(Anne, Player);

        _party.ChangeVitals(new PartyMemberVitalsChangedEvent(Anne.CharacterId, AnneVitals));
        _party.ChangeVitals(new PartyMemberVitalsChangedEvent(Anne.CharacterId, AnneHurtVitals));

        Assert.That(_party.Members.Single(), Is.EqualTo(new PartyRosterMember(4, "Anne", true, AnneHurtVitals)));
    }

    private void Join(params PartyMember[] members)
    {
        Change(PartyChangeKind.Joined, members[^1].Name, members);
    }

    private void Change(PartyChangeKind kind, string changedMemberName, params PartyMember[] members)
    {
        _party.ChangeMembers(new PartyChangedEvent(kind, changedMemberName, members));
    }
}
