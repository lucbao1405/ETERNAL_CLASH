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
            if (strText != null) strText.text = $"STR: {stats.BaseStrength}";
            if (intText != null) intText.text = $"INT: {stats.BaseIntelligence}";
            if (vitText != null) vitText.text = $"VIT: {stats.BaseVitality}";
            if (luckText != null) luckText.text = $"LUCK: {stats.BaseLuck}";

            bool hasPoints = stats.StatPoints > 0;
            if (strButton != null) strButton.interactable = hasPoints;
            if (intButton != null) intButton.interactable = hasPoints;
            if (vitButton != null) vitButton.interactable = hasPoints;
            if (luckButton != null) luckButton.interactable = hasPoints;
        }

        private void OnAddStr() { EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick); PlayerStatSystem.Instance?.AllocateStrength(); }
        private void OnAddInt() { EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick); PlayerStatSystem.Instance?.AllocateIntelligence(); }
        private void OnAddVit() { EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick); PlayerStatSystem.Instance?.AllocateVitality(); }
        private void OnAddLuck() { EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick); PlayerStatSystem.Instance?.AllocateLuck(); }

        private void OnConfirm()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            StageCompleteController.Instance?.OnLevelUpConfirmed();
            if (gameObject != null)
                gameObject.SetActive(false);
        }
    }
}
