using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.UI
{
    /// <summary>
    /// Displays blacksmith outcomes with the existing success and failure panels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UpgradeResultPopup : MonoBehaviour
    {
        [SerializeField] private GameObject successPanel;
        [SerializeField] private GameObject failurePanel;
        [SerializeField] private TMP_Text successTitleText;
        [SerializeField] private TMP_Text successDetailsText;
        [SerializeField] private TMP_Text failureTitleText;
        [SerializeField] private TMP_Text failureReasonText;
        [SerializeField] private Button successCloseButton;
        [SerializeField] private Button failureCloseButton;

        private void Awake()
        {
            AutoWire();
            successCloseButton?.onClick.AddListener(Close);
            failureCloseButton?.onClick.AddListener(Close);
            Close();
        }

        private void OnDestroy()
        {
            successCloseButton?.onClick.RemoveListener(Close);
            failureCloseButton?.onClick.RemoveListener(Close);
        }

        public void ShowSuccess(string itemName, int previousUpgradeLevel, int currentUpgradeLevel,
            string statName, int previousStat, int currentStat)
        {
            if (successTitleText != null)
                successTitleText.text = "UPGRADE SUCCESS";

            if (successDetailsText != null)
            {
                successDetailsText.text = itemName + "\n\nUpgrade +" + previousUpgradeLevel + " -> +" +
                    currentUpgradeLevel + "\n\n" + statName + " +" + previousStat + " -> " + statName +
                    " +" + currentStat;
            }

            failurePanel?.SetActive(false);
            successPanel?.SetActive(true);
        }

        public void ShowFailure(string reason)
        {
            if (failureTitleText != null)
                failureTitleText.text = "UPGRADE FAILED";
            if (failureReasonText != null)
                failureReasonText.text = string.IsNullOrWhiteSpace(reason) ? "Upgrade failed." : reason;

            successPanel?.SetActive(false);
            failurePanel?.SetActive(true);
        }

        public void Close()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            successPanel?.SetActive(false);
            failurePanel?.SetActive(false);
        }

        private void AutoWire()
        {
            successPanel ??= transform.Find("UpGradeSuccess")?.gameObject;
            failurePanel ??= transform.Find("UpGradeFail")?.gameObject;

            successTitleText ??= FindText(successPanel, "Thong_bao/Thong Bao/T");
            successDetailsText ??= FindText(successPanel, "Thong_bao/Chi_tiet/Chiso_tang");
            failureTitleText ??= FindText(failurePanel, "Thong_bao/Thong Bao/T");
            failureReasonText ??= FindText(failurePanel, "Thong_bao/Chi_tiet/Li do");

            successCloseButton ??= successPanel?.transform.Find("X")?.GetComponent<Button>();
            failureCloseButton ??= failurePanel?.transform.Find("X")?.GetComponent<Button>();
        }

        private static TMP_Text FindText(GameObject panel, string path)
        {
            return panel != null ? panel.transform.Find(path)?.GetComponent<TMP_Text>() : null;
        }
    }
}
