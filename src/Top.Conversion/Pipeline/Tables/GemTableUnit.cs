using System;
using System.Collections.Generic;
using System.Linq;
using Top.Contracts.Tables;
using Top.Contracts.Tables.Items;
using Top.Legacy.Tables.Records;
using Top.Logging;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Maps stoneinfo rows to gems: the name, the gem item, the colour its
    /// name and socket show in, the item types it can be socketed into, and
    /// the bonus its hint function in
    /// scripts.lua gives per gem level. The "Gem Bonus " lead that most hint
    /// texts repeat is dropped. A gem whose hint function is missing or is
    /// not a level formula is reported and has no bonus.
    /// The top gem level is a server rule, not client data: the combine
    /// rates of the reference server's forge.lua end at level 9, and the
    /// forge parameter keeps a gem level in one digit.
    /// </summary>
    public class GemTableUnit(ClientTables tables, int maxLevel = GemTableUnit.ReferenceMaxLevel) : ITableUnit
    {
        public const int ReferenceMaxLevel = 9;

        private const string RepeatedLead = "Gem Bonus ";

        public string Name => "gems";

        public string Path => GemTable.Path;

        public IEnumerable<TableEntry> Entries()
        {
            return tables.Stones?.Select(Entry);
        }

        private GemEntry Entry(StoneInfoRecord stone)
        {
            return new GemEntry
            {
                Id = stone.Id,
                Name = stone.Name,
                ItemId = stone.ItemId,
                Color = stone.ItemRgb,
                Bonus = SelectBonus(stone),
                Fits = stone.EquipPositions?.Where(type => type > 0).ToArray() ?? [],
                MaxLevel = maxLevel,
            };
        }

        private GemBonus SelectBonus(StoneInfoRecord stone)
        {
            if (tables.GemHints == null || !tables.GemHints.Hints.TryGetValue(stone.HintFunction ?? string.Empty, out var hint))
            {
                Log.Warning($"gem {stone.Id} '{stone.Name}': hint '{stone.HintFunction}' is not a level formula, "
                            + "the gem has no bonus");

                return null;
            }

            var text = hint.Text.StartsWith(RepeatedLead, StringComparison.Ordinal)
                ? hint.Text.Substring(RepeatedLead.Length)
                : hint.Text;

            return new GemBonus { Text = text, PerLevel = hint.Factor };
        }
    }
}
