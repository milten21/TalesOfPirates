using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Top.Engine.Tests
{
    public class WeaponAttachmentTests
    {
        [Test]
        public void Grip_dummy_lands_on_the_hand_dummy()
        {
            var hand = new GameObject("dummy_9").transform;
            hand.position = new Vector3(1f, 2f, 3f);
            hand.rotation = Quaternion.Euler(0f, 90f, 0f);

            var bones = new Dictionary<string, Transform> { ["dummy_9"] = hand };

            var weaponPrefab = new GameObject("weapon");
            var grip = new GameObject("dummy_0").transform;
            grip.SetParent(weaponPrefab.transform);
            grip.localPosition = new Vector3(0f, 0.5f, 0f);
            grip.localRotation = Quaternion.Euler(0f, 0f, 45f);

            try
            {
                var weapon = WeaponAttachment.Attach(weaponPrefab, WeaponHand.Right, bones);
                var attachedGrip = weapon.transform.Find("dummy_0");

                Assert.That(weapon.transform.parent, Is.EqualTo(hand));
                Assert.That(Vector3.Distance(attachedGrip.position, hand.position),
                    Is.LessThan(1e-4f));
                Assert.That(Quaternion.Angle(attachedGrip.rotation, hand.rotation),
                    Is.LessThan(1e-2f));
            }
            finally
            {
                Object.DestroyImmediate(hand.gameObject);
                Object.DestroyImmediate(weaponPrefab);
            }
        }
    }
}
