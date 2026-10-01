using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Top.Contracts.Assets.Models;
using Top.Contracts.Tables.Rigs;
using Top.Conversion.Models;
using Top.Legacy.Tables.Custom;
using Top.Logging;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// A logical action a main character plays in a stance, or in any stance
    /// when the stance is null, the clip it plays and its events.
    /// </summary>
    public record StanceAction(int Id, Stance? Stance, string Clip, RigEvent[] Events);

    /// <summary>
    /// The clips the main characters on a rig play and the actions that play
    /// them. Their poses are a grid of stance columns: a pose is its action in
    /// the fists column and 54 more in each column after it. The fists pose plays for any
    /// stance, and another stance gets an action of its own only where its
    /// range or its key frames differ. Actions that play the same frame range
    /// share one clip. The clip is named after the lowest action that plays
    /// it, and after that action's stance too when its fists pose does not.
    /// A boat or a tower has no stance grid: each of its poses is the action
    /// of its number and plays in any stance.
    /// </summary>
    public class RigClipSet
    {
        private const int ActionsPerStance = 54;

        private RigClipSet(IReadOnlyList<ClipRange> clips, IReadOnlyList<StanceAction> actions)
        {
            Clips = clips;
            Actions = actions;
        }

        public IReadOnlyList<ClipRange> Clips { get; }

        public IReadOnlyList<StanceAction> Actions { get; }

        public static RigClipSet Decode(string rigName, CharacterAction[] poses)
        {
            var owner = $"rig {rigName}";

            return Build(owner, DecodeStanceGrid(owner, poses));
        }

        public static RigClipSet DecodeWithoutStances(int actionSetId, CharacterAction[] poses)
        {
            var cells = poses
                .OrderBy(pose => pose.ActionNo)
                .Select(pose => new Cell(pose.ActionNo, Stance.Fists, pose))
                .ToList();

            return Build($"action set {actionSetId}", cells);
        }

        private static RigClipSet Build(string owner, IReadOnlyList<Cell> cells)
        {
            var clips = cells
                .GroupBy(cell => (cell.Pose.StartFrame, cell.Pose.EndFrame))
                .Select(range => new ClipRange(NameClip(range.First()), range.Key.StartFrame, range.Key.EndFrame))
                .ToList();

            ReportNearDuplicates(owner, clips);

            var clipNames = clips.ToDictionary(clip => (clip.StartFrame, clip.EndFrame), clip => clip.Name);
            var fists = cells.Where(cell => cell.Stance == Stance.Fists).ToDictionary(cell => cell.Action);
            var actions = cells
                .Where(cell => cell.Stance == Stance.Fists
                               || !fists.TryGetValue(cell.Action, out var fistsCell)
                               || !IsSamePose(cell, fistsCell))
                .Select(cell => new StanceAction(cell.Action, cell.Stance == Stance.Fists ? null : cell.Stance,
                    clipNames[(cell.Pose.StartFrame, cell.Pose.EndFrame)],
                    ActionEvents.FromKeyFrames(owner, cell.Action, cell.Pose)))
                .ToList();

            return new RigClipSet(clips, actions);
        }

        private static List<Cell> DecodeStanceGrid(string owner, CharacterAction[] poses)
        {
            var cells = new List<Cell>();

            foreach (var pose in poses)
            {
                var column = (pose.ActionNo - 1) / ActionsPerStance;

                if (!Enum.IsDefined(typeof(Stance), column))
                {
                    Log.Warning($"{owner}: pose {pose.ActionNo} lies outside the stance grid, it is left out");

                    continue;
                }

                cells.Add(new Cell((pose.ActionNo - 1) % ActionsPerStance + 1, (Stance)column, pose));
            }

            return cells
                .OrderBy(cell => cell.Action)
                .ThenBy(cell => cell.Stance)
                .ToList();
        }

        private static void ReportNearDuplicates(string owner, IReadOnlyList<ClipRange> clips)
        {
            for (var first = 0; first < clips.Count; first++)
            {
                for (var second = first + 1; second < clips.Count; second++)
                {
                    if (AreFrameApart(clips[first], clips[second]))
                    {
                        Log.Warning($"{owner}: clips {Describe(clips[first])} and {Describe(clips[second])} " +
                                    "are a frame apart, they stay two clips");
                    }
                }
            }
        }

        private static bool AreFrameApart(ClipRange first, ClipRange second)
        {
            return Math.Abs(first.StartFrame - second.StartFrame) <= 1 && Math.Abs(first.EndFrame - second.EndFrame) <= 1;
        }

        private static string Describe(ClipRange clip) => $"{clip.Name} ({clip.StartFrame}-{clip.EndFrame})";

        private static string NameClip(Cell cell)
        {
            var name = Naming.ActionClip(cell.Action);

            return cell.Stance == Stance.Fists ? name : $"{name}_{ToSlug(cell.Stance)}";
        }

        private static string ToSlug(Stance stance)
        {
            return typeof(Stance).GetField(stance.ToString())!.GetCustomAttribute<EnumMemberAttribute>()!.Value;
        }

        private static bool IsSamePose(Cell first, Cell second)
        {
            return first.Pose.StartFrame == second.Pose.StartFrame
                   && first.Pose.EndFrame == second.Pose.EndFrame
                   && first.Pose.KeyFrames.SequenceEqual(second.Pose.KeyFrames);
        }

        private record Cell(int Action, Stance Stance, CharacterAction Pose);
    }
}
