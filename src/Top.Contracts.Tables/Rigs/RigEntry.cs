using System;
using Newtonsoft.Json;

namespace Top.Contracts.Tables.Rigs
{
    public class RigEntry : TableEntry
    {
        [JsonProperty("path")] public string Path;
        [JsonProperty("clips")] public RigClip[] Clips = new RigClip[0];
        [JsonProperty("actions")] public RigAction[] Actions = new RigAction[0];

        public RigClip FindClip(int action, Stance stance)
        {
            return FindAction(action, stance) is { } row ? FindClip(row) : null;
        }

        public RigClip FindClip(RigAction action)
        {
            return Clips != null ? Array.Find(Clips, clip => clip.Name == action.Clip) : null;
        }

        public RigAction FindAction(int action, Stance stance)
        {
            if (Actions == null)
            {
                return null;
            }

            return Array.Find(Actions, row => row.Id == action && row.Stance == stance)
                   ?? Array.Find(Actions, row => row.Id == action && row.Stance == null);
        }
    }
}
