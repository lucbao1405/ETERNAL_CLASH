using UnityEngine;
using EternalClash.Data;
using EternalClash.Village;
using EternalClash.Core;
using EternalClash.Core.Save;
using EternalClash.Chest;
using EternalClash.World;
using EternalClash.Player;
using EternalClash.Combat;
using EternalClash.Wave;

namespace EternalClash.Stage
{
    public class StageCompleteController : MonoBehaviour
    {
        public static StageCompleteController Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject chestRewardPanel;
        [SerializeField] private GameObject equipmentPreviewPanel;
        [SerializeField] private GameObject stageResultPanel;
        [SerializeField] private GameObject levelUpPanel;
        [SerializeField] private UI.StageResultUI stageResultUIScript;

        [Header("World")]
        [SerializeField] private World.WorldScroller worldScroller;

        public RewardData CurrentReward => currentReward;
        public StageResultData StageResult => stageResult;
        public UI.StageResultUI StageResultUI => stageResultUI;
        private RewardData currentReward;
        private StageResultData stageResult;
        private UI.StageResultUI stageResultUI;
        private bool isProcessing;
        private bool defeatProcessed;

        private const string BattleSceneName = "Battle";

        /// <summary>
        /// Tu tao controller trong scene Battle neu scene chua co san, theo dung mau
        /// ma UI.BattlePopupController dang dung. Truoc day class nay khong nam trong
        /// scene nao nen Instance luon null - StageManager.CompleteStage() goi
        /// BeginPostStageFlow() vao chỗ trong, va BattlePopupController khong lay duoc
        /// StageResult de hien thi.
        /// Cac tham chieu UI panel deu duoc kiem tra null truoc khi dung nen ban tu
        /// tao (khong co panel nao) van chay an toan.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            // RuntimeInitializeOnLoadMethod chi chay DUNG MOT LAN sau scene dau tien.
            // Neu game khoi dong o MainMenu hoac Town roi moi vao Battle thi kiem tra
            // ten scene o lan chay do that bai va controller khong bao gio duoc tao,
            // khien StageManager.CompleteStage() goi vao Instance null -> khong co
            // popup thang/thua. Vi vay phai bat them su kien sceneLoaded de kiem tra
            // lai moi lan doi scene.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

            EnsureInBattleScene();
        }

        private static void OnSceneLoaded(
            UnityEngine.SceneManagement.Scene scene,
            UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            EnsureInBattleScene();
        }

        /// <summary>
        /// Tu tao controller trong scene Battle neu scene chua co san, theo dung mau
        /// ma UI.BattlePopupController dang dung. Cac tham chieu UI panel deu duoc
        /// kiem tra null truoc khi dung nen ban tu tao van chay an toan.
        /// </summary>
        private static void EnsureInBattleScene()
        {
            if (Instance != null)
                return;

            UnityEngine.SceneManagement.Scene scene =
                UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            if (!scene.IsValid() ||
                !string.Equals(scene.name, BattleSceneName, System.StringComparison.OrdinalIgnoreCase))
                return;

            if (FindObjectOfType<StageCompleteController>() != null)
                return;

            new GameObject("StageCompleteController (Runtime)")
                .AddComponent<StageCompleteController>();

            Debug.Log("[StageComplete] Da tu tao controller cho scene Battle.");
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (chestRewardPanel != null)
                chestRewardPanel.SetActive(false);
            if (equipmentPreviewPanel != null)
                equipmentPreviewPanel.SetActive(false);
            if (stageResultPanel != null)
                stageResultPanel.SetActive(false);
            if (levelUpPanel != null)
                levelUpPanel.SetActive(false);

            if (stageResultUI == null)
            {
                if (stageResultUIScript != null) stageResultUI = stageResultUIScript;
                else if (stageResultPanel != null) stageResultUI = stageResultPanel.GetComponent<UI.StageResultUI>();
            }
        }

        public void BeginPostStageFlow()
        {
            if (isProcessing) return;
            isProcessing = true;

            StopCombat();

            float stageTime = ResolveBattleTime(60f);

            int stageLevel = SaveManager.Instance != null && SaveManager.Instance.Data != null
                ? Mathf.Clamp(SaveManager.Instance.Data.stageLevel, 1, 5)
                : 1;

            // Khong con thuong them khi thang. Popup bao dung so EXP va vang da kiem
            // duoc trong tran (nhat tu quai va vat pham) - nhung so nay da duoc cong
            // vao tai khoan ngay luc nhat roi, o day chi doc lai de hien thi.
            int earnedExp = PlayerStatSystem.Instance != null
                ? PlayerStatSystem.Instance.SessionExpEarned : 0;
            int earnedGold = GoldSystem.Instance != null
                ? GoldSystem.Instance.SessionGoldEarned : 0;

            currentReward = RewardGenerator.GenerateStageReward(stageLevel);

            stageResult = new StageResultData
            {
                stageTime = stageTime,
                earnedExp = earnedExp,
                earnedGold = earnedGold,
                rewards = new System.Collections.Generic.List<RewardData> { currentReward }
            };

            SpawnChest();
        }

