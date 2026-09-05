using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalClash.Stage;
using EternalClash.Data;
using EternalClash.Chest;

namespace EternalClash.UI
{
    /// <summary>
    /// Chest open / reward panel.
    ///
    /// Phase 1 (open prompt): the player is asked to click OPEN ("Open Chest").
    /// Phase 2 (reward): after the chest opening effect finishes, the reward
    /// summary (Gold / Materials / Items) is shown together with the Continue button.
    ///
    /// Reward granting is NOT done here - Continue keeps using the existing
    /// StageCompleteController flow.
    /// </summary>
    public class ChestRewardUI : MonoBehaviour
    {
        [Header("Chest")]
        [SerializeField] private GameObject chestObject;
        [SerializeField] private Button openChestButton;
        [SerializeField] private Button continueButton;

        [Header("Open Prompt")]
        [SerializeField] private TMP_Text openPromptText;

        [Header("Reward Display")]
        [SerializeField] private GameObject rewardContentRoot;
        [SerializeField] private TMP_Text rewardGoldText;
        [SerializeField] private TMP_Text rewardMaterialText;
        [SerializeField] private TMP_Text rewardItemText;

        private bool chestOpened;
        private bool subscribed;
        private bool openButtonBound;
        private bool continueButtonBound;
        private ChestController worldChest;

        private void Awake()
        {
            BindButtons();
        }

        /// <summary>
        /// Binds the runtime button listeners exactly once per instance, so re-shown
        /// (or re-created) panels never accumulate duplicate OPEN/Continue handlers.
        /// OPEN -> OnOpenClicked -> ChestController.OpenChest().
        /// </summary>
        private void BindButtons()
        {
            if (!openButtonBound && openChestButton != null)
            {
                openChestButton.onClick.AddListener(OnOpenClicked);
                openButtonBound = true;
            }

            if (!continueButtonBound && continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
                continueButtonBound = true;
            }
        }

        private void OnEnable()
        {
            chestOpened = false;
            ResolveChest();
            BindChestEvents();
            ShowOpenPhase();
        }

        private void OnDisable()
        {
            UnbindChestEvents();
        }

        private void ResolveChest()
        {
            worldChest = null;

            if (chestObject != null)
                worldChest = chestObject.GetComponent<ChestController>();

            if (worldChest == null && ChestSpawnFlow.Instance != null)
                worldChest = ChestSpawnFlow.Instance.LastSpawnedChest;
        }

        private void BindChestEvents()
        {
            if (subscribed) return;
            if (worldChest == null) return;

            worldChest.OnOpened += OnChestOpened;
            subscribed = true;
        }

        private void UnbindChestEvents()
        {
            if (!subscribed) return;
            if (worldChest == null) return;

            worldChest.OnOpened -= OnChestOpened;
            subscribed = false;
        }

        private void ShowOpenPhase()
        {
            if (openPromptText != null)
            {
                openPromptText.text = "Open Chest";
                openPromptText.gameObject.SetActive(true);
            }

            if (openChestButton != null)
                openChestButton.gameObject.SetActive(true);

            if (rewardContentRoot != null)
                rewardContentRoot.SetActive(false);

            if (continueButton != null)
                continueButton.gameObject.SetActive(false);
        }

        private void OnOpenClicked()
        {
            if (chestOpened) return;
            chestOpened = true;

            if (openPromptText != null)
                openPromptText.gameObject.SetActive(false);
            if (openChestButton != null)
                openChestButton.gameObject.SetActive(false);

            if (worldChest != null)
                worldChest.OpenChest();
            else
                ShowRewardPhase();
        }

        private void OnChestOpened()
        {
            ShowRewardPhase();
        }

        private void ShowRewardPhase()
        {
            chestOpened = true;

            if (openPromptText != null)
                openPromptText.gameObject.SetActive(false);
            if (openChestButton != null)
                openChestButton.gameObject.SetActive(false);

            BuildRewardSummary();

            if (rewardContentRoot != null)
                rewardContentRoot.SetActive(true);

            if (continueButton != null)
                continueButton.gameObject.SetActive(true);
        }

        private void BuildRewardSummary()
        {
            var controller = StageCompleteController.Instance;
            if (controller == null)
            {
                HideRow(rewardGoldText);
                HideRow(rewardMaterialText);
                HideRow(rewardItemText);
                return;
            }

            int gold = 0;
            int material = 0;
            string itemLine = null;

            if (controller.StageResult != null)
                gold = controller.StageResult.earnedGold;

            RewardData reward = controller.CurrentReward;
            if (reward != null)
            {
                switch (reward.type)
                {
                    case RewardType.Gold:
                        gold += reward.amount;
                        break;
                    case RewardType.Material:
                        material = reward.amount;
                        break;
                    case RewardType.Gem:
                        itemLine = $"+{reward.amount} Gems";
                        break;
                    case RewardType.Equipment:
                        if (reward.item != null)
                            itemLine = reward.item.itemName;
                        break;
                }
            }

            ShowRow(rewardGoldText, gold > 0, $"+{gold} Gold");
            ShowRow(rewardMaterialText, material > 0, $"+{material} Materials");
            ShowRow(rewardItemText, !string.IsNullOrEmpty(itemLine), itemLine);
        }

        private static void ShowRow(TMP_Text text, bool visible, string value)
        {
            if (text == null) return;
            text.text = value ?? string.Empty;
            text.gameObject.SetActive(visible);
        }

        private static void HideRow(TMP_Text text)
        {
            if (text == null) return;
            text.gameObject.SetActive(false);
        }

        public void OnContinueClicked()
        {
            StageCompleteController.Instance?.OnChestOpened();
            if (gameObject != null)
                gameObject.SetActive(false);
        }
    }
}
