using UnityEngine;

namespace Top.Client.App
{
    [CreateAssetMenu(menuName = "Top/Shader Settings", fileName = "ShaderSettings")]
    public class ShaderSettings : ScriptableObject
    {
        [SerializeField] private Shader _model;
        [SerializeField] private Shader _terrain;
        [SerializeField] private Shader _water;

        public Shader Model => _model;

        public Shader Terrain => _terrain;

        public Shader Water => _water;
    }
}
