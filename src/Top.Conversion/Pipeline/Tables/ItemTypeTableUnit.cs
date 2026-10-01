using System.Collections.Generic;
using System.Linq;
using Top.Contracts.Tables;
using Top.Contracts.Tables.Items;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Emits the names of the item types an item tooltip shows. itemtype.txt
    /// names too few of them, some in Chinese, and only the skill tooltip of
    /// the original reads it, so the names are kept here. A type with no
    /// clear use across its items is not named. Boat part types are left out.
    /// </summary>
    public class ItemTypeTableUnit : ITableUnit
    {
        private static readonly Dictionary<int, string> Names = new Dictionary<int, string>
        {
            [1] = "Sword",
            [2] = "Greatsword",
            [3] = "Bow",
            [4] = "Firegun",
            [5] = "Blade",
            [6] = "Boxing gloves",
            [7] = "Dagger",
            [8] = "Gold pouch",
            [9] = "Staff",
            [10] = "Hammer",
            [11] = "Shield",
            [12] = "Arrow",
            [13] = "Ammunition",
            [14] = "Headgear",
            [17] = "Repair tool",
            [18] = "Axe",
            [19] = "Pickaxe",
            [20] = "Headgear",
            [21] = "Face",
            [22] = "Body armor",
            [23] = "Gloves",
            [24] = "Shoes",
            [25] = "Necklace",
            [26] = "Ring",
            [27] = "Tattoo",
            [28] = "Hairdo",
            [29] = "Coral",
            [31] = "Consumable",
            [32] = "Consumable",
            [33] = "Consumable",
            [34] = "Fairy skill book",
            [35] = "Life skill book",
            [36] = "Ticket",
            [41] = "Material",
            [42] = "Quest item",
            [44] = "Wings",
            [45] = "Trade permit",
            [46] = "Medal",
            [47] = "Scroll",
            [49] = "Gem",
            [50] = "Refining gem",
            [57] = "Fairy ration",
            [58] = "Fairy fruit",
            [59] = "Fairy",
            [60] = "Scroll",
            [69] = "Blueprint",
            [81] = "Bracelet",
            [82] = "Belt",
            [83] = "Handguard",
            [88] = "Cloak",
            [90] = "Mount",
            [99] = "Lottery ticket",
        };

        public string Name => "itemtypes";

        public string Path => ItemTypeTable.Path;

        public IEnumerable<TableEntry> Entries()
        {
            return Names.Select(pair => new ItemTypeEntry { Id = pair.Key, Name = pair.Value });
        }
    }
}
