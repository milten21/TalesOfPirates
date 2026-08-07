#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Top.Engine
{
    /// <summary>
    /// Editor-time scaffolding that resolves converted content by hard-coded asset
    /// database paths: rig prefabs by model index, item definitions by id.
    /// </summary>
    // TODO: Temporal.
    public static class EditorContentSource
    {
        public static GameObject LoadRig(int model)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Content/Character/Rigs/{model:D4}/{model:D4}.glb");
        }

        public static ItemDefinition LoadItemDefinition(int id)
        {
            return AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDefinition.AssetPath(id));
        }
    }
}
#endif
