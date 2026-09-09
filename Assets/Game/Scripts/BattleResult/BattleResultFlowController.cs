using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EternalClash.Chest;
using EternalClash.Data;
using EternalClash.Item;
using EternalClash.Player;
using EternalClash.Stage;
using EternalClash.UI;
using EternalClash.Village;

namespace EternalClash.BattleResult
{
    /// <summary>
    /// Quản lý toàn bộ flow kết thúc trận (Postknight style).
    ///
    /// WIN :
    ///   StageManager.CompleteStage() -> StageCompleteController delegate sang
    ///   StartVictoryFlow():
    ///     - Delay 2s
    ///     - Spawn chest ở ngoài mép phải màn hình
    ///     - Chest bay/trôi vào vị trí mở (dùng ChestController có sẵn, KHÔNG tạo animation mới)
    ///     - Mở chest (OpenChest) -> chờ animation hoàn tất (OnOpened)
    ///     - Trao reward rương (grant 1 lần, không mất reward)
    ///     - Fade vào Chest Reward Popup -> reveal từng item (ChestRewardUI)
    ///     - Hết item -> CompleteChestReward() -> Hide popup -> Show Win Popup
    ///
    /// LOSE :
    ///   StageManager.FailStage() -> StageCompleteController delegate sang
    ///   StartDefeatFlow():
    ///     - Bỏ qua chest
    ///     - Mark injured
    ///     - Show Lose Popup (defeat banner, time survived, EXP, item đã nhặt)
    ///
    /// Data: BattleRewardData (battleLoot + chestReward + gold + exp + battleTime).
    /// Trước Win Popup, battleLoot và chestReward được gộp & loại duplicate.
    ///
    /// Inspector (user tự gán):
    ///   - chest / chestPrefab / chestSpawnPoint
    ///   - chestRewardPopup (+ ChestRewardUI)
    ///   - winPopup  (+ BattleResultUI winUI)
    ///   - losePopup (+ BattleResultUI loseUI)
    ///
    /// Nếu popup/script chưa được gán, flow vẫn trao reward và tự fallback sang
    /// BattlePopupController cũ để không bị kẹt màn hình.
    /// </summary>
    public class BattleResultFlowController : MonoBehaviour
    {
        public static BattleResultFlowController Instance { get; private set; }

        [Header("World Chest (tự gán Inspector)")]
        [SerializeField] private ChestController chest;
        [SerializeField] private GameObject chestPrefab;
        [SerializeField] private Transform chestSpawnPoint;

        [Header("Chest Reward Popup")]
        [SerializeField] private GameObject chestRewardPopup;
        [SerializeField] private ChestRewardUI chestRewardUI;

        [Header("Win / Lose Popups")]
        [SerializeField] private GameObject winPopup;
        [SerializeField] private GameObject losePopup;
        [SerializeField] private BattleResultUI winUI;
        [SerializeField] private BattleResultUI loseUI;

        [Header("Flow Timing")]
        [SerializeField] private float victoryDelay = 2f;
        [SerializeField] private float defeatDelay = 0.8f;
        [SerializeField] private float chestArriveTimeout = 15f;
        [SerializeField] private float openToPopupGap = 0.5f;
        [SerializeField] private float popupFadeDuration = 0.35f;
        [SerializeField] private float revealTimeout = 120f;
        [SerializeField] private float resultClickTimeout = 600f;

        private readonly List<ItemReward> battleLoot = new List<ItemReward>();
        private readonly List<ItemReward> chestReward = new List<ItemReward>();
        private readonly Dictionary<GameObject, Vector3> savedPopupPositions =
            new Dictionary<GameObject, Vector3>();
        private RewardData pendingChestReward;
        private bool flowActive;
        private bool battleFinished;
        private bool returnRequested;
        private ChestController activeChest;
        private Coroutine activeFlow;

        // ------------------------------------------------------------------
        // Singleton + lifecycle
        // ------------------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ResolveUiScripts();
            CachePopupPositions();
        }

