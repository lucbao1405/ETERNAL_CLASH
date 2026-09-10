using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EternalClash.Upgrade;
using EternalClash.Village;

namespace EternalClash.UI
{
    /// <summary>View-only binding for the persistent alchemist upgrade system.</summary>
    public sealed class AlchemistUpgradeUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text currentLevelText;
        [SerializeField] private TMP_Text nextUpgradeText;
        [SerializeField] private TMP_Text requirementText;
        [SerializeField] private Button upgradeButton;

        private void Awake()
        {
            AutoWireReferences();
            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(UpgradeHealing);
        }

        private void OnEnable() => Refresh();

        private void AutoWireReferences()
        {
            currentLevelText ??= transform.Find("HealingLevelText")?.GetComponent<TMP_Text>();
            nextUpgradeText ??= transform.Find("NextUpgradeText")?.GetComponent<TMP_Text>();
            requirementText ??= transform.Find("RequirementText")?.GetComponent<TMP_Text>();
            upgradeButton ??= transform.Find("Upgrade_Button")?.GetComponent<Button>();
        }

        public void Refresh()
        {
            AlchemistUpgradeSystem alchemist = AlchemistUpgradeSystem.Instance;
            UpgradeRecipeData recipe = alchemist?.GetRecipe();
            GoldSystem gold = GoldSystem.Instance;
            int current = alchemist != null ? alchemist.HealingLevel : 1;
            int value = recipe != null ? Mathf.Max(1, recipe.upgradeValue) : 1;
            int goldCost = recipe != null ? recipe.goldCost : 0;
            int woodCost = GetRequirement(recipe, MaterialType.Wood);
            int currentGold = gold != null ? gold.Gold : 0;
            int currentWood = gold != null ? gold.WoodMaterial : 0;

            if (currentLevelText != null)
                currentLevelText.text = $"Healing Level: {current}";
            if (nextUpgradeText != null)
                nextUpgradeText.text = $"Next upgrade: {current} -> {current + value}";
            if (requirementText != null)
            {
                requirementText.text = $"Wood: {currentWood} / {woodCost}\nGold: {currentGold} / {goldCost}";
                requirementText.color = currentGold >= goldCost && currentWood >= woodCost
                    ? Color.green : Color.red;
            }
            if (upgradeButton != null)
                upgradeButton.interactable = alchemist != null && alchemist.CanUpgradeHealing();
        }

        private void UpgradeHealing()
        {
            if (AlchemistUpgradeSystem.Instance != null &&
                AlchemistUpgradeSystem.Instance.TryUpgradeHealing())
                Refresh();
        }

        private static int GetRequirement(UpgradeRecipeData recipe, MaterialType type)
        {
            foreach (UpgradeMaterialRequirement requirement in recipe?.requiredMaterials ??
                     System.Array.Empty<UpgradeMaterialRequirement>())
                if (requirement.materialType == type)
                    return requirement.amount;
            return 0;
        }
    }
}
