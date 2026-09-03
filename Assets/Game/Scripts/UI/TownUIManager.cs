using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalClash.Village;
using EternalClash.Core;

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

        private void Start()
        {
            ShowMainHall();
            RefreshAllUI();

            if (PlayerStatSystem.Instance != null)
                PlayerStatSystem.Instance.OnStatsChanged += RefreshAllUI;

            if (GoldSystem.Instance != null)
            {
                GoldSystem.Instance.OnGoldChanged += _ => RefreshAllUI();
                GoldSystem.Instance.OnMaterialsChanged += (_, _) => RefreshAllUI();
            }

            if (AffinityManager.Instance != null)
                AffinityManager.Instance.OnAffinityPointsChanged += _ => RefreshAllUI();
        }

        private void OnDestroy()
        {
            if (PlayerStatSystem.Instance != null)
                PlayerStatSystem.Instance.OnStatsChanged -= RefreshAllUI;
        }

        public void ShowMainHall()
        {
            mainHallPanel?.SetActive(true);
            blacksmithPanel?.SetActive(false);
            mailboxPanel?.SetActive(false);
            RefreshAllUI();
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

                int nextWpCost = BlacksmithCraftingSystem.TIER1_GOLD_COST * (smith.WeaponTier + 1);
                int nextWpOre = BlacksmithCraftingSystem.TIER1_ORE_COST * (smith.WeaponTier + 1);
                int nextWpLeather = BlacksmithCraftingSystem.TIER1_LEATHER_COST * (smith.WeaponTier + 1);

                if (weaponUpgradeCostText != null)
                    weaponUpgradeCostText.text = $"Phí: {nextWpCost} Vàng, {nextWpOre} Quặng, {nextWpLeather} Da";

                int nextArmCost = BlacksmithCraftingSystem.TIER1_GOLD_COST * (smith.ArmorTier + 1);
                int nextArmOre = BlacksmithCraftingSystem.TIER1_ORE_COST * (smith.ArmorTier + 1);
                int nextArmLeather = BlacksmithCraftingSystem.TIER1_LEATHER_COST * (smith.ArmorTier + 1);

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

        public void OnClick_StartBattle()
        {
            
    }
}

}
