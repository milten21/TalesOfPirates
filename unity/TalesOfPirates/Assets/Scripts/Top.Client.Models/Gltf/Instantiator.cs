using GLTFast;
using GLTFast.Logging;
using Top.Client.Models.Materials;
using Top.Contracts.Assets.Models;
using UnityEngine;

namespace Top.Client.Models.Gltf
{
    public class Instantiator : GameObjectInstantiator
    {
        private readonly MaterialAnimationSet _materialAnimations;

        public Instantiator(IGltfReadable gltf, Transform parent, MaterialAnimationSet materialAnimations,
            ICodeLogger logger = null, InstantiationSettings settings = null)
            : base(gltf, parent, logger, settings)
        {
            _materialAnimations = materialAnimations;
        }

        public Transform Scene => SceneTransform;

        public GameObject FindNodeObject(uint nodeIndex)
        {
            return m_Nodes != null && m_Nodes.TryGetValue(nodeIndex, out var node) ? node : null;
        }

        public override void AddPrimitive(uint nodeIndex, string meshName, MeshResult meshResult,
            uint[] joints = null, uint? rootJoint = null, float[] morphTargetWeights = null,
            int meshNumeration = 0)
        {
            var node = m_Nodes[nodeIndex];

            if (Naming.IsHelper(node.name))
            {
                node.AddComponent<MeshFilter>().mesh = meshResult.mesh;

                return;
            }

            base.AddPrimitive(nodeIndex, meshName, meshResult, joints, rootJoint,
                morphTargetWeights, meshNumeration);

            if (!node.TryGetComponent<Renderer>(out var renderer))
            {
                return;
            }

            if (Naming.IsLitShell(node.name))
            {
                renderer.enabled = false;
            }

            _materialAnimations.Attach(renderer, meshResult.materialIndices);
        }
    }
}
