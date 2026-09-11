using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
            GoldSystem gold = GoldSystem.Instance;
            int current = alchemist != null ? alchemist.HealingLevel : 1;
            int woodCost = alchemist != null ? alchemist.GetUpgradeCost(WitchAbility.Healing) : 0;
            int currentWood = gold != null ? gold.WoodMaterial : 0;

            if (currentLevelText != null)
                currentLevelText.text = $"Healing Level: {current}";
            if (nextUpgradeText != null)
                nextUpgradeText.text = $"Next upgrade: {current} -> {current + 1}";
            if (requirementText != null)
            {
                requirementText.text = $"Wood: {currentWood} / {woodCost}";
                requirementText.color = currentWood >= woodCost
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

    }
}
