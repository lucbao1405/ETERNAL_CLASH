using System;
using System.Collections;
using System.Collections.Generic;
using EternalClash.Data;
using EternalClash.Item;
using EternalClash.Player;
using EternalClash.Stage;
using EternalClash.UI;
using EternalClash.Village;
using EternalClash.Core.Save;
using UnityEngine;

namespace EternalClash.BattleResult
{
    /// <summary>Owns the battle-result flow: visual chest -> reward popup -> result popup.</summary>
    public sealed class BattleResultFlowController : MonoBehaviour
    {
        public static BattleResultFlowController Instance { get; private set; }

        [Header("Chest Reward Popup")]
        [SerializeField] private GameObject chestRewardPopup;
        [SerializeField] private ChestRewardUI chestRewardUI;
        [SerializeField] private ChestRewardController chestRewardController;

        [Header("Win / Lose Popups")]
        [SerializeField] private GameObject winPopup;
        [SerializeField] private GameObject losePopup;
        [SerializeField] private BattleResultUI winUI;
        [SerializeField] private BattleResultUI loseUI;

        [Header("Monetization")]
        [Tooltip("Hien bang 'xem quang cao de nhan doi thuong ruong' sau khi thang. " +
                 "Dang TAT: tinh nang nay chua chot, bat len thi moi tran thang deu " +
                 "hien quang cao de len popup ket qua.")]
        [SerializeField] private bool enableDoubleRewardOffer = false;

        [Header("Flow Timing")]
        [SerializeField, Min(0f)] private float victoryDelay = 2f;
        [SerializeField, Min(0f)] private float defeatDelay = 0f;
        [SerializeField, Min(0f)] private float popupFadeDuration = 0.25f;
        [SerializeField, Min(1f)] private float revealTimeout = 120f;
        [SerializeField, Min(1f)] private float resultClickTimeout = 600f;

        private readonly List<ItemReward> battleLoot = new List<ItemReward>();
        private readonly List<ItemReward> chestRewards = new List<ItemReward>();
        private RewardData pendingChestReward;
        private RewardType pendingDoubleType;
        private int pendingDoubleAmount;
        private bool flowActive;
        private bool battleFinished;
        private bool returnRequested;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveReferences();
        }

        private void OnEnable()
        {
            battleLoot.Clear();
            chestRewards.Clear();
            pendingChestReward = null;
            battleFinished = false;
            RewardsDoubled = false;
            ItemPickup.OnItemCollected += HandleItemCollected;
        }

        private void Start()
        {
            HideAllPopups();
        }

