using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalClash.Village;
using EternalClash.Core;
using EternalClash.Upgrade;

namespace EternalClash.UI
{
    public class TownUIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainHallPanel;
        [SerializeField] private GameObject blacksmithPanel;
        [SerializeField] private GameObject mailboxPanel;

        [Header("Main Hall / Stats UI")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text expText;
        [SerializeField] private TMP_Text statPointsText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text materialsText;
        [SerializeField] private TMP_Text strText;
        [SerializeField] private TMP_Text intText;
        [SerializeField] private TMP_Text vitText;
        [SerializeField] private TMP_Text luckText;
        [SerializeField] private TMP_Text resetCostText;
        [SerializeField] private Button resetStatsButton;

        [Header("Blacksmith UI")]
        [SerializeField] private TMP_Text weaponTierText;
        [SerializeField] private TMP_Text armorTierText;
        [SerializeField] private TMP_Text weaponUpgradeCostText;
        [SerializeField] private TMP_Text armorUpgradeCostText;
        [SerializeField] private Button upgradeWeaponButton;
        [SerializeField] private Button upgradeArmorButton;

        [Header("Mailbox / Bonds UI")]
        [SerializeField] private TMP_Text affinityPointsText;
        [SerializeField] private TMP_Text lettersListText;

        [Header("Battle Gate")]
        [SerializeField] private Button startBattleButton;
        [SerializeField] private TMP_Text battleButtonLabel;
        [SerializeField] private GameObject injuredNoticeRoot;
        [SerializeField] private TMP_Text injuredNoticeText;
        private GameObject equipmentGuide;

        private void Start()
        {
            ShowMainHall();
            RefreshAllUI();
            ShowEquipmentGuideIfNeeded();

            if (PlayerStatSystem.Instance != null)
                PlayerStatSystem.Instance.OnStatsChanged += RefreshAllUI;

            if (GoldSystem.Instance != null)
            {
                GoldSystem.Instance.OnGoldChanged += _ => RefreshAllUI();
                GoldSystem.Instance.OnMaterialsChanged += (_, _) => RefreshAllUI();
            }

            if (AffinityManager.Instance != null)
                AffinityManager.Instance.OnAffinityPointsChanged += _ => RefreshAllUI();

            var condition = PlayerConditionSystem.Instance;
            if (condition != null)
            {
                condition.OnConditionChanged += _ => RefreshBattleGate();
                condition.OnRecoveredHpChanged += (_, _) => RefreshBattleGate();
                condition.OnRecoveryCompleted += RefreshBattleGate;
                condition.BeginRecoveryIfNeeded();
                RefreshBattleGate();
            }
        }

        private void OnDestroy()
        {
            if (PlayerStatSystem.Instance != null)
                PlayerStatSystem.Instance.OnStatsChanged -= RefreshAllUI;

            var condition = PlayerConditionSystem.Instance;
            if (condition != null)
            {
                condition.OnConditionChanged -= _ => RefreshBattleGate();
                condition.OnRecoveredHpChanged -= (_, _) => RefreshBattleGate();
                condition.OnRecoveryCompleted -= RefreshBattleGate;
            }
        }

        public void ShowMainHall()
        {
            mainHallPanel?.SetActive(true);
            blacksmithPanel?.SetActive(false);
            mailboxPanel?.SetActive(false);
            RefreshAllUI();
            equipmentGuide?.SetActive(false);
        }

        public void ShowBlacksmith()
        {
            mainHallPanel?.SetActive(false);
            blacksmithPanel?.SetActive(true);
            mailboxPanel?.SetActive(false);
            RefreshAllUI();
        }

        public void ShowMailbox()
        {
            mainHallPanel?.SetActive(false);
            blacksmithPanel?.SetActive(false);
            mailboxPanel?.SetActive(true);
            RefreshAllUI();
        }

        public void RefreshAllUI()
        {
            var stats = PlayerStatSystem.Instance;
            var goldSys = GoldSystem.Instance;
            var smith = BlacksmithCraftingSystem.Instance;
            var affinity = AffinityManager.Instance;

            if (stats != null)
            {
                if (levelText != null) levelText.text = $"Cấp: {stats.Level}";
                if (expText != null) expText.text = $"EXP: {stats.CurrentExp} / {stats.RequiredExp}";
                if (statPointsText != null) statPointsText.text = $"Điểm tiềm năng: {stats.StatPoints}";

                if (strText != null) strText.text = $"STR: {stats.Strength} (+{stats.Strength * 2} DMG)";
                if (intText != null) intText.text = $"INT: {stats.Intelligence} (+{stats.Intelligence * 5}% EXP)";
                if (vitText != null) vitText.text = $"VIT: {stats.Vitality} (+{stats.Vitality * 15} HP)";
                if (luckText != null) luckText.text = $"LUCK: {stats.Luck} (+{(stats.Luck * 0.5f):F1}% Crit)";

                if (resetCostText != null)
                {
                    int cost = stats.GetResetCost();
                    resetCostText.text = cost == 0 ? "Tẩy Điểm: Miễn Phí" : $"Tẩy Điểm: {cost} Vàng";
                }

                if (resetStatsButton != null)
                    resetStatsButton.interactable = stats.CanResetStats();
            }

            if (goldSys != null)
            {
                if (goldText != null) goldText.text = $"Vàng: {goldSys.Gold}";
                if (materialsText != null) materialsText.text = $"Quặng: {goldSys.OreMaterial} | Da thú: {goldSys.LeatherMaterial}";
            }

            if (smith != null)
            {
                if (weaponTierText != null) weaponTierText.text = $"Vũ Khí: Bậc {smith.WeaponTier}";
                if (armorTierText != null) armorTierText.text = $"Áo Giáp: Bậc {smith.ArmorTier}";

                UpgradeRecipeData weaponRecipe = smith.GetRecipe(ItemSlot.Weapon);
                int nextWpCost = weaponRecipe != null ? weaponRecipe.goldCost : 0;
                int nextWpOre = GetRequirement(weaponRecipe, MaterialType.Ore);
                int nextWpLeather = GetRequirement(weaponRecipe, MaterialType.Leather);

                if (weaponUpgradeCostText != null)
                    weaponUpgradeCostText.text = $"Phí: {nextWpCost} Vàng, {nextWpOre} Quặng, {nextWpLeather} Da";

                UpgradeRecipeData armorRecipe = smith.GetRecipe(ItemSlot.Armor);
                int nextArmCost = armorRecipe != null ? armorRecipe.goldCost : 0;
                int nextArmOre = GetRequirement(armorRecipe, MaterialType.Ore);
                int nextArmLeather = GetRequirement(armorRecipe, MaterialType.Leather);

                if (armorUpgradeCostText != null)
                    armorUpgradeCostText.text = $"Phí: {nextArmCost} Vàng, {nextArmOre} Quặng, {nextArmLeather} Da";

                if (upgradeWeaponButton != null) upgradeWeaponButton.interactable = smith.CanUpgradeWeapon();
                if (upgradeArmorButton != null) upgradeArmorButton.interactable = smith.CanUpgradeArmor();
            }

            if (affinity != null)
            {
                if (affinityPointsText != null) affinityPointsText.text = $"Điểm Hảo Cảm: {affinity.AffinityPoints}";
                if (lettersListText != null)
                {
                    string summary = "";
                    foreach (var m in affinity.Milestones)
                    {
                        bool unlocked = affinity.IsLetterUnlocked(m.letterId);
                        string status = unlocked ? "[ĐÃ MỞ]" : $"[Cần {m.requiredPoints} pts]";
                        summary += $"• {m.letterTitle} {status}\n  - Gửi bởi: {m.letterSender}\n  - Hiệu ứng: {m.permanentBuffDescription}\n\n";
                    }
                    lettersListText.text = summary;
                }
            }
        }

        // Button Callbacks
        public void OnClick_AddStrength() => PlayerStatSystem.Instance?.AllocateStrength();
        public void OnClick_AddIntelligence() => PlayerStatSystem.Instance?.AllocateIntelligence();
        public void OnClick_AddVitality() => PlayerStatSystem.Instance?.AllocateVitality();
        public void OnClick_AddLuck() => PlayerStatSystem.Instance?.AllocateLuck();
        public void OnClick_ResetStats() => PlayerStatSystem.Instance?.ResetStats();

        public void OnClick_UpgradeWeapon() => BlacksmithCraftingSystem.Instance?.UpgradeWeapon();
        public void OnClick_UpgradeArmor() => BlacksmithCraftingSystem.Instance?.UpgradeArmor();

        private static int GetRequirement(UpgradeRecipeData recipe, MaterialType type)
        {
            if (recipe?.requiredMaterials == null)
                return 0;
            foreach (UpgradeMaterialRequirement requirement in recipe.requiredMaterials)
                if (requirement.materialType == type)
                    return requirement.amount;
            return 0;
        }

        public void OnClick_StartBattle()
        {
            var condition = PlayerConditionSystem.Instance;
            if (condition != null && !condition.CanStartBattle())
            {
                Debug.LogWarning(condition.GetInjuredBlockReason());
                return;
            }

            EternalClash.Core.SceneLoader.LoadBattle();
        }

        public void RefreshBattleGate()
        {
            var condition = PlayerConditionSystem.Instance;

            if (startBattleButton != null)
            {
                bool canBattle = condition == null || condition.CanStartBattle();
                startBattleButton.interactable = canBattle;
            }

            if (battleButtonLabel != null)
            {
                bool injured = condition != null && condition.IsInjured && !condition.CanStartBattle();
                battleButtonLabel.text = injured ? "Recovering..." : "Start Battle";
            }

            if (injuredNoticeRoot != null)
            {
                bool show = condition != null && condition.IsInjured && !condition.CanStartBattle();
                injuredNoticeRoot.SetActive(show);
            }

            if (injuredNoticeText != null && condition != null && condition.IsInjured)
            {
                injuredNoticeText.text = condition.GetInjuredBlockReason();
            }
        }

        private void ShowEquipmentGuideIfNeeded()
        {
            if (EquipmentSystem.Instance == null || !EquipmentSystem.Instance.HasNewEquipment)
                return;

            if (equipmentGuide == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas == null)
                    return;
                equipmentGuide = new GameObject("NewEquipmentGuide", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                equipmentGuide.transform.SetParent(canvas.transform, false);
                RectTransform rect = equipmentGuide.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 260f);
                rect.sizeDelta = new Vector2(620f, 70f);
                equipmentGuide.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.25f, 0.96f);
                equipmentGuide.GetComponent<Button>().onClick.AddListener(ShowBlacksmith);
                CreateGuideLabel(equipmentGuide.transform);
            }

            equipmentGuide.SetActive(true);
            equipmentGuide.transform.SetAsLastSibling();
        }

        private static void CreateGuideLabel(Transform parent)
        {
            GameObject label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(parent, false);
            RectTransform rect = label.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Text text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "New equipment found. Visit the Blacksmith to equip it.";
        }
}

}
