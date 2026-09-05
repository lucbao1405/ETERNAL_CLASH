using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalClash.Stage;
using EternalClash.Data;

namespace EternalClash.UI
{
    public class ChestRewardUI : MonoBehaviour
    {
        [Header("Chest")]
        [SerializeField] private GameObject chestObject;
        [SerializeField] private Button openChestButton;
        [SerializeField] private Button continueButton;

        [Header("Reward Display")]
        [SerializeField] private TMP_Text rewardTitleText;
        [SerializeField] private TMP_Text rewardDescText;
        [SerializeField] private Image rewardIcon;
        [SerializeField] private GameObject equipmentRoot;

        private bool chestOpened;

        private void Start()
        {
            if (openChestButton != null)
                openChestButton.onClick.AddListener(OpenChest);
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnEnable()
        {
            chestOpened = false;
            if (chestObject != null)
                chestObject.SetActive(true);
            if (equipmentRoot != null)
                equipmentRoot.SetActive(false);
            if (continueButton != null)
                continueButton.gameObject.SetActive(false);
        }

        private void OpenChest()
        {
            if (chestOpened) return;
            chestOpened = true;

            if (chestObject != null)
                chestObject.SetActive(false);

            var controller = StageCompleteController.Instance;
            if (controller == null) return;

            RewardData reward = controller.CurrentReward;
            if (reward == null) return;

            DisplayReward(reward);
            if (continueButton != null)
                continueButton.gameObject.SetActive(true);
        }

        private void DisplayReward(RewardData reward)
        {
            if (equipmentRoot != null)
                equipmentRoot.SetActive(true);

            if (reward.type == RewardType.Equipment && reward.item != null)
            {
                if (rewardTitleText != null)
                    rewardTitleText.text = reward.item.itemName;

                if (rewardDescText != null)
                    rewardDescText.text = $"{reward.item.rarity}\n{reward.item.description}";

                if (rewardIcon != null)
                    rewardIcon.sprite = null;
            }
            else
            {
                if (rewardTitleText != null)
                    rewardTitleText.text = $"+{reward.amount} {reward.type}";

                if (rewardDescText != null)
                    rewardDescText.text = "";

                if (rewardIcon != null)
                    rewardIcon.sprite = null;
            }
        }

        public void OnContinueClicked()
        {
            StageCompleteController.Instance?.OnChestOpened();
            if (gameObject != null)
                gameObject.SetActive(false);
        }
    }
}