        private void OnDisable()
        {
            ItemPickup.OnItemCollected -= HandleItemCollected;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool StartWinFlow()
        {
            if (flowActive)
                return true;

            flowActive = true;
            returnRequested = false;
            StartCoroutine(RunWinFlow());
            return true;
        }

        public bool StartLoseFlow()
        {
            if (flowActive)
                return true;

            flowActive = true;
            returnRequested = false;
            StartCoroutine(RunLoseFlow());
            return true;
        }

        // Compatibility with the existing StageCompleteController integration.
        public bool StartVictoryFlow() => StartWinFlow();
        public bool StartDefeatFlow() => StartLoseFlow();

        public void OpenChestRewardPopup()
        {
            if (chestRewardPopup != null)
            {
                chestRewardPopup.SetActive(true);
                chestRewardPopup.transform.SetAsLastSibling();
            }
        }

        public void ShowWinPopup()
        {
            if (!flowActive)
            {
                flowActive = true;
                StartCoroutine(ShowWinPopupFlow());
            }
        }

        public void ShowLosePopup()
        {
            StartLoseFlow();
        }

        private IEnumerator RunWinFlow()
        {
            battleFinished = true;
            StopCombat();
            PlayResultSound(EternalClash.Audio.SoundId.Win);
            HideAllPopups();
            EternalClash.Monetization.OfferOverlayUI.Close();
            PrepareChestReward();

            if (victoryDelay > 0f)
                yield return new WaitForSecondsRealtime(victoryDelay);

            // Panel "Open Chest" sap hien: ruong the gioi (ChestSpawn da troi toi
            // ben player) bien mat de popup ruong tiep quan man mo ruong.
            StageCompleteController.DespawnWorldChest();

            ResolveChestRewardController();
            bool rewardCompleted = false;
            if (chestRewardController != null)
            {
                chestRewardController.BeginReward(chestRewards, () => rewardCompleted = true);
                while (!rewardCompleted)
                    yield return null;
            }

            // Offer X2 (monetization): chi cho thuong Gold/Gem/Material - Equipment
            // va Gift nhan doi se loi (them mon trang bi / hoa cuc). Phai luu lai
            // truoc GrantChestReward() vi no xoa pendingChestReward.
            bool canDouble = enableDoubleRewardOffer &&
                pendingChestReward != null && pendingChestReward.amount > 0 &&
                (pendingChestReward.type == RewardType.Gold ||
                 pendingChestReward.type == RewardType.Gem ||
                 pendingChestReward.type == RewardType.Material);
            RewardType doubleType = canDouble ? pendingChestReward.type : RewardType.Gold;
            int doubleAmount = canDouble ? pendingChestReward.amount : 0;

            GrantChestReward();

            // Offer X2 khong hien truoc popup nua: ShowWinPopupFlow se de no len
            // TREN popup thang vua mo voi dem nguoc 5s tu dong dong.
            pendingDoubleType = doubleType;
            pendingDoubleAmount = canDouble ? doubleAmount : 0;

            yield return ShowWinPopupFlow();
        }

        /// <summary>
        /// Offer xem quang cao de nhan them mot lan phan thuong ruong (X2). Hien
        /// TREN popup ket qua thang sau khi ruong da grant, tu dong dong sau 5
        /// giay khong chon. Neu nguoi choi xem thi so lieu tren popup duoc cap
        /// nhat theo (RefreshWinPopupAfterDouble).
        /// </summary>
        private IEnumerator ShowDoubleRewardOffer(RewardType rewardType, int amount)
        {
            bool success = false;
            bool resolved = false;
            Monetization.OfferOverlayUI.Show(
                "Double Your Reward!",
                "Watch an ad to receive the chest reward one more time.",
                "Watch Ad",
                onWatchClicked: () => Monetization.AdsService.ShowRewarded("x2_chest", watched =>
                {
                    success = watched;
                    resolved = true;
                }),
                onDeclined: () => resolved = true,
                autoDeclineSeconds: 5f);

            while (!resolved)
                yield return null;

            if (success)
            {
                GrantDoubleChestReward(rewardType, amount);
                RefreshWinPopupAfterDouble();
            }
        }

        private void GrantDoubleChestReward(RewardType rewardType, int amount)
        {
            switch (rewardType)
            {
                case RewardType.Gold:
                    GoldSystem.Instance?.AddGold(amount);
                    break;
                case RewardType.Gem:
                    GoldSystem.Instance?.AddGem(amount);
                    break;
                case RewardType.Material:
                    AddNormalItemReward(ItemCatalog.Find("Ore"), amount);
                    break;
            }

            Debug.Log($"[X2] Nhan doi thuong ruong: +{amount} {rewardType}");
        }

        /// <summary>
        /// Sau khi X2 duoc grant, cap nhat lai so lieu tren popup thang: entry
        /// cua ruong nhan doi (vat pham) va doc lai SessionGoldEarned (tien).
        /// Chi doi danh sach hien thi, khong chay lai animation thanh EXP.
        /// </summary>
        /// <summary>Tran nay da nhan doi vat pham chua (nut QCx2Item chi dung 1 lan).</summary>
        public bool RewardsDoubled { get; private set; }

        /// <summary>
        /// Nhan doi TOAN BO thu dang hien trong o Vat_Pham cua Win popup: cong
        /// them dung mot lan nua vao tui do / tien, roi cap nhat so luong tren
        /// popup. Goi sau khi nguoi choi xem xong quang cao X2.
        /// Tra ve false neu da nhan doi roi hoac khong co gi de nhan.
        /// </summary>
        public bool DoubleDisplayedRewards()
        {
            if (RewardsDoubled || winUI == null)
                return false;

            List<ItemReward> shown = BuildResultData(true).GetDisplayItems();
            if (shown == null || shown.Count == 0)
                return false;

            RewardsDoubled = true;

            foreach (ItemReward entry in shown)
            {
                if (entry == null || entry.item == null || entry.quantity <= 0)
                    continue;

                string itemId = entry.item.itemId ?? string.Empty;
                if (string.Equals(itemId, "Gold", StringComparison.OrdinalIgnoreCase))
                    GoldSystem.Instance?.AddGold(entry.quantity);
                else if (string.Equals(itemId, "Gem", StringComparison.OrdinalIgnoreCase))
                    GoldSystem.Instance?.AddGem(entry.quantity);
                else if (entry.item.equipmentSlot != EquipmentSlot.None ||
                    string.Equals(entry.item.itemType, "Equipment", StringComparison.OrdinalIgnoreCase))
                {
                    // Trang bi phai vao kho trang bi, khong phai tui material.
                    for (int i = 0; i < entry.quantity; i++)
                        EquipmentSystem.Instance?.AddToInventory(entry.item);
                    EquipmentSystem.Instance?.SyncEquipmentSave();
                    SaveCoordinator.RequestSave();
                }
                else
                    AddNormalItemReward(entry.item, entry.quantity);
            }

            // So hien tren popup: vang tu cap nhat theo SessionGoldEarned (AddGold
            // o tren da cong), cac item con lai nhan doi truc tiep trong danh sach.
            DoubleQuantities(battleLoot);
            DoubleQuantities(chestRewards);

            winUI.RefreshRewards(BuildResultData(true).GetDisplayItems());
            Debug.Log("[Ads] X2 vat pham: da nhan doi phan thuong tran nay.");
            return true;
        }

        private static void DoubleQuantities(List<ItemReward> list)
        {
            if (list == null)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].quantity > 0)
                    list[i].quantity *= 2;
            }
        }

        private void RefreshWinPopupAfterDouble()
        {
            if (winUI == null || winPopup == null || !winPopup.activeSelf)
                return;

            if (chestRewards.Count > 0 && chestRewards[0] != null)
                chestRewards[0].quantity *= 2;

            winUI.RefreshRewards(BuildResultData(true).GetDisplayItems());
        }

        private IEnumerator RunLoseFlow()
        {
            battleFinished = true;
            StopCombat();
            PlayResultSound(EternalClash.Audio.SoundId.Lose);
            MarkPlayerInjured();
            HideAllPopups();
            DestroyVisualChest();
            chestRewardController?.CancelReward();

            if (defeatDelay > 0f)
                yield return new WaitForSecondsRealtime(defeatDelay);

            BattleRewardData data = BuildResultData(false);
            if (losePopup != null && loseUI != null)
            {
                BattlePopupController.SetResultOverlay(true);
                losePopup.SetActive(true);
                losePopup.transform.SetAsLastSibling();
                yield return FadeIn(losePopup);

                bool clicked = false;
                loseUI.ShowDefeat(data.exp, data.battleTime, data.GetDisplayItems(), () => clicked = true);
                yield return WaitForResultClick(() => clicked);
                loseUI.Close();
                BattlePopupController.SetResultOverlay(false);
                ReturnToVillage();
                yield break;
            }

            if (!BattlePopupController.TryShowLose(ToStageResult(data)))
                ReturnToVillage();
            else
                EndFlow();
        }

        private IEnumerator PlayChestRewardPopup()
        {
            if (chestRewardPopup == null || chestRewardUI == null)
                yield break;

            OpenChestRewardPopup();
            yield return FadeIn(chestRewardPopup);

            bool completed = false;
            chestRewardUI.ShowRewards(chestRewards, () => completed = true);
            float elapsed = 0f;
            while (!completed && elapsed < revealTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            chestRewardUI.Hide();
        }

        private IEnumerator ShowWinPopupFlow()
        {
            BattleRewardData data = BuildResultData(true);
            if (winPopup != null && winUI != null)
            {
                BattlePopupController.SetResultOverlay(true);
                winPopup.SetActive(true);
                winPopup.transform.SetAsLastSibling();
                yield return FadeIn(winPopup);

                bool clicked = false;
                winUI.ShowVictory(data.exp, data.gold, data.battleTime, data.GetDisplayItems(), () => clicked = true);

                // Offer X2 xem quang cao nam tren popup thang: khong chon trong
                // 5 giay thi tu dong dong (OfferOverlayUI autoDecline).
                if (pendingDoubleAmount > 0)
                {
                    yield return ShowDoubleRewardOffer(pendingDoubleType, pendingDoubleAmount);
                    pendingDoubleAmount = 0;
                }

                yield return WaitForResultClick(() => clicked);
                winUI.Close();
                BattlePopupController.SetResultOverlay(false);
                ReturnToVillage();
                yield break;
            }

            if (!BattlePopupController.TryShowWin(ToStageResult(data)))
                ReturnToVillage();
            else
                EndFlow();
        }

        private IEnumerator WaitForResultClick(Func<bool> clicked)
        {
            float elapsed = 0f;
            while (!clicked() && elapsed < resultClickTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void PrepareChestReward()
        {
            chestRewards.Clear();
            pendingChestReward = RewardGenerator.GenerateStageReward(ResolveStageLevel());
            if (pendingChestReward == null)
                return;

            ItemReward entry = null;
            switch (pendingChestReward.type)
            {
                case RewardType.Gold:
                    entry = BattleRewardData.CreateEntry("Gold", "Gold", Mathf.Max(1, pendingChestReward.amount));
                    break;
                case RewardType.Gem:
                    entry = BattleRewardData.CreateEntry("Gem", "Gem", Mathf.Max(1, pendingChestReward.amount));
                    break;
                case RewardType.Material:
                    entry = BattleRewardData.CreateEntry("Ore", "Copper Ore", Mathf.Max(1, pendingChestReward.amount));
                    break;
                case RewardType.Equipment:
                    if (pendingChestReward.item != null)
                        entry = new ItemReward(pendingChestReward.item, Mathf.Max(1, pendingChestReward.amount));
                    break;
                case RewardType.Gift:
                    if (pendingChestReward.item != null)
                        entry = new ItemReward(pendingChestReward.item, Mathf.Max(1, pendingChestReward.amount));
                    break;
            }

            if (entry != null)
                chestRewards.Add(entry);
        }

        private void GrantChestReward()
        {
            if (pendingChestReward == null)
                return;

            GoldSystem currency = GoldSystem.Instance;
            switch (pendingChestReward.type)
            {
                case RewardType.Gold:
                    currency?.AddGold(pendingChestReward.amount);
                    EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.PickupCoin);
                    break;
                case RewardType.Gem:
                    currency?.AddGem(pendingChestReward.amount);
                    EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.PickupGem);
                    break;
                case RewardType.Material:
                    AddNormalItemReward(ItemCatalog.Find("Ore"), pendingChestReward.amount);
                    EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.PickupMaterial);
                    break;
                case RewardType.Equipment:
                    EquipIfUpgrade(pendingChestReward.item);
                    break;
                case RewardType.Gift:
                    AddNormalItemReward(pendingChestReward.item, pendingChestReward.amount);
                    break;
            }

            pendingChestReward = null;

            // Qua dam bao theo stage (man 3: 2 hoa de tang Ela), trao ke ca khi
            // ruong ra Gold/Gem/Equipment.
            foreach (RewardData guaranteed in RewardGenerator.GetGuaranteedGifts(ResolveStageLevel()))
            {
                if (guaranteed != null && guaranteed.amount > 0)
                    AddNormalItemReward(guaranteed.item, guaranteed.amount);
            }
        }

        private void EquipIfUpgrade(ItemData item)
        {
            if (item == null || EquipmentSystem.Instance == null)
                return;

            // Equipment is selected only in Town's Blacksmith. Battle rewards
            // always enter persistent inventory and never replace equipped gear.
            EquipmentSystem.Instance.AddToInventory(item);
            EquipmentSystem.Instance.SyncEquipmentSave();
            SaveCoordinator.RequestSave();
        }

        private void HandleItemCollected(ItemPickup pickup, int amount)
        {
            if (pickup == null || amount <= 0 || flowActive || battleFinished)
                return;

            ItemReward entry = null;
            switch (pickup.itemType)
            {
                case ItemType.Gold:
                    entry = BattleRewardData.CreateEntry("Gold", "Gold", amount);
                    break;
                case ItemType.Ore:
                    entry = BattleRewardData.CreateEntry("Ore", "Copper Ore", amount);
                    break;
                case ItemType.Leather:
                    entry = BattleRewardData.CreateEntry("Leather", "Wolf Hide", amount);
                    break;
                case ItemType.Wood:
                    entry = BattleRewardData.CreateEntry("Wood", "Wood", amount);
                    break;
                case ItemType.Gem:
                    entry = BattleRewardData.CreateEntry("Gem", "Gem", amount);
                    break;
                case ItemType.Flower:
                    entry = new ItemReward(ItemCatalog.Find(
                        string.IsNullOrEmpty(pickup.itemIdOverride) ? "blue_flower" : pickup.itemIdOverride), amount);
                    break;
            }

            if (entry != null)
                BattleRewardData.AddOrMerge(battleLoot, entry);
        }

        private static void AddNormalItemReward(ItemData rewardItem, int amount)
        {
            if (rewardItem == null || amount <= 0 || IsCurrencyReward(rewardItem))
                return;

            ItemData item = ItemCatalog.Find(rewardItem.itemId) ?? rewardItem;
            if (item == null || string.IsNullOrWhiteSpace(item.itemId) || IsCurrencyReward(item))
                return;

            SaveData data = SaveManager.Instance?.Data;
            if (data == null)
                return;

            data.inventory ??= new InventorySaveData();
            data.inventory.items ??= new List<ItemStackSaveData>();

            ItemStackSaveData stack = data.inventory.items.Find(value =>
                value != null && string.Equals(value.itemId, item.itemId, StringComparison.OrdinalIgnoreCase));
            if (stack == null)
            {
                stack = new ItemStackSaveData { itemId = item.itemId };
                data.inventory.items.Add(stack);
            }

            stack.amount += amount;
            SaveCoordinator.RequestSave();
        }

        private static bool IsCurrencyReward(ItemData item)
        {
            if (item == null)
                return true;

            if (string.Equals(item.itemType, "Currency", StringComparison.OrdinalIgnoreCase))
                return true;

            string itemId = item.itemId ?? string.Empty;
            return string.Equals(itemId, "gold", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(itemId, "coin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(itemId, "gem", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(itemId, "diamond", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(itemId, "diamon", StringComparison.OrdinalIgnoreCase);
        }

        private BattleRewardData BuildResultData(bool includeChest)
        {
            var data = new BattleRewardData
            {
                battleTime = GetBattleTime(),
                exp = PlayerStatSystem.Instance != null ? PlayerStatSystem.Instance.SessionExpEarned : 0,
                gold = GoldSystem.Instance != null ? GoldSystem.Instance.SessionGoldEarned : 0
            };

            for (int i = 0; i < battleLoot.Count; i++)
                BattleRewardData.AddOrMerge(data.battleLoot, battleLoot[i]);
            if (includeChest)
            {
                for (int i = 0; i < chestRewards.Count; i++)
                    BattleRewardData.AddOrMerge(data.chestReward, chestRewards[i]);

                // Vang cua ruong: GrantChestReward() -> AddGold() da cong vao
                // SessionGoldEarned roi. Chi cong them khi ruong chua duoc trao
                // (pendingChestReward con), neu khong se bi tinh 2 lan.
                if (pendingChestReward != null && pendingChestReward.type == RewardType.Gold)
                    data.gold += Mathf.Max(0, pendingChestReward.amount);
            }

            return data;
        }

        private int ResolveStageLevel()
        {
            if (StageManager.Instance != null)
                return Mathf.Clamp(StageManager.Instance.CurrentStageLevel, 1, StageManager.MaxKnownStageLevel);
            var saveData = EternalClash.Core.Save.SaveManager.Instance?.Data;
            return saveData != null ? Mathf.Clamp(saveData.stageLevel, 1, StageManager.MaxKnownStageLevel) : 1;
        }

        private float GetBattleTime()
        {
            if (StageManager.Instance != null)
                return StageManager.Instance.GetBattleTime();
            StageProgressController progress = FindObjectOfType<StageProgressController>();
            return progress != null ? progress.GetStageTime() : 0f;
        }

        private void ResolveReferences()
        {
            if (chestRewardUI == null && chestRewardPopup != null)
                chestRewardUI = chestRewardPopup.GetComponentInChildren<ChestRewardUI>(true);
            if (winUI == null && winPopup != null)
                winUI = winPopup.GetComponentInChildren<BattleResultUI>(true);
            if (loseUI == null && losePopup != null)
                loseUI = losePopup.GetComponentInChildren<BattleResultUI>(true);
            ResolveChestRewardController();
        }

        private void ResolveChestRewardController()
        {
            if (chestRewardController == null)
                chestRewardController = GetComponent<ChestRewardController>();
            if (chestRewardController == null)
                chestRewardController = gameObject.AddComponent<ChestRewardController>();
            chestRewardController.Configure(null, null, chestRewardPopup);
        }

        private void DestroyVisualChest() { }

        private void StopCombat()
        {
            StageCompleteController.Instance?.StopCombat();
        }

        private void MarkPlayerInjured()
        {
            StageCompleteController.Instance?.MarkPlayerInjured();
        }

        /// <summary>Tat nhac tran dau va phat nhac thang / thua.</summary>
        private static void PlayResultSound(EternalClash.Audio.SoundId id)
        {
            EternalClash.Core.AudioController.Instance?.StopMusic();
            EternalClash.Audio.GameAudio.Play(id);
        }

        private void HideAllPopups()
        {
            if (chestRewardPopup != null)
                chestRewardPopup.SetActive(false);
            if (winPopup != null)
                winPopup.SetActive(false);
            if (losePopup != null)
                losePopup.SetActive(false);
        }

        private IEnumerator FadeIn(GameObject popup)
        {
            CanvasGroup group = popup.GetComponent<CanvasGroup>();
            if (group == null)
                group = popup.AddComponent<CanvasGroup>();

            group.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < popupFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, popupFadeDuration));
                yield return null;
            }
            group.alpha = 1f;
        }

        private static StageResultData ToStageResult(BattleRewardData data)
        {
            return new StageResultData
            {
                stageTime = data.battleTime,
                earnedExp = data.exp,
                earnedGold = data.gold,
                rewards = new List<RewardData>()
            };
        }

        private void ReturnToVillage()
        {
            if (returnRequested)
                return;

            returnRequested = true;
            if (StageCompleteController.Instance != null)
                StageCompleteController.Instance.ReturnToVillage();
            else
                EternalClash.Core.SceneLoader.LoadTown();
            EndFlow();
        }

        private void EndFlow()
        {
            flowActive = false;
            battleFinished = false;
        }
    }
}
