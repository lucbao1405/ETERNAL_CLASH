using System;
using System.Collections;
using System.Collections.Generic;
using EternalClash.Data;
using EternalClash.Item;
using EternalClash.Player;
using EternalClash.Stage;
using EternalClash.UI;
using EternalClash.Village;
using UnityEngine;

namespace EternalClash.BattleResult
{
    /// <summary>Owns the battle-result flow: visual chest -> reward popup -> result popup.</summary>
    public sealed class BattleResultFlowController : MonoBehaviour
    {
        public static BattleResultFlowController Instance { get; private set; }

        [Header("Battle Chest Transition")]
        [SerializeField] private GameObject chestPrefab;
        [SerializeField] private Transform chestSpawnPoint;
        [SerializeField, Min(0.01f)] private float chestTravelDuration = 0.8f;

        [Header("Chest Reward Popup")]
        [SerializeField] private GameObject chestRewardPopup;
        [SerializeField] private ChestRewardUI chestRewardUI;
        [SerializeField] private ChestRewardController chestRewardController;

        [Header("Win / Lose Popups")]
        [SerializeField] private GameObject winPopup;
        [SerializeField] private GameObject losePopup;
        [SerializeField] private BattleResultUI winUI;
        [SerializeField] private BattleResultUI loseUI;

        [Header("Flow Timing")]
        [SerializeField, Min(0f)] private float victoryDelay = 2f;
        [SerializeField, Min(0f)] private float defeatDelay = 0f;
        [SerializeField, Min(0f)] private float popupFadeDuration = 0.25f;
        [SerializeField, Min(1f)] private float revealTimeout = 120f;
        [SerializeField, Min(1f)] private float resultClickTimeout = 600f;

        private readonly List<ItemReward> battleLoot = new List<ItemReward>();
        private readonly List<ItemReward> chestRewards = new List<ItemReward>();
        private RewardData pendingChestReward;
        private GameObject visualChest;
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

        public void SpawnChest()
        {
            ResolveChestRewardController();
            chestRewardController?.SpawnChest();
        }

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
            HideAllPopups();
            PrepareChestReward();

            if (victoryDelay > 0f)
                yield return new WaitForSecondsRealtime(victoryDelay);

            ResolveChestRewardController();
            bool rewardCompleted = false;
            if (chestRewardController != null)
            {
                chestRewardController.BeginReward(chestRewards, () => rewardCompleted = true);
                while (!rewardCompleted)
                    yield return null;
            }
            GrantChestReward();
            yield return ShowWinPopupFlow();
        }

        private IEnumerator RunLoseFlow()
        {
            battleFinished = true;
            StopCombat();
            MarkPlayerInjured();
            HideAllPopups();
            DestroyVisualChest();
            chestRewardController?.CancelReward();

            if (defeatDelay > 0f)
                yield return new WaitForSecondsRealtime(defeatDelay);

            BattleRewardData data = BuildResultData(false);
            if (losePopup != null && loseUI != null)
            {
                losePopup.SetActive(true);
                losePopup.transform.SetAsLastSibling();
                yield return FadeIn(losePopup);

                bool clicked = false;
                loseUI.ShowDefeat(data.exp, data.battleTime, data.battleLoot, () => clicked = true);
                yield return WaitForResultClick(() => clicked);
                loseUI.Close();
                ReturnToVillage();
                yield break;
            }

            if (!BattlePopupController.TryShowLose(ToStageResult(data)))
                ReturnToVillage();
            else
                EndFlow();
        }

        private IEnumerator MoveVisualChestToCenter()
        {
            if (visualChest == null)
                yield break;

            Vector3 start = visualChest.transform.position;
            Vector3 target = GetScreenCenterPosition(start.z);
            float elapsed = 0f;
            while (elapsed < chestTravelDuration && visualChest != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / chestTravelDuration));
                visualChest.transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }

            if (visualChest != null)
                visualChest.transform.position = target;
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
                winPopup.SetActive(true);
                winPopup.transform.SetAsLastSibling();
                yield return FadeIn(winPopup);

                bool clicked = false;
                winUI.ShowVictory(data.exp, data.gold, data.battleTime, data.GetWinDisplayItems(), () => clicked = true);
                yield return WaitForResultClick(() => clicked);
                winUI.Close();
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
                    break;
                case RewardType.Gem:
                    currency?.AddGem(pendingChestReward.amount);
                    break;
                case RewardType.Material:
                    currency?.AddMaterials(pendingChestReward.amount, 0);
                    break;
                case RewardType.Equipment:
                    EquipIfUpgrade(pendingChestReward.item);
                    break;
            }

            pendingChestReward = null;
        }

        private void EquipIfUpgrade(ItemData item)
        {
            if (item == null || EquipmentSystem.Instance == null)
                return;

            var saveData = EternalClash.Core.Save.SaveManager.Instance?.Data;
            bool improves = saveData == null;
            if (saveData != null && item.weaponTier > 0)
                improves = item.weaponTier > saveData.weaponTier;
            else if (saveData != null && item.armorTier > 0)
                improves = item.armorTier > saveData.armorTier;

            if (improves)
                EquipmentSystem.Instance.EquipItem(item);
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
            }

            if (entry != null)
                BattleRewardData.AddOrMerge(battleLoot, entry);
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
                {
                    BattleRewardData.AddOrMerge(data.chestReward, chestRewards[i]);
                    if (chestRewards[i] != null && chestRewards[i].item != null &&
                        string.Equals(chestRewards[i].item.itemId, "Gold", StringComparison.OrdinalIgnoreCase))
                    {
                        data.gold += Mathf.Max(0, chestRewards[i].quantity);
                    }
                }
            }

            return data;
        }

        private int ResolveStageLevel()
        {
            if (StageManager.Instance != null)
                return Mathf.Clamp(StageManager.Instance.CurrentStageLevel, 1, 5);
            var saveData = EternalClash.Core.Save.SaveManager.Instance?.Data;
            return saveData != null ? Mathf.Clamp(saveData.stageLevel, 1, 5) : 1;
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
            chestRewardController.Configure(chestPrefab, chestSpawnPoint, chestRewardPopup);
        }

        private Vector3 GetRightScreenPosition()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return Vector3.right * 10f;
            float z = 0f;
            float distance = Mathf.Abs(camera.transform.position.z - z);
            Vector3 point = camera.ViewportToWorldPoint(new Vector3(1.15f, 0.5f, distance));
            point.z = z;
            return point;
        }

        private Vector3 GetScreenCenterPosition(float z)
        {
            Camera camera = Camera.main;
            if (camera == null)
                return new Vector3(0f, 0f, z);
            Vector3 point = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, Mathf.Abs(camera.transform.position.z - z)));
            point.z = z;
            return point;
        }

        private void DestroyVisualChest()
        {
            if (visualChest != null)
                Destroy(visualChest);
            visualChest = null;
        }

        private void StopCombat()
        {
            StageCompleteController.Instance?.StopCombat();
        }

        private void MarkPlayerInjured()
        {
            StageCompleteController.Instance?.MarkPlayerInjured();
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
