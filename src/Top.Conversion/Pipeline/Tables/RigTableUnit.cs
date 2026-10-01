using System.Collections.Generic;
using System.Linq;
using Top.Contracts.Assets.Models;
using Top.Contracts.Tables;
using Top.Contracts.Tables.Rigs;
using Top.Legacy.Tables.Custom;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Maps the skeleton models characterinfo rows animate on to rig entries.
    /// A main character animates on its rig and plays the clips of its
    /// <see cref="RigClipSet"/>, a file each. A monster has no rig of its own
    /// and no stances, and plays the clips baked into its model.
    /// </summary>
    public class RigTableUnit(ClientTables tables) : ITableUnit
    {
        public string Name => "rigs";

        public string Path => RigTable.Path;

        public static bool HasEntryFor(CharacterInfoRecord row)
        {
            return row.ModalType is CharacterModalType.MainCharacter or CharacterModalType.Other;
        }

        public IEnumerable<TableEntry> Entries()
        {
            if (tables.Actions == null)
            {
                return null;
            }

            return tables.Characters?
                .Where(HasEntryFor)
                .GroupBy(row => row.Model)
                .OrderBy(rows => rows.Key)
                .Select(CreateEntry);
        }

        private RigEntry CreateEntry(IGrouping<int, CharacterInfoRecord> rows)
        {
            var mainCharacters = rows.Where(row => row.ModalType == CharacterModalType.MainCharacter).ToList();

            return mainCharacters.Count > 0 ? CreateMainCharacterEntry(rows.Key, mainCharacters) : CreateMonsterEntry(rows.Key, rows.ToList());
        }

        private RigEntry CreateMainCharacterEntry(int model, IReadOnlyList<CharacterInfoRecord> rows)
        {
            var rigName = ToModelName(model);
            var clipSet = RigClipSet.Decode(rigName, tables.ActionsFor(rows, model) ?? []);

            return new RigEntry
            {
                Id = model,
                Path = OutputPaths.RigContentPath(rigName),
                Clips = clipSet.Clips
                    .Select(clip => new RigClip
                    {
                        Name = clip.Name, File = OutputPaths.ClipContentPath(rigName, clip.Name), Animation = clip.Name,
                    })
                    .ToArray(),
                Actions = clipSet.Actions
                    .Select(action => new RigAction
                    {
                        Id = action.Id, Stance = action.Stance, Clip = action.Clip, Events = action.Events,
                    })
                    .ToArray(),
            };
        }

        private RigEntry CreateMonsterEntry(int model, IReadOnlyList<CharacterInfoRecord> rows)
        {
            var modelName = ToModelName(model);
            var file = OutputPaths.ModelContentPath(ContentKind.Character, modelName);
            var poses = tables.ActionsFor(rows, model) ?? [];
            var animations = poses.Select(pose => Naming.ActionClip(modelName, pose.ActionNo)).ToList();

            return new RigEntry
            {
                Id = model,
                Clips = animations
                    .Select(animation => new RigClip { Name = animation, File = file, Animation = animation })
                    .ToArray(),
                Actions = poses
                    .Zip(animations, (pose, animation) => new RigAction
                    {
                        Id = pose.ActionNo, Clip = animation,
                        Events = ActionEvents.FromKeyFrames($"rig {modelName}", pose.ActionNo, pose),
                    })
                    .ToArray(),
            };
        }

        private static string ToModelName(int model) => $"{model:D4}";
    }
}
