using UnityEngine;

namespace Top.Client.Models.Animations
{
    public class OpacityAnimationTrack : ScriptableObject
    {
        public float framesPerSecond = 30f;
        public int[] keyFrames;
        public float[] values;
    }
}
