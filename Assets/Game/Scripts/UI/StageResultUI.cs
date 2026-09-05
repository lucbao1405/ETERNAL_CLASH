using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalClash.Stage;
using EternalClash.Data;
using EternalClash.Village;
using EternalClash.Core;

namespace EternalClash.UI
{
    public enum StageResultMode
    {
        Victory,
        Defeat
    }

    public class StageResultUI : MonoBehaviour
    {
        [Header("Top")]
        [SerializeField] private GameObject playerDisplayRoot;
        [SerializeField] private TMP_Text levelText;

        [Header("Title")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private string victoryTitle = "Stage Complete";
        [SerializeField] private string defeatTitle = "Defeated";

        [Header("Message")]
        [SerializeField] private GameObject messageBoxRoot;
        [SerializeField] private TMP_Text messageBoxText;
        [SerializeField] private string defeatMessage = "Your character has fallen in battle.";

        [Header("Progress")]
        [SerializeField] private Image expBarFill;
        [SerializeField] private TMP_Text expPercentText;

        [Header("Info Boxes")]
        [SerializeField] private GameObject timeBoxRoot;
        [SerializeField] private TMP_Text timeBoxText;
        [SerializeField] private GameObject xpBoxRoot;
        [SerializeField] private TMP_Text xpBoxText;

        [Header("Actions")]
        [SerializeField] private Button continueButton;

        [Header("Reward")]
        [SerializeField] private GameObject rewardItemRoot;
        [SerializeField] private Image rewardItemIcon;
        [SerializeField] private TMP_Text rewardItemNameText;

        private StageResultMode currentMode = StageResultMode.Victory;
        private int defeatEarnedExp;

        private void Start()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        public void ShowVictory()
        {
            currentMode = StageResultMode.Victory;
            gameObject.SetActive(true);
            RefreshUI();
        }

        public void ShowDefeat(float stageTime, int earnedExp)
        {
            currentMode = StageResultMode.Defeat;
            defeatEarnedExp = earnedExp;
            gameObject.SetActive(true);
            RefreshDefeat(stageTime, earnedExp);
        }

        private void RefreshUI()
        {
            if (currentMode == StageResultMode.Defeat)
            {
                var controller = StageCompleteController.Instance;
                float t = controller != null && controller.StageResult != null ? controller.StageResult.stageTime : 0f;
                int e = controller != null && controller.StageResult != null ? controller.StageResult.earnedExp : defeatEarnedExp;
                RefreshDefeat(t, e);
                return;
            }

            var stageCtrl = StageCompleteController.Instance;
            if (stageCtrl == null || stageCtrl.StageResult == null)
                return;

            StageResultData result = stageCtrl.StageResult;
            var stats = PlayerStatSystem.Instance;

            ApplyChrome(victoryTitle, null, true, true, true, true);

            int minutes = Mathf.FloorToInt(result.stageTime / 60f);
            int seconds = Mathf.FloorToInt(result.stageTime % 60f);

            if (timeBoxText != null)
                timeBoxText.text = $"Time\n{minutes:00}:{seconds:00}";

            if (xpBoxText != null)
                xpBoxText.text = $"XP\n+{result.earnedExp}";

            if (stats != null)
            {
                if (levelText != null)
                    levelText.text = $"Level {stats.Level}";

                if (expBarFill != null)
                {
                    float pct = stats.RequiredExp > 0 ? (float)stats.CurrentExp / stats.RequiredExp : 0f;
                    expBarFill.fillAmount = pct;
                }

                if (expPercentText != null)
                {
                    int pct = stats.RequiredExp > 0 ? Mathf.RoundToInt((float)stats.CurrentExp / stats.RequiredExp * 100f) : 0;
                    expPercentText.text = $"{stats.CurrentExp} / {stats.RequiredExp} ({pct}%)";
                }
            }

            if (rewardItemRoot != null)
                rewardItemRoot.SetActive(false);

            if (result.rewards != null && result.rewards.Count > 0)
            {
                foreach (var reward in result.rewards)
                {
                    if (reward.type == RewardType.Equipment && reward.item != null)
                    {
                        if (rewardItemRoot != null)
                            rewardItemRoot.SetActive(true);

                        if (rewardItemNameText != null)
                            rewardItemNameText.text = reward.item.itemName;

                        if (rewardItemIcon != null && !string.IsNullOrEmpty(reward.item.iconSpriteName))
                        {
                            rewardItemIcon.sprite = Resources.Load<Sprite>(reward.item.iconSpriteName);
                            rewardItemIcon.enabled = rewardItemIcon.sprite != null;
                        }
                        break;
                    }
                    else if (reward.type == RewardType.Material)
                    {
                        if (rewardItemRoot != null)
                            rewardItemRoot.SetActive(true);

                        if (rewardItemNameText != null)
                            rewardItemNameText.text = $"+{reward.amount} {reward.type}";

                        if (rewardItemIcon != null)
                            rewardItemIcon.enabled = false;
                        break;
                    }
                }
            }
        }

        private void RefreshDefeat(float stageTime, int earnedExp)
        {
            ApplyChrome(defeatTitle, defeatMessage, false, true, true, false);

            int minutes = Mathf.FloorToInt(stageTime / 60f);
            int seconds = Mathf.FloorToInt(stageTime % 60f);

            if (timeBoxText != null)
                timeBoxText.text = $"Time Survived\n{minutes:00}:{seconds:00}";

            if (xpBoxText != null)
                xpBoxText.text = earnedExp > 0 ? $"EXP\n+{earnedExp}" : "EXP\n+0";

            if (rewardItemRoot != null)
                rewardItemRoot.SetActive(false);
        }

        private void ApplyChrome(string title, string message, bool showExpBar, bool showTimeBox, bool showXpBox, bool showReward)
        {
            if (titleText != null)
                titleText.text = title;

            if (messageBoxRoot != null) messageBoxRoot.SetActive(!string.IsNullOrEmpty(message));
            if (messageBoxText != null) messageBoxText.text = message ?? string.Empty;

            if (expBarFill != null && expBarFill.gameObject.activeSelf != showExpBar)
                expBarFill.gameObject.SetActive(showExpBar);
            if (expPercentText != null && expPercentText.gameObject.activeSelf != showExpBar)
                expPercentText.gameObject.SetActive(showExpBar);

            if (timeBoxRoot != null) timeBoxRoot.SetActive(showTimeBox);
            if (xpBoxRoot != null) xpBoxRoot.SetActive(showXpBox);

            if (rewardItemRoot != null) rewardItemRoot.SetActive(false);

            if (playerDisplayRoot != null) playerDisplayRoot.SetActive(showReward);

            if (continueButton != null)
            {
                TMP_Text btnLabel = continueButton.GetComponentInChildren<TMP_Text>();
                if (btnLabel != null)
                    btnLabel.text = "Continue";
            }
        }

        private void OnContinueClicked()
        {
            if (currentMode == StageResultMode.Defeat)
            {
                StageCompleteController.Instance?.OnDefeatContinueClicked();
            }
            else
            {
                StageCompleteController.Instance?.OnWinContinueClicked();
            }

            if (gameObject != null)
                gameObject.SetActive(false);
        }
    }
}
