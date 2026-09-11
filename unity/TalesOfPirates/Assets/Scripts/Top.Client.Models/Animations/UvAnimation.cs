using UnityEngine;

namespace Top.Client.Models.Animations
{
    [RequireComponent(typeof(Renderer))]
    public class UvAnimation : MonoBehaviour
    {
        private static readonly int UvMatProperty = Shader.PropertyToID("_UvMat");
        private static readonly int UvAnimatedProperty = Shader.PropertyToID("_UvAnimated");

        public int materialIndex;
        public UvAnimationTrack track;

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
            if (track == null || track.frames == null || track.frames.Length == 0)
            {
                return;
            }

            var frames = track.frames;
            var position = Time.time * track.framesPerSecond;
            var index = (int)position % frames.Length;
            var frame = frames[index];
            var next = index + 1;

            if (next < frames.Length)
            {
                frame = LerpMatrix(frame, frames[next], position - Mathf.Floor(position));
            }

            _renderer.GetPropertyBlock(_block, materialIndex);
            _block.SetMatrix(UvMatProperty, frame);
            _block.SetFloat(UvAnimatedProperty, 1f);
            _renderer.SetPropertyBlock(_block, materialIndex);
        }

        private static Matrix4x4 LerpMatrix(Matrix4x4 a, Matrix4x4 b, float t)
        {
            var result = Matrix4x4.identity;

            for (var i = 0; i < 16; i++)
            {
                result[i] = Mathf.Lerp(a[i], b[i], t);
            }

            return result;
        }
    }
}
