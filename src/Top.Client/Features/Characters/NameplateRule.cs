using Top.Client.Messages.Play.Data;

namespace Top.Client.Features.Characters;

// TODO: a published side id adds the side and faction branch between the team branch and the default.
// TODO: picking forces the plates of the hovered and the targeted character on.
public readonly record struct NameplateRule(
    CharacterState Player,
    bool IsShowAllNamesHeld,
    bool IsShowTeamNamesHeld)
{
    public bool ShowsName(CharacterState character)
    {
        if (IsShowAllNamesHeld)
        {
            return true;
        }

        if (IsShowTeamNamesHeld)
        {
            return IsTeamLeader(character);
        }

        return IsOwnCharacter(character) || IsTeamMate(character) || IsNpc(character);
    }

    public bool ShowsHealth(CharacterState character)
    {
        if (IsShowAllNamesHeld)
        {
            return !IsNpc(character);
        }

        if (IsShowTeamNamesHeld)
        {
            return IsTeamLeader(character);
        }

        return IsOwnCharacter(character) || IsTeamMate(character);
    }

    private static bool IsNpc(CharacterState character)
    {
        return character.ControlType == CharacterControlType.Npc;
    }

    // TODO: sailing compares the community id, because a boat's world id is not its owner's.
    private static bool IsTeamLeader(CharacterState character)
    {
        return character.TeamLeaderId != 0 && character.TeamLeaderId == character.WorldId;
    }

    private bool IsOwnCharacter(CharacterState character)
    {
        return character.WorldId == Player.WorldId;
    }

    private bool IsTeamMate(CharacterState character)
    {
        return Player.TeamLeaderId != 0 && character.TeamLeaderId == Player.TeamLeaderId;
    }
}
