using Top.Client.Messages.Play.Data;

namespace Top.Client.Messages.Party.Data
{
    public record PartyMemberVitals(
        int Level,
        int Health,
        int MaxHealth,
        int Mana,
        int MaxMana,
        int TypeId,
        CharacterLook? Look);
}
