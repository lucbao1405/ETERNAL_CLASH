using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EternalClash.Core.Save;
using EternalClash.Data;
using EternalClash.Village;

namespace EternalClash.UI
{
    public sealed class ShopPhuThuyController : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text healingLabelText;
        [SerializeField] private TMP_Text healingValueText;
        [SerializeField] private TMP_Text cooldownLabelText;
        [SerializeField] private TMP_Text cooldownValueText;
        [SerializeField] private Button healingSelectButton;
        [SerializeField] private Button cooldownSelectButton;
        [SerializeField] private GameObject thongTinNangCap;
        [SerializeField] private TMP_Text upgradePreviewText;
        [SerializeField] private ScrollRect upgradePreviewScrollRect;

        [Header("Controls")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject materialSlots;
        [SerializeField] private GameObject successPanel;
        [SerializeField] private TMP_Text successText;
        [SerializeField] private GameObject failurePanel;
        [SerializeField] private TMP_Text failureText;

        private WitchAbility selectedAbility = WitchAbility.Healing;

        private void Awake()
        {
            AutoWire();
            ConfigureUpgradePreviewScroll();
            upgradeButton?.onClick.AddListener(HandleUpgrade);
            closeButton?.onClick.AddListener(Close);
            healingSelectButton?.onClick.AddListener(SelectHealingSkill);
            cooldownSelectButton?.onClick.AddListener(SelectCooldownSkill);
            AddPopupCloseListener(successPanel);
            AddPopupCloseListener(failurePanel);
        }

        private void OnEnable()
        {
            AutoWire();
            ConfigureUpgradePreviewScroll();
            SelectHealingSkill();
            successPanel?.SetActive(false);
            failurePanel?.SetActive(false);
        }

        private void OnDestroy()
        {
            upgradeButton?.onClick.RemoveListener(HandleUpgrade);
            closeButton?.onClick.RemoveListener(Close);
            healingSelectButton?.onClick.RemoveListener(SelectHealingSkill);
            cooldownSelectButton?.onClick.RemoveListener(SelectCooldownSkill);
            RemovePopupCloseListener(successPanel);
            RemovePopupCloseListener(failurePanel);
        }

        public void Open()
        {
            ShopPanelAnimator animator = GetComponent<ShopPanelAnimator>();
            if (animator != null)
                animator.Open();
            else
                gameObject.SetActive(true);
            SelectHealingSkill();
        }

        public void SelectHealingSkill()
        {
            selectedAbility = WitchAbility.Healing;
            UpdateState();
        }

        public void SelectCooldownSkill()
        {
            selectedAbility = WitchAbility.Cooldown;
            UpdateState();
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

        private void UpdateState()
        {
            SelectHealing();
        }

        public void SelectHealing()
        {
            AlchemistUpgradeSystem witchSkills = AlchemistUpgradeSystem.Instance;
            int healingBonus = witchSkills != null ? witchSkills.GetHealingBonusPercent() : 0;
            int cooldownSeconds = witchSkills != null ? witchSkills.GetCooldownValue() : AlchemistUpgradeSystem.BaseCooldownSeconds;

            SetText(nameText, selectedAbility == WitchAbility.Healing ? "Healing" : "Cooldown");
            SetText(healingLabelText, "Healing");
            SetText(healingValueText, "+" + healingBonus + "%");
            SetText(cooldownLabelText, "Cooldown");
            SetText(cooldownValueText, cooldownSeconds + " sec");
            SetText(upgradePreviewText, "Healing:\n\nCurrent:\n+" + healingBonus +
                "%\n\nAfter:\n+" + (healingBonus + AlchemistUpgradeSystem.HealingBonusPerLevelPercent) +
                "%\n\nCooldown:\n\nCurrent:\n" + cooldownSeconds + " sec\n\nAfter:\n" +
                Mathf.Max(0, cooldownSeconds - AlchemistUpgradeSystem.CooldownReductionPerLevelSeconds) + " sec");
            RefreshUpgradePreviewLayout();
            thongTinNangCap?.SetActive(true);
            RefreshMaterials(witchSkills);

            if (upgradeButton != null)
                upgradeButton.interactable = witchSkills != null;
        }

        public void HandleUpgrade()
        {
            AlchemistUpgradeSystem witchSkills = AlchemistUpgradeSystem.Instance;
            int previousLevel = witchSkills != null ? GetLevel(witchSkills, selectedAbility) : 0;
            if (witchSkills == null || !witchSkills.TryUpgrade(selectedAbility))
            {
                successPanel?.SetActive(false);
                SetText(failureText, "Not enough material.");
                failurePanel?.SetActive(true);
                RefreshMaterials(witchSkills);
                return;
            }

            int currentLevel = GetLevel(witchSkills, selectedAbility);
            SetText(successText, FormatSuccess(selectedAbility, previousLevel, currentLevel));
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
            healingSelectButton ??= transform.Find("Khung/HieuQuaHoiMau")?.GetComponent<Button>();
            cooldownSelectButton ??= transform.Find("Khung/GiamHoiChieu")?.GetComponent<Button>();
            upgradeButton ??= transform.Find("UPGRADE")?.GetComponent<Button>();
            closeButton ??= transform.Find("X")?.GetComponent<Button>();
            materialSlots ??= transform.Find("Vat_Pham_Can/Hienthivp")?.gameObject;
            thongTinNangCap ??= transform.Find("ThongTinNangCap")?.gameObject;
            upgradePreviewText ??= FindText("ThongTinNangCap/ContentArea/Content/TTNC_Text");
            upgradePreviewText ??= FindText("ThongTinNangCap/TTNC_Text") ?? FindText("ThongTinNangCap/Image/Text");
            upgradePreviewScrollRect ??= thongTinNangCap?.transform.Find("ContentArea")?.GetComponent<ScrollRect>();
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

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private void ConfigureUpgradePreviewScroll()
        {
            if (thongTinNangCap == null || upgradePreviewText == null)
                return;

            Transform contentArea = thongTinNangCap.transform.Find("ContentArea");
            Transform content = contentArea?.Find("Content");
            if (contentArea == null || content == null)
                return;

            upgradePreviewScrollRect ??= contentArea.GetComponent<ScrollRect>();
            upgradePreviewScrollRect ??= contentArea.gameObject.AddComponent<ScrollRect>();
            if (contentArea.GetComponent<RectMask2D>() == null)
                contentArea.gameObject.AddComponent<RectMask2D>();

            upgradePreviewScrollRect.viewport = contentArea as RectTransform;
            upgradePreviewScrollRect.content = content as RectTransform;
            upgradePreviewScrollRect.horizontal = false;
            upgradePreviewScrollRect.vertical = true;
            upgradePreviewScrollRect.movementType = ScrollRect.MovementType.Clamped;
            upgradePreviewScrollRect.scrollSensitivity = 20f;

            RectTransform contentRect = content as RectTransform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);

            VerticalLayoutGroup layoutGroup = content.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
                layoutGroup.enabled = false;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter != null)
                fitter.enabled = false;

            RectTransform textRect = upgradePreviewText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.offsetMin = new Vector2(20f, textRect.offsetMin.y);
            textRect.offsetMax = new Vector2(-20f, textRect.offsetMax.y);
            upgradePreviewText.enableWordWrapping = true;
            upgradePreviewText.overflowMode = TextOverflowModes.Overflow;
            upgradePreviewText.alignment = TextAlignmentOptions.Top;
        }

        private void RefreshUpgradePreviewLayout()
        {
            if (upgradePreviewScrollRect == null)
                return;

            ConfigureUpgradePreviewScroll();
            upgradePreviewText.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            RectTransform viewport = upgradePreviewScrollRect.viewport;
            RectTransform content = upgradePreviewScrollRect.content;
            RectTransform text = upgradePreviewText.rectTransform;
            float textHeight = upgradePreviewText.preferredHeight;
            float contentHeight = Mathf.Max(viewport.rect.height, textHeight);

            text.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            upgradePreviewScrollRect.verticalNormalizedPosition = 1f;
        }

        private void RefreshMaterials(AlchemistUpgradeSystem witchSkills)
        {
            if (materialSlots == null)
                return;

            Transform[] slots = new Transform[3];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = materialSlots.transform.Find("Slot_" + (i + 1));

            MaterialType material = witchSkills != null
                ? witchSkills.GetRequiredMaterial(selectedAbility)
                : MaterialType.Wood;
            int required = witchSkills != null ? witchSkills.GetUpgradeCost(selectedAbility) : 0;
            string itemId = AlchemistUpgradeSystem.GetMaterialItemId(material);
            int current = AlchemistUpgradeSystem.GetMaterialAmount(SaveManager.Instance?.Data?.inventory?.items, itemId);
            ItemData item = ItemCatalog.Find(itemId);

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                bool used = i == 0 && witchSkills != null;
                slots[i].gameObject.SetActive(used);
                if (!used)
                    continue;

                Image icon = slots[i].Find("ItemPic")?.GetComponent<Image>();
                TMP_Text quantity = slots[i].Find("Soluong")?.GetComponent<TMP_Text>();
                ShopMaterialDisplay.Refresh(icon, quantity, item, required, current >= required);
            }
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

        private static int GetLevel(AlchemistUpgradeSystem witchSkills, WitchAbility ability)
        {
            return ability == WitchAbility.Healing ? witchSkills.HealingLevel : witchSkills.CooldownLevel;
        }

        private string FormatSuccess(WitchAbility ability, int previousLevel, int currentLevel)
        {
            if (ability == WitchAbility.Cooldown)
            {
                int oldValue = Mathf.Max(0, AlchemistUpgradeSystem.BaseCooldownSeconds -
                    previousLevel * AlchemistUpgradeSystem.CooldownReductionPerLevelSeconds);
                int newValue = Mathf.Max(0, AlchemistUpgradeSystem.BaseCooldownSeconds -
                    currentLevel * AlchemistUpgradeSystem.CooldownReductionPerLevelSeconds);
                return "Cooldown\n\nLevel:\n" + previousLevel + " -> " + currentLevel +
                    "\n\nCooldown:\n" + oldValue + " sec -> " + newValue + " sec";
            }

            return "Healing\n\nLevel:\n" + previousLevel + " -> " + currentLevel +
                "\n\nHeal:\n+" + AlchemistUpgradeSystem.GetHealingBonusPercent(previousLevel) + "% -> +" +
                AlchemistUpgradeSystem.GetHealingBonusPercent(currentLevel) + "%";
        }

    }
}
