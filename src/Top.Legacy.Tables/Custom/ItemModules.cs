using System;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Custom
{
    /// <summary>
    /// Resolves an item's per-framework model module: iteminfo column
    /// model + 1 of the five module columns names the .lgo the client
    /// uses for player framework 0-3 - a skinned wearable part for
    /// equipment, an item mesh for held weapons. "0" or blank means the
    /// item has no model for that framework. The body column lists the
    /// frameworks that may equip the item by the same model + 1 index, and
    /// -1 as its first value means all of them.
    /// </summary>
    public static class ItemModules
    {
        private const int AnyFramework = -1;

        public static bool TryGetModule(ItemInfoRecord item, int model, out string module)
        {
            module = null;

            if (item?.Modules == null || model < 0 || model + 1 >= item.Modules.Length)
            {
                return false;
            }

            var value = item.Modules[model + 1];

            if (string.IsNullOrEmpty(value) || value == "0")
            {
                return false;
            }

            module = value;
            return true;
        }

        public static bool CanEquip(ItemInfoRecord item, int model)
        {
            if (item?.Body == null || item.Body.Length == 0 || model < 0)
            {
                return false;
            }

            return item.Body[0] == AnyFramework || Array.IndexOf(item.Body, model + 1) >= 0;
        }

        public static bool IsWearable(ItemType type)
        {
            return type is ItemType.Hair or ItemType.Face or ItemType.Armor or ItemType.Gloves
                or ItemType.Shoe or ItemType.Tattoo or ItemType.Hairdo;
        }
    }
}
