using EternalClash.Core.Save;
using Spine.Unity;
using UnityEngine;

namespace EternalClash.Village
{
    /// <summary>
    /// Applies the saved starter equipment to the player's existing Spine slots.
    /// The equipment data and upgrade calculations remain owned by EquipmentSystem.
    /// </summary>
    public sealed class EquipmentVisualBinder : MonoBehaviour
    {
        private SkeletonAnimation skeletonAnimation;

        private void Awake()
        {
            skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);
        }

        private void Start()
        {
            ApplySavedEquipment();
        }

        public void ApplySavedEquipment()
        {
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);
            if (skeletonAnimation == null || SaveManager.Instance?.Data?.equipment == null)
                return;

            skeletonAnimation.Initialize(false);
            if (skeletonAnimation.Skeleton == null)
                return;

            EquipmentSaveData equipment = SaveManager.Instance.Data.equipment;
            skeletonAnimation.Skeleton.SetSlotsToSetupPose();
            // Ten slot / attachment theo bo Spine Player moi: slot va attachment trung ten.
            SetEquipmentSlot(WeaponSlot, HasItem(equipment.weapon));
            SetEquipmentSlot(ShieldSlot, HasItem(equipment.shield));
            SetEquipmentSlot(ArmorSlot, HasItem(equipment.armor));
            skeletonAnimation.AnimationState.Apply(skeletonAnimation.Skeleton);
        }

        private const string WeaponSlot = "kiem";
        private const string ShieldSlot = "khien";
        private const string ArmorSlot = "ao chaong";

        /// <summary>
        /// Bat/tat hinh trang bi tren slot. Skeleton.SetAttachment nem loi neu slot
        /// khong ton tai (vd sau khi doi file Spine), nen kiem tra truoc va chi canh bao.
        /// </summary>
        private void SetEquipmentSlot(string slotName, bool visible)
        {
            Spine.Skeleton skeleton = skeletonAnimation.Skeleton;
            if (skeleton.FindSlot(slotName) == null)
            {
                Debug.LogWarning($"[EquipmentVisual] Spine Player khong co slot \"{slotName}\" - bo qua.");
                return;
            }

            if (visible && skeleton.GetAttachment(slotName, slotName) == null)
            {
                Debug.LogWarning($"[EquipmentVisual] Slot \"{slotName}\" khong co attachment cung ten - bo qua.");
                return;
            }

            skeleton.SetAttachment(slotName, visible ? slotName : null);
        }

        private static bool HasItem(EquipmentItemSaveData item)
        {
            return item != null && !string.IsNullOrEmpty(item.itemId);
        }
    }
}
