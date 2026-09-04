using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalClash.Village;
using EternalClash.Stage;

namespace EternalClash.UI
{
    public class LevelUpUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text pointsText;
        [SerializeField] private TMP_Text strText;
        [SerializeField] private TMP_Text intText;
        [SerializeField] private TMP_Text vitText;
        [SerializeField] private TMP_Text luckText;
        [SerializeField] private Button strButton;
        [SerializeField] private Button intButton;
        [SerializeField] private Button vitButton;
        [SerializeField] private Button luckButton;
        [SerializeField] private Button confirmButton;

        private void Start()
        {
            if (strButton != null) strButton.onClick.AddListener(OnAddStr);
            if (intButton != null) intButton.onClick.AddListener(OnAddInt);
            if (vitButton != null) vitButton.onClick.AddListener(OnAddVit);
            if (luckButton != null) luckButton.onClick.AddListener(OnAddLuck);
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        }

        private void OnEnable()
        {
            RefreshUI();
            if (PlayerStatSystem.Instance != null)
                PlayerStatSystem.Instance.OnStatsChanged += RefreshUI;
        }

        private void OnDisable()
        {
            if (PlayerStatSystem.Instance != null)
                PlayerStatSystem.Instance.OnStatsChanged -= RefreshUI;
        }

        private void RefreshUI()
        {
            var stats = PlayerStatSystem.Instance;
            if (stats == null) return;

            if (pointsText != null) pointsText.text = $"Points: {stats.StatPoints}";
            if (strText != null) strText.text = $"STR: {stats.Strength}";
            if (intText != null) intText.text = $"INT: {stats.Intelligence}";
            if (vitText != null) vitText.text = $"VIT: {stats.Vitality}";
            if (luckText != null) luckText.text = $"LUCK: {stats.Luck}";

            bool hasPoints = stats.StatPoints > 0;
            if (strButton != null) strButton.interactable = hasPoints;
            if (intButton != null) intButton.interactable = hasPoints;
            if (vitButton != null) vitButton.interactable = hasPoints;
            if (luckButton != null) luckButton.interactable = hasPoints;
        }

        private void OnAddStr() => PlayerStatSystem.Instance?.AllocateStrength();
        private void OnAddInt() => PlayerStatSystem.Instance?.AllocateIntelligence();
        private void OnAddVit() => PlayerStatSystem.Instance?.AllocateVitality();
        private void OnAddLuck() => PlayerStatSystem.Instance?.AllocateLuck();

        private void OnConfirm()
        {
            StageCompleteController.Instance?.OnLevelUpConfirmed();
            if (gameObject != null)
                gameObject.SetActive(false);
        }
    }
}
