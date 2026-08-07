using System.Collections.Generic;
using Top.Assets.Contract.Models;
using Top.Logging;
using UnityEngine;

namespace Top.Engine
{
    public enum WeaponHand
    {
        Right,
        Left,
    }

    /// <summary>
    /// Provides functionality for attaching weapon prefabs to specific hand dummies
    /// within a character rig.
    /// </summary>
    public static class WeaponAttachment
    {
        private const uint LeftHandDummy = 6;
        private const uint RightHandDummy = 9;
        private const uint GripDummy = 0;

        public static GameObject Attach(GameObject weaponPrefab, WeaponHand hand,
            IReadOnlyDictionary<string, Transform> bones)
        {
            var dummyName = Naming.Dummy(
                hand == WeaponHand.Left ? LeftHandDummy : RightHandDummy);

            if (!bones.TryGetValue(dummyName, out var handDummy))
            {
                Log.Warning($"rig has no '{dummyName}' dummy, weapon skipped");
                return null;
            }

            var weapon = Object.Instantiate(weaponPrefab, handDummy, false);
            var gripName = Naming.Dummy(GripDummy);
            var grip = Hierarchy.FindDeep(weapon.transform, gripName);

            if (grip == null)
            {
                Log.Warning($"weapon '{weaponPrefab.name}' has no '{gripName}' dummy, attached at the hand origin");
                return weapon;
            }

            var relative = weapon.transform.worldToLocalMatrix * grip.localToWorldMatrix;
            var rotation = Quaternion.Inverse(relative.rotation);

            weapon.transform.localRotation = rotation;
            weapon.transform.localPosition = rotation * -(Vector3)relative.GetColumn(3);

            return weapon;
        }
    }
}
