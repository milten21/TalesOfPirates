using UnityEngine;

namespace Top.Client.Models.Animations
{
    public class UvAnimationTrack : ScriptableObject
    {
        public float framesPerSecond = 30f;
        public Matrix4x4[] frames;
    }
}
