using System;
using EternalClash.Core.Save;
using EternalClash.Upgrade;
using UnityEngine;

namespace EternalClash.Village
{
    /// <summary>Persistent alchemist upgrade state backed by UpgradeRecipeData.</summary>
    public sealed class AlchemistUpgradeSystem : MonoBehaviour
    {
        public static AlchemistUpgradeSystem Instance { get; private set; }
        public int HealingLevel { get; private set; } = 1;

        private UpgradeRecipeData[] recipes;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            recipes = Resources.LoadAll<UpgradeRecipeData>("UpgradeRecipes");
        }

        public UpgradeRecipeData GetRecipe(string upgradeId = "healing_ability")
        {
            foreach (UpgradeRecipeData recipe in recipes ?? Array.Empty<UpgradeRecipeData>())
                if (recipe != null && recipe.itemId == upgradeId)
                    return recipe;
            return null;
        }

        public bool CanUpgradeHealing()
        {
            UpgradeRecipeData recipe = GetRecipe();
            GoldSystem gold = GoldSystem.Instance;
            return recipe != null && gold != null && HasResources(gold, recipe);
        }

        public bool TryUpgradeHealing() => TryUpgrade("healing_ability");

        public bool TryUpgrade(string upgradeId)
        {
            UpgradeRecipeData recipe = GetRecipe(upgradeId);
            GoldSystem gold = GoldSystem.Instance;
            if (recipe == null || gold == null || !HasResources(gold, recipe))
                return false;

            gold.SpendGold(recipe.goldCost);
            foreach (UpgradeMaterialRequirement requirement in recipe.requiredMaterials ?? Array.Empty<UpgradeMaterialRequirement>())
                if (requirement.amount > 0)
                    gold.SpendMaterial(requirement.materialType, requirement.amount);

            HealingLevel += Mathf.Max(1, recipe.upgradeValue);
            SaveData data = SaveManager.Instance?.Data;
            if (data != null)
            {
                data.abilities ??= new AbilitySaveData();
                data.abilities.healingLevel = HealingLevel;
                SaveCoordinator.RequestSave();
            }

            Debug.Log($"[ALCHEMIST] {upgradeId} upgraded to level {HealingLevel}");
            return true;
        }

        public void LoadFromSave(SaveData data)
        {
            HealingLevel = Mathf.Max(1, data?.abilities?.healingLevel ?? 1);
        }

        private static bool HasResources(GoldSystem gold, UpgradeRecipeData recipe)
        {
            if (gold.Gold < recipe.goldCost)
                return false;
            foreach (UpgradeMaterialRequirement requirement in recipe.requiredMaterials ?? Array.Empty<UpgradeMaterialRequirement>())
                if (gold.GetMaterial(requirement.materialType) < requirement.amount)
                    return false;
            return true;
        }
    }
}
