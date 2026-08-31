using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EternalClash.UI
{
    public class StageProgressUI : MonoBehaviour
    {
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text stageNameText;

        private void Start()
        {
            if (stageNameText != null)
                stageNameText.text = "Ải 1: Thảo Nguyên";
        }

        private void Update()
        {
            if (DistanceProgress.Instance == null)
                return;

            float progress = DistanceProgress.Instance.Progress;

            if (progressSlider != null)
                progressSlider.value = progress;

            if (fillImage != null)
                fillImage.fillAmount = progress;

            if (progressText != null)
                progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }
    }
}
