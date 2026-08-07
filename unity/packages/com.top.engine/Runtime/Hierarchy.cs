using System.Linq;
using UnityEngine;

namespace Top.Engine
{
    /// <summary>
    /// Transform tree lookups the composition code shares.
    /// </summary>
    public static class Hierarchy
    {
        public static Transform FindDeep(Transform root, string name)
        {
            return root
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform != root && transform.name == name);
        }
    }
}
