using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalClash.Stage;
using EternalClash.Data;

namespace EternalClash.UI
{
    public class EquipmentPreviewUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text rarityText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button backButton;

        private void Start()
        {
            if (equipButton != null)
                equipButton.onClick.AddListener(OnEquipClicked);
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            var controller = StageCompleteController.Instance;
            if (controller == null || controller.CurrentReward == null || controller.CurrentReward.item == null)
                return;

            ItemData item = controller.CurrentReward.item;

            if (itemNameText != null) itemNameText.text = item.itemName;
            if (rarityText != null) rarityText.text = $"{item.rarity}";
            if (descText != null) descText.text = item.description;

            if (statsText != null)
            {
                string stats = "";
                if (item.strBonus > 0) stats += $"STR +{item.strBonus}\n";
                if (item.intBonus > 0) stats += $"INT +{item.intBonus}\n";
                if (item.vitBonus > 0) stats += $"VIT +{item.vitBonus}\n";
                if (item.luckBonus > 0) stats += $"LUCK +{item.luckBonus}\n";
                statsText.text = stats.TrimEnd('\n');
            }
        }

        private void OnEquipClicked()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            StageCompleteController.Instance?.OnEquipAccepted();
            if (gameObject != null)
                gameObject.SetActive(false);
        }

        private void OnBackClicked()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            StageCompleteController.Instance?.OnEquipSkipped();
            if (gameObject != null)
                gameObject.SetActive(false);
        }
    }
}
