using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EternalClash.Village;

namespace EternalClash.UI
{
    public sealed class ShopPhuThuyController : MonoBehaviour
    {
        private const int BaseHealingPercent = 30;
        private const int HealingPercentPerLevel = 6;
        private const int BaseCooldownSeconds = 15;
        private const int CooldownReductionPerLevel = 2;
        private const int MinimumCooldownSeconds = 2;

        [Header("Stats")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text healingLabelText;
        [SerializeField] private TMP_Text healingValueText;
        [SerializeField] private TMP_Text cooldownLabelText;
        [SerializeField] private TMP_Text cooldownValueText;

        [Header("Controls")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject materialSlots;
        [SerializeField] private GameObject successPanel;
        [SerializeField] private TMP_Text successText;
        [SerializeField] private GameObject failurePanel;
        [SerializeField] private TMP_Text failureText;

        private void Awake()
        {
            AutoWire();
            upgradeButton?.onClick.AddListener(UpgradeHealing);
            closeButton?.onClick.AddListener(Close);
            AddPopupCloseListener(successPanel);
            AddPopupCloseListener(failurePanel);
        }

        private void OnEnable()
        {
            SelectHealing();
            successPanel?.SetActive(false);
            failurePanel?.SetActive(false);
        }

        private void OnDestroy()
        {
            upgradeButton?.onClick.RemoveListener(UpgradeHealing);
            closeButton?.onClick.RemoveListener(Close);
            RemovePopupCloseListener(successPanel);
            RemovePopupCloseListener(failurePanel);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            SelectHealing();
        }

        public void SelectHealingSkill()
        {
            SelectHealing();
        }

        public void Close()
        {
            successPanel?.SetActive(false);
            failurePanel?.SetActive(false);
            ShopPanelAnimator animator = GetComponent<ShopPanelAnimator>();
            if (animator != null)
                animator.Close();
            else
                gameObject.SetActive(false);
        }

        public void SelectHealing()
        {
            AlchemistUpgradeSystem witchSkills = AlchemistUpgradeSystem.Instance;
            int healingLevel = witchSkills != null ? witchSkills.HealingLevel : 1;
            int cooldownLevel = witchSkills != null ? witchSkills.CooldownLevel : 1;

            if (nameText != null) nameText.text = "Healing";
            if (healingLabelText != null) healingLabelText.text = "Healing";
            if (healingValueText != null) healingValueText.text = "Current: +" + GetHealingPercent(healingLevel) + "%\nAfter upgrade: +" +
                GetHealingPercent(healingLevel + 1) + "%";
            if (cooldownLabelText != null) cooldownLabelText.text = "Cooldown";
            if (cooldownValueText != null) cooldownValueText.text = GetCooldownSeconds(cooldownLevel) + " sec";
            RefreshMaterials(witchSkills);

            if (upgradeButton != null)
                upgradeButton.interactable = witchSkills != null;
        }

        public void UpgradeHealing()
        {
            AlchemistUpgradeSystem witchSkills = AlchemistUpgradeSystem.Instance;
            int previousLevel = witchSkills != null ? witchSkills.HealingLevel : 1;
            if (witchSkills == null || !witchSkills.TryUpgrade(WitchAbility.Healing))
            {
                successPanel?.SetActive(false);
                if (failureText != null) failureText.text = "Not enough material.";
                failurePanel?.SetActive(true);
                return;
            }

            int currentLevel = witchSkills.HealingLevel;
            if (successText != null) successText.text = "Healing\n\nLevel:\n" + previousLevel + " -> " + currentLevel +
                "\n\nHeal:\n+" + GetHealingPercent(previousLevel) + "% -> +" + GetHealingPercent(currentLevel) + "%";
            failurePanel?.SetActive(false);
            successPanel?.SetActive(true);
            SelectHealing();
        }

        private void AutoWire()
        {
            nameText ??= FindText("Name/Text");
            healingLabelText ??= FindText("Khung/HieuQuaHoiMau/Mota");
            healingValueText ??= FindText("Khung/HieuQuaHoiMau/%");
            cooldownLabelText ??= FindText("Khung/GiamHoiChieu/Mota");
            cooldownValueText ??= FindText("Khung/GiamHoiChieu/%");
            upgradeButton ??= transform.Find("UPGRADE")?.GetComponent<Button>();
            closeButton ??= transform.Find("X")?.GetComponent<Button>();
            materialSlots ??= transform.Find("Vat_Pham_Can/Hienthivp")?.gameObject;
            successPanel ??= transform.Find("UpGradeSuccess")?.gameObject;
            failurePanel ??= transform.Find("UpGradeFail")?.gameObject;
            successText ??= FindText("UpGradeSuccess/Thong_bao/Chi_tiet/Chiso_tang") ?? FindFirstText(successPanel);
            failureText ??= FindText("UpGradeFail /Thong_bao/Chi_tiet/Li do") ?? FindFirstText(failurePanel);
        }

        private TMP_Text FindText(string path)
        {
            return transform.Find(path)?.GetComponent<TMP_Text>();
        }

        private static TMP_Text FindFirstText(GameObject root)
        {
            return root != null ? root.GetComponentInChildren<TMP_Text>(true) : null;
        }

        private void RefreshMaterials(AlchemistUpgradeSystem witchSkills)
        {
            if (materialSlots == null)
                return;

            TMP_Text[] texts = materialSlots.GetComponentsInChildren<TMP_Text>(true);
            int cost = witchSkills != null ? witchSkills.GetUpgradeCost(WitchAbility.Healing) : 0;
            for (int i = 0; i < texts.Length; i++)
                texts[i].text = i == 0 ? "Wood x" + cost : string.Empty;
        }

        private void AddPopupCloseListener(GameObject popup)
        {
            if (popup == null)
                return;

            Transform close = popup.transform.Find("X");
            Button button = close != null ? close.GetComponent<Button>() : null;
            if (button != null)
                button.onClick.AddListener(ClosePopups);
        }

        private void RemovePopupCloseListener(GameObject popup)
        {
            Transform close = popup != null ? popup.transform.Find("X") : null;
            Button button = close != null ? close.GetComponent<Button>() : null;
            if (button != null)
                button.onClick.RemoveListener(ClosePopups);
        }

        private void ClosePopups()
        {
            successPanel?.SetActive(false);
            failurePanel?.SetActive(false);
        }

        private static int GetHealingPercent(int level)
        {
            return BaseHealingPercent + Mathf.Max(0, level - 1) * HealingPercentPerLevel;
        }

        private static int GetCooldownSeconds(int level)
        {
            return Mathf.Max(MinimumCooldownSeconds,
                BaseCooldownSeconds - Mathf.Max(0, level - 1) * CooldownReductionPerLevel);
        }
    }
}
