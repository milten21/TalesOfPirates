using UnityEngine;

namespace Top.Client.Models.Animations
{
    public class FlipbookTrack : ScriptableObject
    {
        public float framesPerSecond = 30f;
        public Texture2D[] frames;
    }
}