        private void StopCombat()
        {
            var scroller = FindObjectOfType<WorldScroller>();
            if (scroller != null)
            {
                scroller.StopScroll();
                scroller.SetSpeedMultiplier(0f);
            }

            var loopController = FindObjectOfType<WorldLoopController>();
            if (loopController != null)
                loopController.StopScroll();

            var loopSpawner = FindObjectOfType<WorldLoopSpawner>();
            if (loopSpawner != null)
                loopSpawner.enabled = false;

            var encounterSpawner = FindObjectOfType<EncounterSpawner>();
            if (encounterSpawner != null)
                encounterSpawner.enabled = false;

            var waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
                waveManager.enabled = false;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var autoRunner = player.GetComponent<AutoRunner>();
                if (autoRunner != null)
                    autoRunner.StopRunning();

                var combatController = player.GetComponent<CombatController>();
                if (combatController != null)
                    combatController.enabled = false;
            }
        }

        private void SpawnChest()
        {
            ChestController chest = null;
            if (ChestSpawnFlow.Instance != null)
                chest = ChestSpawnFlow.Instance.SpawnChest();

            if (chest != null && currentReward != null)
            {
                chest.SetRewardData(currentReward);
            }

            // Khong sinh duoc ruong va cung khong co panel "Open Chest": khong con
            // ai co the goi OnChestOpened(), luong se dung tai day va popup ket qua
            // khong bao gio hien. Truong hop do di thang toi trao thuong + popup.
            // Khi scene co du ruong/panel thi duong cu van chay nhu thiet ke.
            if (chest == null && chestRewardPanel == null)
            {
                GrantReward();
                ContinueAfterReward();
                return;
            }

            ShowChestRewardUI();
        }

        /// <summary>
        /// Enables the scene ChestRewardPanel ("Open Chest" prompt). Public so the
        /// runtime chest-spawn flow can re-show it without owning the panel object.
        /// </summary>
        public void ShowChestRewardUI()
        {
            if (chestRewardPanel != null)
                chestRewardPanel.SetActive(true);
        }

        public void OnChestOpened()
        {
            if (chestRewardPanel != null)
                chestRewardPanel.SetActive(false);

            if (currentReward != null && currentReward.type == RewardType.Equipment)
            {
                ShowEquipmentPreview();
            }
            else
            {
                GrantReward();
                ContinueAfterReward();
            }
        }

        private void ShowEquipmentPreview()
        {
            if (equipmentPreviewPanel != null)
                equipmentPreviewPanel.SetActive(true);
        }

        public void OnEquipAccepted()
        {
            if (equipmentPreviewPanel != null)
                equipmentPreviewPanel.SetActive(false);

            EquipmentSystem.Instance?.EquipItem(currentReward.item);

            GrantReward();
            ContinueAfterReward();
        }

        public void OnEquipSkipped()
        {
            if (equipmentPreviewPanel != null)
                equipmentPreviewPanel.SetActive(false);

            GrantReward();
            ContinueAfterReward();
        }

        private void GrantReward()
        {
            if (stageResult == null) return;

            var goldSys = GoldSystem.Instance;

            // Khong cong lai earnedGold / earnedExp: hai con so do la TONG da kiem
            // duoc trong tran va da vao tai khoan ngay luc nhat. Cong them lan nua
            // se thanh nhan doi. O day chi trao phan thuong cua ruong.

            // Trao phan thuong cua ruong theo dung loai. Truoc day chi xu ly Material:
            // phan thuong Gold bi bo qua (popup van hien so nen nguoi choi thay thieu
            // vang), con Gem thi mat han du RewardGenerator sinh no voi ti le 20%.
            if (currentReward != null && currentReward.amount > 0)
            {
                switch (currentReward.type)
                {
                    case RewardType.Gold:
                        goldSys?.AddGold(currentReward.amount);
                        break;

                    case RewardType.Gem:
                        goldSys?.AddGem(currentReward.amount);
                        break;

                    case RewardType.Material:
                        goldSys?.AddMaterials(currentReward.amount, 0);
                        break;

                    // Equipment duoc trao rieng qua EquipmentSystem.EquipItem()
                    // trong OnEquipAccepted(), khong xu ly o day.
                }
            }
        }

        private Sprite ResolveRewardSprite(RewardData reward)
        {
            if (reward == null) return null;

            if (reward.HasItem && !string.IsNullOrEmpty(reward.item.iconSpriteName))
            {
                return Resources.Load<Sprite>(reward.item.iconSpriteName);
            }

            return null;
        }

        private void ContinueAfterReward()
        {
            ShowVictoryFlow();
        }

        /// <summary>
        /// WIN path after the chest reward has been granted: the old StageClearBanner
        /// is no longer used, the victory StageResultPanel opens directly.
        /// </summary>
        private void ShowVictoryFlow()
        {
            if (UI.BattlePopupController.TryShowWin())
            {
                if (stageResultPanel != null)
                    stageResultPanel.SetActive(false);
                return;
            }

            if (stageResultUI != null)
                stageResultUI.ShowVictory();
            else if (stageResultPanel != null)
                stageResultPanel.SetActive(true);
        }

        private void ShowDefeatPopup(float stageTime, int earnedExp)
        {
            if (UI.BattlePopupController.TryShowLose())
            {
                if (stageResultPanel != null)
                    stageResultPanel.SetActive(false);
                return;
            }

            if (stageResultUI != null)
                stageResultUI.ShowDefeat(stageTime, earnedExp);

            if (stageResultPanel != null)
                stageResultPanel.SetActive(true);
        }

        public void OnWinContinueClicked()
        {
            if (stageResultPanel != null)
                stageResultPanel.SetActive(false);

            ReturnToVillage();
        }

        public void OnLevelUpConfirmed()
        {
            if (levelUpPanel != null)
                levelUpPanel.SetActive(false);

            ReturnToVillage();
        }

        public void ReturnToVillage()
        {
            isProcessing = false;
            defeatProcessed = false;

            // Chi hoi day mau khi THANG. Neu vua thua thi phai giu nguyen trang thai
            // thuong tich ma BeginDefeatFlow() vua dat - truoc day doan nay chay vo
            // dieu kien nen no xoa sach trang thai do va co che hoi phuc khong bao
            // gio chay duoc.
            bool injured = EternalClash.Core.PlayerConditionSystem.Instance != null
                && EternalClash.Core.PlayerConditionSystem.Instance.IsInjured;

            var data = SaveManager.Instance?.Data;
            if (data != null && !injured)
            {
                data.playerCondition = (int)PlayerCondition.Normal;
                var player = GameObject.FindGameObjectWithTag("Player");
                var health = player != null ? player.GetComponent<EternalClash.Character.HealthSystem>() : null;
                if (health != null)
                {
                    data.maxHp = health.MaxHealth;
                    data.currentHp = health.MaxHealth;
                }
            }

            SaveManager.Instance?.Save();
            EternalClash.Core.SceneLoader.LoadTown();
        }

        public void BeginDefeatFlow()
        {
            if (defeatProcessed) return;
            defeatProcessed = true;

            StopCombat();

            float stageTime = ResolveBattleTime(0f);

            // Thua cung giu nguyen nhung gi da kiem duoc trong tran, khong bi tru.
            // Cach tinh giong het luong thang - chi khac la khong co ruong phan thuong.
            int earnedExp = PlayerStatSystem.Instance != null
                ? PlayerStatSystem.Instance.SessionExpEarned : 0;
            int earnedGold = GoldSystem.Instance != null
                ? GoldSystem.Instance.SessionGoldEarned : 0;

            stageResult = new StageResultData
            {
                stageTime = stageTime,
                earnedExp = earnedExp,
                earnedGold = earnedGold,
                rewards = new System.Collections.Generic.List<RewardData>()
            };

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var health = player.GetComponent<EternalClash.Character.HealthSystem>();
                int maxHp = health != null ? health.MaxHealth : 100;
                EternalClash.Core.PlayerConditionSystem.Instance?.MarkInjured(maxHp);
            }
            else
            {
                int maxHp = PlayerStatSystem.Instance != null
                    ? PlayerStatSystem.Instance.TotalMaxHealth : 100;
                EternalClash.Core.PlayerConditionSystem.Instance?.MarkInjured(maxHp);
            }

            // Khong cong lai gi o day: EXP va vang deu da vao tai khoan ngay luc
            // nhat trong tran. Cong them nua se thanh nhan doi.
            ShowDefeatPopup(stageTime, earnedExp);

            Debug.Log("[DEFEAT] Defeat result UI shown.");
        }

        public void OnDefeatContinueClicked()
        {
            ReturnToVillage();
        }

        private float ResolveBattleTime(float fallback)
        {
            if (StageManager.Instance != null)
                return StageManager.Instance.GetBattleTime();

            var progress = FindObjectOfType<StageProgressController>();
            return progress != null ? progress.GetStageTime() : fallback;
        }
    }

    public class StageResultData
    {
        public float stageTime;
        public int earnedExp;
        public int earnedGold;
        public System.Collections.Generic.List<RewardData> rewards;
    }
}
