using System.Collections.Generic;
using System.Linq;
using Top.Contracts.Tables;
using Top.Contracts.Tables.Rigs;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Maps the action sets boats and towers play to action set entries. A
    /// boat or a tower has no skeleton and no stance grid: a pose is the
    /// action of its number, and poses that play the same frame range share
    /// one clip. A clip names no file, as it is in the part that plays the
    /// actions.
    /// </summary>
    public class ActionSetTableUnit(ClientTables tables) : ITableUnit
    {
        public string Name => "actionsets";

        public string Path => ActionSetTable.Path;

        public static bool HasEntryFor(CharacterInfoRecord row)
        {
            return row.ModalType is CharacterModalType.Boat or CharacterModalType.Employee;
        }

        public IEnumerable<TableEntry> Entries()
        {
            if (tables.Actions == null)
            {
                return null;
            }

            return tables.Characters?
                .Where(HasEntryFor)
                .Select(row => row.ActionId)
                .Distinct()
                .OrderBy(actionSetId => actionSetId)
                .Select(CreateEntry);
        }

        private RigEntry CreateEntry(int actionSetId)
        {
            var clipSet = RigClipSet.DecodeWithoutStances(actionSetId,
                tables.Actions.TryGetActions(actionSetId, out var poses) ? poses : []);

            return new RigEntry
            {
                Id = actionSetId,
                Clips = clipSet.Clips
                    .Select(clip => new RigClip { Name = clip.Name, Animation = clip.Name })
                    .ToArray(),
                Actions = clipSet.Actions
                    .Select(action => new RigAction { Id = action.Id, Clip = action.Clip, Events = action.Events })
                    .ToArray(),
            };
        }
    }
}
