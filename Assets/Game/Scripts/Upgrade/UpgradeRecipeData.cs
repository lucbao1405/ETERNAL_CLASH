using System;
using UnityEngine;
using EternalClash.Village;

namespace EternalClash.Upgrade
{
    [Serializable]
    public struct UpgradeMaterialRequirement
    {
        public MaterialType materialType;
        [Min(0)] public int amount;
    }

    [CreateAssetMenu(fileName = "UpgradeRecipe", menuName = "Game/Upgrade Recipe")]
    public sealed class UpgradeRecipeData : ScriptableObject
    {
        public string itemId;
        public UpgradeMaterialRequirement[] requiredMaterials;
        [Min(0)] public int goldCost;
        public int upgradeValue = 1;
    }
}
