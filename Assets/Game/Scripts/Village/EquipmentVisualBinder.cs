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
            skeletonAnimation.Skeleton.SetAttachment("kiem",
                HasItem(equipment.weapon) ? "kiem" : null);
            skeletonAnimation.Skeleton.SetAttachment("shield",
                HasItem(equipment.shield) ? "shield" : null);
            skeletonAnimation.Skeleton.SetAttachment("aochoang",
                HasItem(equipment.armor) ? "aochoang" : null);
            skeletonAnimation.AnimationState.Apply(skeletonAnimation.Skeleton);
        }

        private static bool HasItem(EquipmentItemSaveData item)
        {
            return item != null && !string.IsNullOrEmpty(item.itemId);
        }
    }
}
