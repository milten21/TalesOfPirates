using System.Runtime.Serialization;

namespace Top.Contracts.Tables.Rigs
{
    public enum Stance
    {
        [EnumMember(Value = "fists")] Fists = 0,
        [EnumMember(Value = "sword")] Sword = 1,
        [EnumMember(Value = "two_hand_sword")] TwoHandSword = 2,
        [EnumMember(Value = "dual_swords")] DualSwords = 3,
        [EnumMember(Value = "firegun")] Firegun = 4,
        [EnumMember(Value = "bow")] Bow = 5,
        [EnumMember(Value = "dagger")] Dagger = 6,
    }
}
