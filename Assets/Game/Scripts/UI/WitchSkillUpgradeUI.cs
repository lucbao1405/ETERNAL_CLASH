using System;
using EternalClash.Village;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.UI
{
    /// <summary>
    /// Drives the Witch's healing-skill upgrade presentation and confirmation flow.
    /// Persistent upgrade state and resource transactions remain owned by AlchemistUpgradeSystem.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WitchSkillUpgradeUI : MonoBehaviour
    {
        private WitchAbility selectedAbility = WitchAbility.Healing;
        private const int BaseHealingPercent = 30;
        private const int HealingPercentPerLevel = 6;
        private const int BaseCooldownSeconds = 10;
        private const int CooldownReductionPerLevel = 2;
        private const int MinimumCooldownSeconds = 2;

        [Header("Witch Shop References")]
        [SerializeField] private TMP_Text abilityNameText;
        [SerializeField] private TMP_Text healingText;
        [SerializeField] private TMP_Text healingPreviewText;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private TMP_Text cooldownPreviewText;
        [SerializeField] private TMP_Text requirementText;
        [SerializeField] private GameObject materialSlots;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private TMP_Text confirmationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private GameObject successPanel;
        [SerializeField] private TMP_Text successText;
        [SerializeField] private GameObject failurePanel;
        [SerializeField] private TMP_Text failureText;

        private void Awake()
        {
            AutoWire();
            upgradeButton?.onClick.AddListener(OpenConfirmation);
            confirmButton?.onClick.AddListener(ConfirmUpgrade);
            cancelButton?.onClick.AddListener(CloseConfirmation);
            AddPopupCloseListeners(successPanel);
            AddPopupCloseListeners(failurePanel);
            CloseAllPopups();
        }

        private void OnDestroy()
        {
            upgradeButton?.onClick.RemoveListener(OpenConfirmation);
            confirmButton?.onClick.RemoveListener(ConfirmUpgrade);
            cancelButton?.onClick.RemoveListener(CloseConfirmation);
            RemovePopupCloseListeners(successPanel);
            RemovePopupCloseListeners(failurePanel);
        }

        private void OnEnable()
        {
            Refresh();
        }

        /// <summary>Entry point for an authored Witch ability selection button.</summary>
        public void SelectHealingAbility()
        {
            selectedAbility = WitchAbility.Healing;
            CloseAllPopups();
            Refresh();
        }

        /// <summary>Entry point for an authored Witch cooldown ability selection button.</summary>
        public void SelectCooldownAbility()
        {
            selectedAbility = WitchAbility.Cooldown;
            CloseAllPopups();
            Refresh();
        }

        public void Refresh()
        {
            AlchemistUpgradeSystem witchSkills = AlchemistUpgradeSystem.Instance;
            int level = GetLevel(witchSkills);

            abilityNameText.SetTextIfPresent(GetAbilityName());
            healingText.SetTextIfPresent("Healing Effect");
            healingPreviewText.SetTextIfPresent($"{GetHealingPercent(level)}% -> {GetHealingPercent(level + 1)}%");
            cooldownText.SetTextIfPresent("Cooldown Reduction");
            cooldownPreviewText.SetTextIfPresent($"{GetCooldownSeconds(level)}s -> {GetCooldownSeconds(level + 1)}s");
            requirementText.SetTextIfPresent(FormatRequirements(witchSkills));
            PopulateMaterialSlots(witchSkills);

            if (upgradeButton != null)
                upgradeButton.interactable = witchSkills != null;
        }

        private void OpenConfirmation()
        {
            AlchemistUpgradeSystem witchSkills = AlchemistUpgradeSystem.Instance;
            if (witchSkills == null)
            {
                ShowFailure("Upgrade data is unavailable.");
                return;
            }

            int level = GetLevel(witchSkills);
            confirmationText.SetTextIfPresent(
                $"{GetAbilityName()}\n\nUpgrade:\nLevel {level} -> Level {level + 1}\n\n" +
                $"Healing Effect: {GetHealingPercent(level)}% -> {GetHealingPercent(level + 1)}%\n" +
                $"Cooldown: {GetCooldownSeconds(level)}s -> {GetCooldownSeconds(level + 1)}s\n\n" +
                FormatRequirements(witchSkills));
            confirmationPanel?.SetActive(true);
        }

        private void ConfirmUpgrade()
        {
            AlchemistUpgradeSystem witchSkills = AlchemistUpgradeSystem.Instance;
            int previousLevel = GetLevel(witchSkills);
            CloseConfirmation();

            if (witchSkills == null || !witchSkills.TryUpgrade(selectedAbility))
            {
                ShowFailure("Not enough material.");
                Refresh();
                return;
            }

            int currentLevel = GetLevel(witchSkills);
            successText.SetTextIfPresent(
                $"{GetAbilityName()}\n\nLevel {currentLevel}\n\n" +
                $"Healing Effect: {GetHealingPercent(previousLevel)}% -> {GetHealingPercent(currentLevel)}%\n" +
                $"Cooldown: {GetCooldownSeconds(previousLevel)}s -> {GetCooldownSeconds(currentLevel)}s");
            failurePanel?.SetActive(false);
            successPanel?.SetActive(true);
            Refresh();
        }

        private void ShowFailure(string message)
        {
            failureText.SetTextIfPresent(message);
            successPanel?.SetActive(false);
            failurePanel?.SetActive(true);
        }

        private void CloseConfirmation() => confirmationPanel?.SetActive(false);

        private void CloseAllPopups()
        {
            CloseConfirmation();
            successPanel?.SetActive(false);
            failurePanel?.SetActive(false);
        }

        private void AutoWire()
        {
            abilityNameText ??= FindText("Name/Text");
            healingText ??= FindText("Khung/HieuQuaHoiMau/Mota");
            healingPreviewText ??= FindText("Khung/HieuQuaHoiMau/%");
            cooldownText ??= FindText("Khung/GiamHoiChieu/Mota");
            cooldownPreviewText ??= FindText("Khung/GiamHoiChieu/%");
            requirementText ??= FindText("Vat_Pham_Can/REQUIRES") ?? FindFirstText("Vat_Pham_Can/Hienthivp");
            materialSlots ??= transform.Find("Vat_Pham_Can/Hienthivp")?.gameObject;
            upgradeButton ??= transform.Find("UPGRADE/Ok")?.GetComponent<Button>();

            confirmationPanel ??= transform.Find("ThongTinNangCap")?.gameObject;
            confirmationText ??= FindText(confirmationPanel, "Thong_bao/Chi_tiet") ?? FindFirstText(confirmationPanel);
            confirmButton ??= FindButton(confirmationPanel, "ok", "confirm", "yes");
            cancelButton ??= FindButton(confirmationPanel, "cancel", "no", "x", "close");

            successPanel ??= transform.Find("UpGradeSuccess")?.gameObject;
            successText ??= FindText(successPanel, "Thong_bao/Chi_tiet/Chiso_tang") ?? FindFirstText(successPanel);
            failurePanel ??= transform.Find("UpGradeFail")?.gameObject;
            failureText ??= FindText(failurePanel, "Thong_bao/Chi_tiet/Li do") ?? FindFirstText(failurePanel);
        }

        private TMP_Text FindText(string path) => transform.Find(path)?.GetComponent<TMP_Text>();

        private static TMP_Text FindText(GameObject root, string path) =>
            root != null ? root.transform.Find(path)?.GetComponent<TMP_Text>() : null;

        private TMP_Text FindFirstText(string path)
        {
            Transform root = transform.Find(path);
            return root != null ? root.GetComponentInChildren<TMP_Text>(true) : null;
        }

        private static TMP_Text FindFirstText(GameObject root) =>
            root != null ? root.GetComponentInChildren<TMP_Text>(true) : null;

        private static Button FindButton(GameObject root, params string[] names)
        {
            if (root == null)
                return null;

            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                string buttonName = button.name.ToLowerInvariant();
                foreach (string name in names)
                    if (buttonName == name || buttonName.Contains(name))
                        return button;
            }
            return null;
        }

        private void AddPopupCloseListeners(GameObject popup)
        {
            if (popup == null)
                return;

            foreach (Button button in popup.GetComponentsInChildren<Button>(true))
                if (button.name.Equals("x", StringComparison.OrdinalIgnoreCase) ||
                    button.name.IndexOf("close", StringComparison.OrdinalIgnoreCase) >= 0)
                    button.onClick.AddListener(CloseAllPopups);
        }

        private void RemovePopupCloseListeners(GameObject popup)
        {
            if (popup == null)
                return;

            foreach (Button button in popup.GetComponentsInChildren<Button>(true))
                button.onClick.RemoveListener(CloseAllPopups);
        }

        private void PopulateMaterialSlots(AlchemistUpgradeSystem witchSkills)
        {
            if (materialSlots == null)
                return;

            TMP_Text[] slotTexts = materialSlots.GetComponentsInChildren<TMP_Text>(true);
            GoldSystem resources = GoldSystem.Instance;
            for (int i = 0; i < slotTexts.Length; i++)
            {
                if (i > 0 || witchSkills == null)
                {
                    slotTexts[i].text = string.Empty;
                    continue;
                }

                MaterialType material = witchSkills.GetRequiredMaterial(selectedAbility);
                int current = resources != null ? resources.GetMaterial(material) : 0;
                slotTexts[i].text = $"{GetMaterialName(material)}: {current}/{witchSkills.GetUpgradeCost(selectedAbility)}";
            }
        }

        private static int GetHealingPercent(int level) =>
            BaseHealingPercent + Mathf.Max(0, level - 1) * HealingPercentPerLevel;

        private static int GetCooldownSeconds(int level) =>
            Mathf.Max(MinimumCooldownSeconds, BaseCooldownSeconds - Mathf.Max(0, level - 1) * CooldownReductionPerLevel);

        private string FormatRequirements(AlchemistUpgradeSystem witchSkills)
        {
            if (witchSkills == null)
                return "Materials required unavailable.";

            GoldSystem resources = GoldSystem.Instance;
            MaterialType material = witchSkills.GetRequiredMaterial(selectedAbility);
            int current = resources != null ? resources.GetMaterial(material) : 0;
            return $"Materials required:\n{GetMaterialName(material)}: {current} / {witchSkills.GetUpgradeCost(selectedAbility)}";
        }

        private int GetLevel(AlchemistUpgradeSystem witchSkills)
        {
            if (witchSkills == null)
                return 1;
            return selectedAbility == WitchAbility.Healing ? witchSkills.HealingLevel : witchSkills.CooldownLevel;
        }

        private string GetAbilityName() => selectedAbility == WitchAbility.Healing ? "Healing" : "Cooldown Reduction";

        private static string GetMaterialName(MaterialType material) =>
            material == MaterialType.Ore ? "Copper Ore" : material.ToString();
    }

    internal static class WitchSkillUpgradeTextExtensions
    {
        public static void SetTextIfPresent(this TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }
    }
}
