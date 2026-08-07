using UnityEngine;

namespace Top.Engine
{
    /// <summary>
    /// Draws the transform hierarchy under this object as a skeleton while
    /// the object is selected.
    /// </summary>
    public class SkeletonGizmos : MonoBehaviour
    {
        public float jointRadius = 0.03f;
        public Color boneColor = new Color(0.4f, 1f, 0.6f);
        public Color dummyColor = new Color(1f, 0.7f, 0.2f);

        private void OnDrawGizmosSelected()
        {
            foreach (var node in GetComponentsInChildren<Transform>())
            {
                if (node == transform)
                {
                    continue;
                }

                var isDummy = node.name.StartsWith("dummy_") || node.name.StartsWith("Dummy");

                Gizmos.color = isDummy ? dummyColor : boneColor;
                Gizmos.DrawLine(node.parent.position, node.position);

                if (isDummy)
                {
                    Gizmos.DrawWireCube(node.position, Vector3.one * (jointRadius * 2f));
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(node.position, node.name);
#endif
                }
                else
                {
                    Gizmos.DrawWireSphere(node.position, jointRadius);
                }
            }
        }
    }
}
