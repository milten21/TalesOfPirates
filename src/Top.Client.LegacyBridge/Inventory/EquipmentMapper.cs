using System;
using System.Collections.Generic;
using Top.Client.Messages.Inventory.Data;
using Top.Legacy.Protocol.Messages.Characters;

namespace Top.Client.LegacyBridge.Inventory
{
    public static class EquipmentMapper
    {
        public static IReadOnlyList<BagItem?> ToEquipment(LookInfo look)
        {
            if (look == null)
            {
                throw new ArgumentNullException(nameof(look));
            }

            var equipment = new BagItem?[LookInfo.LinkCount];

            for (var link = 0; link < look.Links.Count; link++)
            {
                var grid = look.Links[link];

                equipment[link] = IsMarker(grid) ? null : BagSyncMapper.ToBagItem(grid);
            }

            return equipment;
        }

        private static bool IsMarker(ItemGrid grid)
        {
            return grid.Id is LookInfo.BothHandsMarker or LookInfo.TotemMarker;
        }
    }
}