        /// <summary>
        /// Luu vi tri goc cua cac popup. BattlePopupController (he thong cu) tu dong
        /// tao luc scene load va day cac popup ten giong ra khoi man hinh, nen chung
        /// ta phai nho vi tri ban dau de phuc hoi truoc khi show popup cua minh.
        /// </summary>
        private void CachePopupPositions()
        {
            CachePosition(chestRewardPopup);
            CachePosition(winPopup);
            CachePosition(losePopup);
        }

        private void CachePosition(GameObject popup)
        {
            if (popup == null) return;
            if (popup.TryGetComponent(out RectTransform rect))
                savedPopupPositions[popup] = rect.position;
        }

        private void RestorePopupPosition(GameObject popup)
        {
            if (popup == null) return;
            if (!savedPopupPositions.TryGetValue(popup, out Vector3 saved))
                return;
            if (popup.TryGetComponent(out RectTransform rect))
                rect.position = saved;
        }

        private void OnEnable()
        {
            battleLoot.Clear();
            chestReward.Clear();
            pendingChestReward = null;
            battleFinished = false;
            ItemPickup.OnItemCollected += HandleItemCollected;
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

        private void Start()
        {
            // Rương trong scene (nếu user gán) phải nằm im tới khi flow thắng; nếu nó
            // active từ đầu trận thì ChestController sẽ tự bay vào giữa màn hình sớm.
            if (chest != null &&
                chest.gameObject != null &&
                chest.gameObject.scene.IsValid() &&
                chest.gameObject.activeInHierarchy)
            {
                chest.gameObject.SetActive(false);
            }

            // Controller này tiếp quản các popup kết quả, nên che/giấu chúng ngay khi
            // scene nạp (BattlePopupController sẽ bỏ qua chúng khi có controller này).
            HidePopup(chestRewardPopup);
            HidePopup(winPopup);
            HidePopup(losePopup);
        }

        private void HidePopup(GameObject popup)
        {
            if (popup == null) return;
            if (!popup.scene.IsValid()) return;
            if (popup.activeSelf)
                popup.SetActive(false);
        }

        private void ResolveUiScripts()
        {
            if (chestRewardUI == null && chestRewardPopup != null)
                chestRewardUI = chestRewardPopup.GetComponentInChildren<ChestRewardUI>(true);
            if (winUI == null && winPopup != null)
                winUI = winPopup.GetComponentInChildren<BattleResultUI>(true);
            if (loseUI == null && losePopup != null)
                loseUI = losePopup.GetComponentInChildren<BattleResultUI>(true);
        }

        // ------------------------------------------------------------------
        // Battle loot tracking (hook từ ItemPickup)
        // ------------------------------------------------------------------

        private void HandleItemCollected(ItemPickup pickup, int amount)
        {
            if (pickup == null || amount <= 0) return;
            if (flowActive || battleFinished) return;

            ItemReward entry = null;
            switch (pickup.itemType)
            {
                case ItemType.Ore:
                    entry = BattleRewardData.CreateEntry("Ore", "Ore", amount);
                    break;
                case ItemType.Leather:
                    entry = BattleRewardData.CreateEntry("Leather", "Leather", amount);
                    break;
                case ItemType.Wood:
                    entry = BattleRewardData.CreateEntry("Wood", "Wood", amount);
                    break;
            }

            if (entry != null)
                BattleRewardData.AddOrMerge(battleLoot, entry);
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>Trả true nếu manager đã tiếp quản flow thắng.</summary>
        public bool StartVictoryFlow()
        {
            if (flowActive) return true;
            returnRequested = false;
            flowActive = true;
            activeFlow = StartCoroutine(RunVictoryFlow());
            return true;
        }

        /// <summary>Trả true nếu manager đã tiếp quản flow thua.</summary>
        public bool StartDefeatFlow()
        {
            if (flowActive) return true;
            returnRequested = false;
            flowActive = true;
            activeFlow = StartCoroutine(RunDefeatFlow());
            return true;
        }

        /// <summary>Bắt đầu đoạn chest (spawn -> bay -> mở). Dùng cho test/manual trigger.</summary>
        public void StartChestSequence()
        {
            if (flowActive) return;
            flowActive = true;
            activeFlow = StartCoroutine(RunChestSequenceOnly());
        }

        /// <summary>Mở chest đang có và tiếp tục reveal reward. Dùng cho test/manual.</summary>
        public void OpenChestReward()
        {
            if (flowActive) return;
            flowActive = true;
            activeFlow = StartCoroutine(RunOpenAndReveal());
        }

        /// <summary>Đóng popup rương -> lưu reward -> hiện Win Popup.</summary>
        public void CompleteChestReward()
        {
            if (chestRewardPopup != null)
                chestRewardPopup.SetActive(false);
            StartCoroutine(ShowWinPopupFlow());
        }

        public void ShowWinPopup()
        {
            if (flowActive && activeFlow != null) return;
            activeFlow = StartCoroutine(ShowWinPopupFlow());
        }

        public void ShowLosePopup()
        {
            if (flowActive && activeFlow != null) return;
            flowActive = true;
            activeFlow = StartCoroutine(RunDefeatFlow());
        }

        // ------------------------------------------------------------------
        // VICTORY FLOW
        // ------------------------------------------------------------------

        private IEnumerator RunVictoryFlow()
        {
            battleFinished = true;

            StopCombat();
            PrepareChestReward();
            HideAllResultPopups();

            yield return new WaitForSecondsRealtime(victoryDelay);

            yield return StartCoroutine(PlayChestSequence());

            yield return new WaitForSecondsRealtime(openToPopupGap);

            // Trao reward rương trước khi popup (không bao giờ mất reward).
            GrantChestReward();

            yield return StartCoroutine(PlayChestRewardPopup());

            // Hết item -> đóng popup -> Win Popup
            yield return StartCoroutine(ShowWinPopupFlow());
        }

        private IEnumerator RunChestSequenceOnly()
        {
            battleFinished = true;
            StopCombat();
            PrepareChestReward();
            HideAllResultPopups();
            yield return StartCoroutine(PlayChestSequence());
            yield return new WaitForSecondsRealtime(openToPopupGap);
            GrantChestReward();
            yield return StartCoroutine(PlayChestRewardPopup());
            yield return StartCoroutine(ShowWinPopupFlow());
        }

        private IEnumerator RunOpenAndReveal()
        {
            battleFinished = true;
            if (pendingChestReward == null && chestReward.Count == 0)
                PrepareChestReward();

            if (activeChest != null)
                yield return StartCoroutine(OpenChestAndWait(activeChest));
            GrantChestReward();
            yield return StartCoroutine(PlayChestRewardPopup());
            yield return StartCoroutine(ShowWinPopupFlow());
        }

        private void PrepareChestReward()
        {
            chestReward.Clear();
            pendingChestReward = RewardGenerator.GenerateStageReward(ResolveStageLevel());
            if (pendingChestReward == null)
                return;

            switch (pendingChestReward.type)
            {
                case RewardType.Gold:
                    chestReward.Add(BattleRewardData.CreateEntry("Gold", "Gold", Mathf.Max(1, pendingChestReward.amount)));
                    break;
                case RewardType.Gem:
                    chestReward.Add(BattleRewardData.CreateEntry("Gem", "Gem", Mathf.Max(1, pendingChestReward.amount)));
                    break;
                case RewardType.Material:
                    chestReward.Add(BattleRewardData.CreateEntry("Ore", "Ore", Mathf.Max(1, pendingChestReward.amount)));
                    break;
                case RewardType.Equipment:
                    if (pendingChestReward.item != null)
                        chestReward.Add(new ItemReward(pendingChestReward.item, Mathf.Max(1, pendingChestReward.amount)));
                    break;
            }
        }

        private int ResolveStageLevel()
        {
            if (StageManager.Instance != null)
                return Mathf.Clamp(StageManager.Instance.CurrentStageLevel, 1, 5);

            if (EternalClash.Core.Save.SaveManager.Instance?.Data != null)
                return Mathf.Clamp(EternalClash.Core.Save.SaveManager.Instance.Data.stageLevel, 1, 5);

            return 1;
        }

        private void GrantChestReward()
        {
            if (pendingChestReward == null)
                return;

            var gold = GoldSystem.Instance;
            switch (pendingChestReward.type)
            {
                case RewardType.Gold:
                    gold?.AddGold(pendingChestReward.amount);
                    break;
                case RewardType.Gem:
                    gold?.AddGem(pendingChestReward.amount);
                    break;
                case RewardType.Material:
                    gold?.AddMaterials(pendingChestReward.amount, 0);
                    break;
                case RewardType.Equipment:
                    EquipChestEquipment(pendingChestReward.item);
                    break;
            }

            pendingChestReward = null;
        }

        /// <summary>
        /// Trang bị chỉ khi cao cấp hơn (hoặc chưa có) để không hạ cấp vũ khí/giáp
        /// đang mặc. Đọc từ SaveManager (chỉ đọc, không sửa hệ thống).
        /// </summary>
        private void EquipChestEquipment(ItemData item)
        {
            if (item == null || EquipmentSystem.Instance == null)
                return;

            var data = EternalClash.Core.Save.SaveManager.Instance?.Data;
            bool improves = false;

            if (item.weaponTier > 0)
            {
                int current = data != null ? data.weaponTier : 0;
                improves = item.weaponTier > current;
            }
            else if (item.armorTier > 0)
            {
                int current = data != null ? data.armorTier : 0;
                improves = item.armorTier > current;
            }

            if (improves || data == null)
                EquipmentSystem.Instance.EquipItem(item);
        }

        private IEnumerator PlayChestSequence()
        {
            activeChest = SpawnChest();
            if (activeChest == null)
                yield break;

            activeChest.SetRewardData(pendingChestReward);

            // Chest tự bay/trôi vào vị trí (ChestController.Update). Chờ đến nơi.
            float elapsed = 0f;
            while (activeChest != null &&
                   activeChest.gameObject != null &&
                   !activeChest.IsReadyForInteraction &&
                   elapsed < chestArriveTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (activeChest == null || activeChest.gameObject == null)
                yield break;

            yield return StartCoroutine(OpenChestAndWait(activeChest));

            // Giữ chest một lúc để nhìn thấy trạng thái mở rồi xoá khỏi map.
            yield return new WaitForSecondsRealtime(0.4f);
            if (activeChest != null && activeChest.gameObject != null)
                Destroy(activeChest.gameObject);
            activeChest = null;
        }

        private IEnumerator OpenChestAndWait(ChestController targetChest)
        {
            if (targetChest == null || targetChest.State != ChestState.Closed)
                yield break;

            // Đảm bảo có effect mở chest (overlay/fade) mà không sửa code animation.
            ChestOpenEffectController.EnsureInstance();

            bool opened = false;
            System.Action handleOpened = () => opened = true;
            targetChest.OnOpened += handleOpened;

            targetChest.OpenChest();

            float elapsed = 0f;
            while (!opened && elapsed < 20f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            targetChest.OnOpened -= handleOpened;
        }

        // ------------------------------------------------------------------
        // Chest spawn (dùng chest/ChestController có sẵn, KHÔNG tạo animation)
        // ------------------------------------------------------------------

        private ChestController SpawnChest()
        {
            GameObject source = null;
            if (chestPrefab != null)
                source = chestPrefab;
            else if (chest != null)
                source = chest.gameObject;

            if (source == null)
            {
                Debug.LogWarning("[BattleResult] Chưa gán chestPrefab hoặc chest trong Inspector.");
                return null;
            }

            Vector3 spawnPos = chestSpawnPoint != null
                ? chestSpawnPoint.position
                : ComputeRightEdgePosition();

            // Luôn instantiate bản mới tại vị trí spawn (có scene hay prefab đều được)
            // để ChestController.Awake nắm đúng startPosition cho quỹ đạo bay vào.
            if (source.scene.IsValid())
            {
                // Nếu là object trong scene, giấu bản gốc đi để không bị trùng.
                source.SetActive(false);
            }

            GameObject instance = Instantiate(source, spawnPos, Quaternion.identity);
            ChestController controller = instance != null
                ? instance.GetComponent<ChestController>()
                : null;
            if (controller == null && instance != null)
                controller = instance.GetComponentInChildren<ChestController>();

            return controller;
        }

        private Vector3 ComputeRightEdgePosition()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 world = cam.ViewportToWorldPoint(new Vector3(1.25f, 0.5f, 10f));
                world.z = 0f;
                return world;
            }
            return Vector3.zero;
        }

        // ------------------------------------------------------------------
        // Chest Reward Popup
        // ------------------------------------------------------------------

        private IEnumerator PlayChestRewardPopup()
        {
            if (chestRewardUI != null)
            {
                RestorePopupPosition(chestRewardPopup);
                if (chestRewardPopup != null)
                {
                    chestRewardPopup.SetActive(true);
                    yield return StartCoroutine(FadeInRoutine(GetOrCreateGroup(chestRewardPopup), popupFadeDuration));
                }

                bool done = false;
                Action onFinished = () => done = true;
                chestRewardUI.ShowRewards(chestReward, onFinished);

                float elapsed = 0f;
                while (!done && elapsed < revealTimeout)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            else
            {
                // Không có ChestRewardUI -> giữ popup (nếu có) một chút cho đỡ hụt hẫng.
                if (chestRewardPopup != null)
                {
                    RestorePopupPosition(chestRewardPopup);
                    chestRewardPopup.SetActive(true);
                    yield return StartCoroutine(FadeInRoutine(GetOrCreateGroup(chestRewardPopup), popupFadeDuration));
                    yield return new WaitForSecondsRealtime(0.4f);
                }
            }

            if (chestRewardPopup != null)
                chestRewardPopup.SetActive(false);
        }

        // ------------------------------------------------------------------
        // Win Popup
        // ------------------------------------------------------------------

        private IEnumerator ShowWinPopupFlow()
        {
            BattleRewardData data = BuildResultData(includeChest: true);

            if (winUI != null && winPopup != null)
            {
                RestorePopupPosition(winPopup);
                if (winPopup != null)
                {
                    winPopup.SetActive(true);
                    yield return StartCoroutine(FadeInRoutine(GetOrCreateGroup(winPopup), popupFadeDuration));
                }

                bool clicked = false;
                winUI.ShowVictory(data.exp, data.gold, data.battleTime, data.GetWinDisplayItems(), () => clicked = true);

                float elapsed = 0f;
                while (!clicked && elapsed < resultClickTimeout)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (winUI != null)
                    winUI.Close();
                ReturnToVillage();
                yield break;
            }

            // Fallback: popup cũ (BattlePopupController) tự dựng, nút Return tự thoát.
            bool shown = BattlePopupController.TryShowWin(new StageResultData
            {
                stageTime = data.battleTime,
                earnedExp = data.exp,
                earnedGold = data.gold,
                rewards = new List<RewardData>()
            });

            if (!shown)
                ReturnToVillage();

            EndFlow();
        }

        // ------------------------------------------------------------------
        // DEFEAT FLOW
        // ------------------------------------------------------------------

        private IEnumerator RunDefeatFlow()
        {
            battleFinished = true;

            StopCombat();
            MarkPlayerInjured();
            HideAllResultPopups();

            yield return new WaitForSecondsRealtime(defeatDelay);

            BattleRewardData data = BuildResultData(includeChest: false);

            if (loseUI != null && losePopup != null)
            {
                RestorePopupPosition(losePopup);
                losePopup.SetActive(true);
                yield return StartCoroutine(FadeInRoutine(GetOrCreateGroup(losePopup), popupFadeDuration));

                bool clicked = false;
                loseUI.ShowDefeat(data.exp, data.battleTime, data.battleLoot, () => clicked = true);

                float elapsed = 0f;
                while (!clicked && elapsed < resultClickTimeout)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (loseUI != null)
                    loseUI.Close();
                ReturnToVillage();
                yield break;
            }

            bool shown = BattlePopupController.TryShowLose(new StageResultData
            {
                stageTime = data.battleTime,
                earnedExp = data.exp,
                earnedGold = data.gold,
                rewards = new List<RewardData>()
            });

            if (!shown)
                ReturnToVillage();

            EndFlow();
        }

        // ------------------------------------------------------------------
        // Data helpers
        // ------------------------------------------------------------------

        private BattleRewardData BuildResultData(bool includeChest)
        {
            var data = new BattleRewardData
            {
                battleTime = GetBattleTime()
            };

            data.exp = PlayerStatSystem.Instance != null
                ? PlayerStatSystem.Instance.SessionExpEarned : 0;
            data.gold = GoldSystem.Instance != null
                ? GoldSystem.Instance.SessionGoldEarned : 0;

            foreach (ItemReward loot in battleLoot)
                BattleRewardData.AddOrMerge(data.battleLoot, loot);

            if (includeChest)
            {
                foreach (ItemReward reward in chestReward)
                    BattleRewardData.AddOrMerge(data.chestReward, reward);
            }

            return data;
        }

        private float GetBattleTime()
        {
            if (StageManager.Instance != null)
                return StageManager.Instance.GetBattleTime();

            var progress = FindObjectOfType<StageProgressController>();
            return progress != null ? progress.GetStageTime() : 0f;
        }

        private void StopCombat()
        {
            if (StageCompleteController.Instance != null)
            {
                StageCompleteController.Instance.StopCombat();
                return;
            }

            var world = FindObjectOfType<EternalClash.World.WorldScroller>();
            if (world != null)
                world.StopScroll();

            var wave = FindObjectOfType<EternalClash.Wave.WaveManager>();
            if (wave != null)
                wave.enabled = false;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var autoRunner = player.GetComponent<AutoRunner>();
                if (autoRunner != null)
                    autoRunner.StopRunning();
            }
        }

        private void MarkPlayerInjured()
        {
            if (StageCompleteController.Instance != null)
            {
                StageCompleteController.Instance.MarkPlayerInjured();
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            var health = player != null
                ? player.GetComponent<EternalClash.Character.HealthSystem>()
                : null;
            int maxHp = health != null
                ? health.MaxHealth
                : (PlayerStatSystem.Instance != null ? PlayerStatSystem.Instance.TotalMaxHealth : 100);

            EternalClash.Core.PlayerConditionSystem.Instance?.MarkInjured(maxHp);
        }

        private void HideAllResultPopups()
        {
            if (chestRewardPopup != null)
                chestRewardPopup.SetActive(false);
            if (winPopup != null)
                winPopup.SetActive(false);
            if (losePopup != null)
                losePopup.SetActive(false);
        }

        private CanvasGroup GetOrCreateGroup(GameObject popup)
        {
            CanvasGroup group = popup.GetComponent<CanvasGroup>();
            if (group == null)
                group = popup.AddComponent<CanvasGroup>();
            return group;
        }

        private IEnumerator FadeInRoutine(CanvasGroup group, float duration)
        {
            if (group == null) yield break;

            group.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                yield return null;
            }
            group.alpha = 1f;
        }

        private void ReturnToVillage()
        {
            if (returnRequested)
                return;
            returnRequested = true;

            if (StageCompleteController.Instance != null)
            {
                StageCompleteController.Instance.ReturnToVillage();
            }
            else
            {
                EternalClash.Core.SceneLoader.LoadTown();
            }
            EndFlow();
        }

        private void EndFlow()
        {
            flowActive = false;
            battleFinished = false;
            activeFlow = null;
            returnRequested = false;
        }
    }
}
