using UnityEngine;

namespace Top.Client.Models.Animations
{
    [RequireComponent(typeof(Renderer))]
    public class OpacityAnimation : MonoBehaviour
    {
        private static readonly int OpacityProperty = Shader.PropertyToID("_Opacity");

        public int materialIndex;
        public OpacityAnimationTrack track;

        private Renderer _renderer;
        private MaterialPropertyBlock _block;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        private void OnBecameVisible()
        {
            enabled = true;
        }

        private void OnBecameInvisible()
        {
            enabled = false;
        }

        private void Update()
        {
            if (track == null || track.keyFrames == null || track.keyFrames.Length == 0
                || track.values == null || track.values.Length != track.keyFrames.Length)
            {
                return;
            }

            var frame = LoopFrame(track, Time.time * track.framesPerSecond);

            _renderer.GetPropertyBlock(_block, materialIndex);
            _block.SetFloat(OpacityProperty, Sample(track, frame));
            _renderer.SetPropertyBlock(_block, materialIndex);
        }

        public static float LoopFrame(OpacityAnimationTrack track, float frame)
        {
            return frame % (track.keyFrames[^1] + 1);
        }

        public static float Sample(OpacityAnimationTrack track, float frame)
        {
            var keys = track.keyFrames;
            var values = track.values;

            if (frame <= keys[0])
            {
                return values[0];
            }

            for (var i = 1; i < keys.Length; i++)
            {
                if (frame <= keys[i])
                {
                    var t = (frame - keys[i - 1]) / (keys[i] - keys[i - 1]);

                    return Mathf.Lerp(values[i - 1], values[i], t);
                }
            }

            return values[values.Length - 1];
        }
    }
}
