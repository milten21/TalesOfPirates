using System;
using System.Linq;
using Top.Contracts.Assets.Models;
using Top.Contracts.Tables.Rigs;
using Top.Legacy.Tables.Custom;
using Top.Logging;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// The key frames of a pose as events of its logical action, named after
    /// that action. An action the names do not cover gets <see cref="RigEvent.Mark"/>.
    /// </summary>
    public static class ActionEvents
    {
        private const int SecondsDecimals = 3;

        public static RigEvent[] FromKeyFrames(string owner, int action, CharacterAction pose)
        {
            var events = pose.KeyFrames
                .Where(frame => IsInRange(owner, pose, frame))
                .Select(frame => new RigEvent { Name = NameFor(action), Time = ToSeconds(frame - pose.StartFrame) })
                .ToArray();

            return events.Length > 0 ? events : null;
        }

        private static string NameFor(int action)
        {
            return action switch
            {
                5 or 6 => RigEvent.Footstep,
                >= 7 and <= 15 or 40 or 41 => RigEvent.Hit,
                28 => RigEvent.Mine,
                29 => RigEvent.Gather,
                17 => RigEvent.Death,
                1 or 4 => RigEvent.Breath,
                38 => RigEvent.Beat,
                _ => RigEvent.Mark,
            };
        }

        private static bool IsInRange(string owner, CharacterAction pose, int frame)
        {
            if (frame >= pose.StartFrame && frame <= pose.EndFrame)
            {
                return true;
            }

            Log.Warning($"{owner}: pose {pose.ActionNo} has key frame {frame} outside its range " +
                        $"{pose.StartFrame}-{pose.EndFrame}, it is left out");

            return false;
        }

        private static float ToSeconds(int frames)
        {
            return MathF.Round(frames / AnimationRate.FramesPerSecond, SecondsDecimals);
        }
    }
}
