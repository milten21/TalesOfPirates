using System.Collections.Generic;
using Top.Logging;
using UnityEngine;

namespace Top.Engine
{
    /// <summary>
    /// Editor-time scaffolding that composes a player character from a rig and item
    /// slots (hair, face, body, gloves, shoes) plus weapon attachments, reading
    /// committed content assets straight from the asset database.
    /// </summary>
    // TODO: Temporal.
    [ExecuteAlways]
    public class PlayerCharacter : MonoBehaviour
    {
        public int model;
        public int hairItem;
        public int faceItem;
        public int bodyItem;
        public int gloveItem;
        public int shoesItem;
        public int rightWeaponItem;
        public int leftWeaponItem;

        private GameObject _composed;
        private bool _dirty;

        private void OnEnable()
        {
            Compose();
            _dirty = false;
        }

        private void OnDisable()
        {
            Teardown();
        }

        private void OnValidate()
        {
            _dirty = true;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += ComposeDeferred;
            }
#endif
        }

        private void Update()
        {
            if (_dirty && Application.isPlaying)
            {
                _dirty = false;
                Compose();
            }
        }

        private void Teardown()
        {
            if (_composed == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_composed);
            }
            else
            {
                DestroyImmediate(_composed);
            }

            _composed = null;
        }

#if UNITY_EDITOR
        private void ComposeDeferred()
        {
            UnityEditor.EditorApplication.delayCall -= ComposeDeferred;

            if (this == null || Application.isPlaying || !isActiveAndEnabled || !_dirty)
            {
                return;
            }

            _dirty = false;
            Compose();
        }
#endif

        public void Compose()
        {
#if UNITY_EDITOR
            Teardown();

            var rigPrefab = EditorContentSource.LoadRig(model);

            if (rigPrefab == null)
            {
                Debug.LogError($"[Top] no rig for model {model}", this);
                return;
            }

            _composed = Instantiate(rigPrefab, transform);
            _composed.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            var bones = SkinnedPartBinder.MapBones(_composed.transform);

            ComposeSlot(hairItem, 0, bones);
            ComposeSlot(faceItem, 1, bones);
            ComposeSlot(bodyItem, 2, bones);
            ComposeSlot(gloveItem, 3, bones);
            ComposeSlot(shoesItem, 4, bones);
            ComposeWeapon(rightWeaponItem, WeaponHand.Right, bones);
            ComposeWeapon(leftWeaponItem, WeaponHand.Left, bones);

            foreach (var node in _composed.GetComponentsInChildren<Transform>(true))
            {
                node.gameObject.hideFlags = _composed.hideFlags;
            }
#else
            Debug.LogError("[Top] PlayerCharacter composes in the editor only", this);
#endif
        }

#if UNITY_EDITOR
        private void ComposeSlot(int itemId, int slot, Dictionary<string, Transform> bones)
        {
            if (itemId == 0)
            {
                return;
            }

            var definition = EditorContentSource.LoadItemDefinition(itemId);

            if (definition == null)
            {
                Log.Warning($"slot {slot}: item {itemId} is not converted yet, convert it via Top/Importer");
                return;
            }

            var partModel = model >= 0 && model < definition.models.Length ? definition.models[model] : null;

            if (partModel == null)
            {
                Log.Warning($"item {definition.id} '{definition.itemName}' has no model for framework {model}");
                return;
            }

            var source = partModel.GetComponentInChildren<SkinnedMeshRenderer>(true);

            if (source == null)
            {
                Log.Warning($"item {definition.id} model has no skinned mesh");
                return;
            }

            SkinnedPartBinder.Bind(source, _composed.transform, bones);
        }

        private void ComposeWeapon(int itemId, WeaponHand hand, Dictionary<string, Transform> bones)
        {
            if (itemId == 0)
            {
                return;
            }

            var definition = EditorContentSource.LoadItemDefinition(itemId);

            if (definition == null)
            {
                Log.Warning($"weapon item {itemId} is not converted yet, convert it via Top/Importer");
                return;
            }

            var weaponPrefab = model >= 0 && model < definition.models.Length ? definition.models[model] : null;

            if (weaponPrefab == null)
            {
                Log.Warning($"weapon {definition.id} '{definition.itemName}' has no model for framework {model}");
                return;
            }

            WeaponAttachment.Attach(weaponPrefab, hand, bones);
        }
#endif
    }
}
