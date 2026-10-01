using System.Collections.Generic;
using Top.Contracts;

namespace Top.Client.Messages.Inventory.Data
{
    public record BagItem(
        int ItemId,
        int NeedLevel,
        int Count,
        int Durability,
        int MaxDurability,
        int Energy,
        int MaxEnergy,
        int ForgeLevel,
        bool IsValid,
        bool IsTradable,
        bool IsLocked,
        int Expiration,
        int ForgeParameter,
        int InstanceParameter,
        IReadOnlyDictionary<CharacterStat, int> InstanceStats);
}
