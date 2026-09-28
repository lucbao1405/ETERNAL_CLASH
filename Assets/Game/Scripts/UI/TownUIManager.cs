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

        // Delegate duoc giu trong field de OnDestroy huy dung delegate da sub,
        // tranh leak handler khi GoldSystem/PlayerConditionSystem (DontDestroyOnLoad)
        // ton tai dai hon UI cua scene Town.
        private bool subscribed;
        private PlayerConditionSystem subscribedCondition;
        private System.Action<int> onGoldChanged;
        private System.Action<int, int> onMaterialsChanged;
        private System.Action<int> onAffinityChanged;
        private System.Action<EternalClash.Core.PlayerCondition> onConditionChanged;
        private System.Action<int, int> onRecoveredHpChanged;

        private void Start()
        {
            ShowMainHall();
            RefreshAllUI();
            ShowEquipmentGuideIfNeeded();

            onGoldChanged = _ => RefreshAllUI();
            onMaterialsChanged = (_, _) => RefreshAllUI();
            onAffinityChanged = _ => RefreshAllUI();
            onConditionChanged = _ => RefreshBattleGate();
            onRecoveredHpChanged = (_, _) => RefreshBattleGate();

            TrySubscribeSystems();
        }

        private void Update()
        {
            // Cac system khong Awake cung luc voi UI; retry den khi sub du thi dung.
            if (!subscribed)
                TrySubscribeSystems();
        }

        private void TrySubscribeSystems()
        {
            if (subscribed) return;

            if (PlayerStatSystem.Instance != null && !statsSubscribed)
            {
                PlayerStatSystem.Instance.OnStatsChanged += RefreshAllUI;
                statsSubscribed = true;
            }
            else if (PlayerStatSystem.Instance == null)
                return;

            if (GoldSystem.Instance != null && !goldSubscribed)
            {
                GoldSystem.Instance.OnGoldChanged += onGoldChanged;
                GoldSystem.Instance.OnMaterialsChanged += onMaterialsChanged;
                goldSubscribed = true;
            }
            else if (GoldSystem.Instance == null)
                return;

            if (AffinityManager.Instance != null && !affinitySubscribed)
            {
                AffinityManager.Instance.OnAffinityPointsChanged += onAffinityChanged;
                affinitySubscribed = true;
            }
            else if (AffinityManager.Instance == null)
                return;

            if (PlayerConditionSystem.Instance != null && subscribedCondition == null)
            {
                subscribedCondition = PlayerConditionSystem.Instance;
                subscribedCondition.OnConditionChanged += onConditionChanged;
                subscribedCondition.OnRecoveredHpChanged += onRecoveredHpChanged;
                subscribedCondition.OnRecoveryCompleted += RefreshBattleGate;
                subscribedCondition.BeginRecoveryIfNeeded();
                RefreshBattleGate();
            }
            else if (PlayerConditionSystem.Instance == null)
                return;

            subscribed = true;
        }

        private bool statsSubscribed;
        private bool goldSubscribed;
        private bool affinitySubscribed;

        private void OnDestroy()
        {
            if (PlayerStatSystem.Instance != null && statsSubscribed)
                PlayerStatSystem.Instance.OnStatsChanged -= RefreshAllUI;

            if (GoldSystem.Instance != null && goldSubscribed)
            {
                GoldSystem.Instance.OnGoldChanged -= onGoldChanged;
                GoldSystem.Instance.OnMaterialsChanged -= onMaterialsChanged;
            }

            if (AffinityManager.Instance != null && affinitySubscribed)
                AffinityManager.Instance.OnAffinityPointsChanged -= onAffinityChanged;

            if (subscribedCondition != null)
            {
                subscribedCondition.OnConditionChanged -= onConditionChanged;
                subscribedCondition.OnRecoveredHpChanged -= onRecoveredHpChanged;
                subscribedCondition.OnRecoveryCompleted -= RefreshBattleGate;
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

                if (strText != null) strText.text = $"STR: {stats.BaseStrength} (+{stats.BaseStrength * 2} DMG)";
                if (intText != null) intText.text = $"INT: {stats.BaseIntelligence} (+{stats.BaseIntelligence * 5}% EXP)";
                if (vitText != null) vitText.text = $"VIT: {stats.BaseVitality} (+{stats.BaseVitality * 15} HP)";
                if (luckText != null) luckText.text = $"LUCK: {stats.BaseLuck} (+{(stats.BaseLuck * 0.5f):F1}% Crit)";

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
        public void OnClick_AddStrength() { EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick); PlayerStatSystem.Instance?.AllocateStrength(); }
        public void OnClick_AddIntelligence() { EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick); PlayerStatSystem.Instance?.AllocateIntelligence(); }
        public void OnClick_AddVitality() { EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick); PlayerStatSystem.Instance?.AllocateVitality(); }
        public void OnClick_AddLuck() { EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick); PlayerStatSystem.Instance?.AllocateLuck(); }
        public void OnClick_ResetStats() { EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick); PlayerStatSystem.Instance?.ResetStats(); }

        public void OnClick_UpgradeWeapon() { EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick); BlacksmithCraftingSystem.Instance?.UpgradeWeapon(); }
        public void OnClick_UpgradeArmor() { EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick); BlacksmithCraftingSystem.Instance?.UpgradeArmor(); }

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
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);

            // Nut danh man thuong: huy thu thach boss neu con ton tai tu tran boss truoc.
            EternalClash.Enemy.BossChallenge.Cancel();

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
                // Tutorial dang khoa nua thi khong ghi de lock cua tutorial.
                if (!Tutorial.TutorialManager.IsBattleLockedByTutorial)
                {
                    bool canBattle = condition == null || condition.CanStartBattle();
                    startBattleButton.interactable = canBattle;
                }
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
            // Unity 2022.2+ doi ten font built-in: Arial.ttf con lai se throw.
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "New equipment found. Visit the Blacksmith to equip it.";
        }
}

}
