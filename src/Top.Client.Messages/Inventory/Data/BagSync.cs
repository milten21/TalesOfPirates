using System.Collections.Generic;

namespace Top.Client.Messages.Inventory.Data
{
    public record BagSync(int? Capacity, IReadOnlyList<BagSlot> Slots);
}
