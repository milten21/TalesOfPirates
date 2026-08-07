using UnityEngine;

namespace Top.Engine
{
    /// <summary>
    /// The item asset gameplay reads: iteminfo row id and name plus the model
    /// prefab each of the four player frameworks wears or holds it as.
    /// </summary>
    public class ItemDefinition : ScriptableObject
    {
        public int id;
        public string itemName;
        public GameObject[] models = new GameObject[4];

        public static string AssetPath(int id) => $"Assets/Content/Item/Definitions/{id:D4}.asset";
    }
}
