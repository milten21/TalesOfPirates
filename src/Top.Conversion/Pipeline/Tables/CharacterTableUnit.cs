using System.Collections.Generic;
using System.Linq;
using Top.Contracts.Tables;
using Top.Contracts.Tables.Characters;
using Original = Top.Legacy.Tables.Records;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Maps characterinfo rows to character entries, naming the model each one
    /// converts to and the rig entry it animates through. A player animates on
    /// the rig of its body type and wears its default parts; a monster is one merged
    /// model named after its skeleton, and so is its rig entry. A boat or a
    /// tower is built of a part per slot and plays its action set.
    /// </summary>
    public class CharacterTableUnit(ClientTables tables) : ITableUnit
    {
        public string Name => "characters";

        public string Path => CharacterTable.Path;

        public IEnumerable<TableEntry> Entries()
        {
            return tables.Characters?.Select(Entry);
        }

        private CharacterEntry Entry(Original.CharacterInfoRecord row)
        {
            var machine = ActionSetTableUnit.HasEntryFor(row);
            var modalType = (CharacterModalType)row.ModalType;
            var composed = modalType == CharacterModalType.MainCharacter;
            var merged = modalType == CharacterModalType.Other;

            return new CharacterEntry
            {
                Id = row.Id,
                Name = row.Name,
                ModalType = modalType,
                ModelPath = merged ? OutputPaths.ModelContentPath(ContentKind.Character, $"{row.Model:D4}") : null,
                RigId = RigTableUnit.HasEntryFor(row) ? row.Model : null,
                BodyType = composed ? row.Model : null,
                DefaultItems = composed ? ClientTables.SelectDefaultItems(row) : [],
                PartPaths = machine ? SelectPartPaths(row) : [],
                ActionSetId = machine ? row.ActionId : null,
                Height = row.Height,
                Radius = row.Radius,
                Territory = row.Territory,
                SeaHeight = row.SeaHeight,
                EffectAction = CreateEffectAction(row.EffectActionIds),
            };
        }

        private string[] SelectPartPaths(Original.CharacterInfoRecord row)
        {
            return tables.SelectMachineModules(row)
                .Select(module => module == null ? null : OutputPaths.ModelContentPath(ContentKind.Machine, module))
                .ToArray();
        }

        private static EffectAction CreateEffectAction(int[] ids)
        {
            if (ids is not [var keyIndex, > 0 and var effect, ..])
            {
                return null;
            }

            return new EffectAction { KeyIndex = keyIndex, Effect = effect, Dummy = ids.Length > 2 ? ids[2] : 0 };
        }
    }
}
