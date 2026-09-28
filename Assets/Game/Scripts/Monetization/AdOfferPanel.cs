using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.Monetization
{
    /// <summary>
    /// Panel offer "xem quang cao?" dat san trong scene (Battle.unity) theo kieu
    /// art cua Town: khung_Lon + button.png. OfferOverlayUI tu tim panel theo
    /// ten "Ad_Offer" khi hien offer (X2 cuoi tran, hoi sinh); neu scene khong
    /// co panel nay thi tu dung phan sinh UI runtime nhu cu.
    /// cau truc con (tu wire theo ten, khong can wire tay):
    ///   Ad_Offer (script nay, an mac dinh)
    ///     ├─ TitleText  ├─ MessageText  ├─ TimerText (dem nguoc tu dong, co the de trong)
    ///     ├─ WatchButton { WatchLabel }
    ///     └─ CloseButton { CloseLabel }
    /// </summary>
    public sealed class AdOfferPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text watchLabel;
        [SerializeField] private Button watchButton;
        [SerializeField] private Button closeButton;

        private Action<bool> onResult;
        private Coroutine autoDeclineRoutine;
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            AutoWireMissingReferences();

            if (watchButton != null)
                watchButton.onClick.AddListener(OnWatchClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDestroy()
        {
            if (watchButton != null)
                watchButton.onClick.RemoveListener(OnWatchClicked);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        /// <summary>Hien panel; ket qua tra ve qua callback: true = dong y xem,
        /// false = tu choi. Ban than panel khong goi AdsService - ben goi lo.
        /// autoDeclineSeconds > 0: khong chon trong thoi gian do thi tu dong coi
        /// nhu tu choi (dem nguoc hien tren TimerText, dung thoi gian that nen
        /// van chay khi game bi freeze timeScale = 0).</summary>
        public void Present(string title, string message, string watchLabelText,
            float autoDeclineSeconds, Action<bool> result)
        {
            if (titleText != null)
                titleText.text = title;
            if (messageText != null)
                messageText.text = message;
            if (watchLabel != null)
                watchLabel.text = watchLabelText;

            onResult = result;
            IsOpen = true;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);

            StopAutoDecline();
            if (autoDeclineSeconds > 0f)
                autoDeclineRoutine = StartCoroutine(RunAutoDecline(autoDeclineSeconds));
        }

        /// <summary>Dong ma khong tra ket qua (dung khi force-close tu ben ngoai).</summary>
        public void Dismiss()
        {
            StopAutoDecline();
            IsOpen = false;
            onResult = null;
            gameObject.SetActive(false);
        }

        private IEnumerator RunAutoDecline(float seconds)
        {
            float remaining = seconds;
            while (remaining > 0f)
            {
                if (timerText != null)
                    timerText.text = $"Auto-close in {Mathf.CeilToInt(remaining)}s";
                yield return new WaitForSecondsRealtime(0.25f);
                remaining -= 0.25f;
            }

            Close(false);
        }

        private void StopAutoDecline()
        {
            if (autoDeclineRoutine != null)
            {
                StopCoroutine(autoDeclineRoutine);
                autoDeclineRoutine = null;
            }

            if (timerText != null)
                timerText.text = string.Empty;
        }

        private void OnWatchClicked()
        {
            Close(true);
        }

        private void OnCloseClicked()
        {
            Close(false);
        }

        private void Close(bool result)
        {
            StopAutoDecline();
            IsOpen = false;
            gameObject.SetActive(false);

            Action<bool> callback = onResult;
            onResult = null;
            if (callback != null)
                callback(result);
        }

        private void AutoWireMissingReferences()
        {
            if (titleText == null)
                titleText = FindChild("TitleText")?.GetComponent<TMP_Text>();
            if (messageText == null)
                messageText = FindChild("MessageText")?.GetComponent<TMP_Text>();
            if (timerText == null)
                timerText = FindChild("TimerText")?.GetComponent<TMP_Text>();
            if (watchLabel == null)
                watchLabel = FindChild("WatchButton/WatchLabel")?.GetComponent<TMP_Text>();
            if (watchButton == null)
                watchButton = FindChild("WatchButton")?.GetComponent<Button>();
            if (closeButton == null)
                closeButton = FindChild("CloseButton")?.GetComponent<Button>();
        }

        private Transform FindChild(string name)
        {
            return FindDeepChild(transform, name);
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
