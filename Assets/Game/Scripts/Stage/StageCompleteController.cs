using UnityEngine;
using System.Collections;
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
        [SerializeField] private GameObject stageClearBanner;
        [SerializeField] private float stageClearBannerHoldSeconds = 1.6f;
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

        private void Awake()
        {
            if (Instance != null)
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
            if (stageClearBanner != null)
                stageClearBanner.SetActive(false);

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

            var progress = FindObjectOfType<StageProgressController>();
            float stageTime = progress != null ? progress.GetStageTime() : 60f;

            int baseExp = 100;
            int stageLevel = SaveManager.Instance != null && SaveManager.Instance.Data != null
                ? Mathf.Clamp(SaveManager.Instance.Data.stageLevel, 1, 5)
                : 1;
            int earnedExp = baseExp + Mathf.RoundToInt(stageTime) * 2;

            int earnedGold = Random.Range(30, 80);

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
                chest.SetRewardSprite(ResolveRewardSprite(currentReward));
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
            var stats = PlayerStatSystem.Instance;

            if (goldSys != null)
                goldSys.AddGold(stageResult.earnedGold);

            if (stats != null)
                stats.AddExp(stageResult.earnedExp);

            if (currentReward != null && currentReward.type == RewardType.Material && currentReward.amount > 0)
            {
                goldSys?.AddMaterials(currentReward.amount, 0);
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
        /// WIN path after the chest reward has been granted: disable ChestRewardPanel
        /// (already handled by the caller), show the StageClearBanner briefly, then
        /// reveal the victory StageResultPanel.
        /// </summary>
        private void ShowVictoryFlow()
        {
            StopAllCoroutines();

            if (stageClearBanner != null)
                stageClearBanner.SetActive(true);

            StartCoroutine(ShowStageResultAfterBanner());
        }

        private IEnumerator ShowStageResultAfterBanner()
        {
            if (stageClearBannerHoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(stageClearBannerHoldSeconds);

            if (stageClearBanner != null)
                stageClearBanner.SetActive(false);

            if (stageResultUI != null)
                stageResultUI.ShowVictory();
            else if (stageResultPanel != null)
                stageResultPanel.SetActive(true);
        }

        private void ShowDefeatPopup(float stageTime, int earnedExp)
        {
            if (stageResultUI != null)
                stageResultUI.ShowDefeat(stageTime, earnedExp);

            if (stageResultPanel != null)
                stageResultPanel.SetActive(true);
        }

        public void OnWinContinueClicked()
        {
            if (stageResultPanel != null)
                stageResultPanel.SetActive(false);

            CheckLevelUp();
        }

        private void CheckLevelUp()
        {
            var stats = PlayerStatSystem.Instance;
            if (stats != null && stats.StatPoints > 0)
            {
                ShowLevelUpUI();
            }
            else
            {
                ReturnToVillage();
            }
        }

        private void ShowLevelUpUI()
        {
            if (levelUpPanel != null)
                levelUpPanel.SetActive(true);
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

            var data = SaveManager.Instance?.Data;
            if (data != null)
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

            var progress = FindObjectOfType<StageProgressController>();
            float stageTime = progress != null ? progress.GetStageTime() : 0f;

            int baseExp = 10;
            int earnedExp = Mathf.Max(0, Mathf.RoundToInt(stageTime) / 6);

            stageResult = new StageResultData
            {
                stageTime = stageTime,
                earnedExp = earnedExp,
                earnedGold = 0,
                rewards = new System.Collections.Generic.List<RewardData>()
            };

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var health = player.GetComponent<EternalClash.Character.HealthSystem>();
                int maxHp = health != null ? health.MaxHealth : 100;
                EternalClash.Core.PlayerConditionSystem.Instance?.MarkInjured(0, maxHp);
            }
            else
            {
                EternalClash.Core.PlayerConditionSystem.Instance?.MarkInjured(0, 100);
            }

            ShowDefeatPopup(stageTime, earnedExp);

            Debug.Log("[DEFEAT] Defeat result UI shown.");
        }

        public void OnDefeatContinueClicked()
        {
            ReturnToVillage();
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
