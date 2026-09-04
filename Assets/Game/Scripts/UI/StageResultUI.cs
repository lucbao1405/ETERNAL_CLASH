using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalClash.Stage;
using EternalClash.Data;

namespace EternalClash.UI
{
    public class StageResultUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text expText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private Button continueButton;

        private void Start()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            var controller = StageCompleteController.Instance;
            if (controller == null || controller.StageResult == null)
                return;

            StageResultData result = controller.StageResult;

            int minutes = Mathf.FloorToInt(result.stageTime / 60f);
            int seconds = Mathf.FloorToInt(result.stageTime % 60f);

            if (timeText != null) timeText.text = $"Time: {minutes:00}:{seconds:00}";
            if (expText != null) expText.text = $"EXP: {result.earnedExp}";
            if (goldText != null) goldText.text = $"Gold: {result.earnedGold}";

            if (rewardText != null && result.rewards != null && result.rewards.Count > 0)
            {
                string rewardSummary = "";
                foreach (var reward in result.rewards)
                {
                    if (reward.type == RewardType.Equipment && reward.item != null)
                        rewardSummary += $"{reward.item.itemName}\n";
                    else
                        rewardSummary += $"{reward.amount} {reward.type}\n";
                }
                rewardText.text = rewardSummary.TrimEnd('\n');
            }
        }

        private void OnContinueClicked()
        {
            StageCompleteController.Instance?.ReturnToVillage();
            if (gameObject != null)
                gameObject.SetActive(false);
        }
    }
}
