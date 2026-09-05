using UnityEngine;
using EternalClash.Data;
using EternalClash.Village;
using EternalClash.Core;

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

        [Header("World")]
        [SerializeField] private World.WorldScroller worldScroller;

        public RewardData CurrentReward => currentReward;
        public StageResultData StageResult => stageResult;
        private RewardData currentReward;
        private StageResultData stageResult;
        private bool isProcessing;

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
        }

        public void BeginPostStageFlow()
        {
            if (isProcessing) return;
            isProcessing = true;

            StopWorld();

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

        private void StopWorld()
        {
            if (worldScroller != null)
                worldScroller.SetSpeedMultiplier(0f);
        }

        private void SpawnChest()
        {
            if (ChestSpawnFlow.Instance != null)
                ChestSpawnFlow.Instance.SpawnChest();

            ShowChestRewardUI();
        }

        private void ShowChestRewardUI()
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
        }

        private void ContinueAfterReward()
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
            EternalClash.Core.SceneLoader.LoadTown();
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
