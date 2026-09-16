using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.UI
{
    /// <summary>
    /// Thong bao ngan hien giua man hinh roi tu mo di, vd "Khong du mau de vao tran".
    /// Tu tao Canvas rieng (nam tren moi UI), khong can dat vao scene.
    /// </summary>
    public sealed class ToastMessage : MonoBehaviour
    {
        private const float ShowSeconds = 1.8f;
        private const float FadeSeconds = 0.35f;

        private static ToastMessage instance;

        private CanvasGroup group;
        private TMP_Text label;
        private Coroutine routine;

        public static void Show(string message)
        {
            if (instance == null)
                instance = Create();
            instance.Display(message);
        }

        private static ToastMessage Create()
        {
            var root = new GameObject("ToastMessage", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;

            var toast = root.AddComponent<ToastMessage>();
            toast.group = root.GetComponent<CanvasGroup>();
            toast.group.alpha = 0f;
            toast.group.blocksRaycasts = false;
            toast.group.interactable = false;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(root.transform, false);
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(860f, 220f);
            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.8f);
            panelImage.raycastTarget = false;

            var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)text.transform;
            textRect.SetParent(panelRect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(30f, 20f);
            textRect.offsetMax = new Vector2(-30f, -20f);

            toast.label = text.GetComponent<TextMeshProUGUI>();
            toast.label.alignment = TextAlignmentOptions.Center;
            toast.label.fontSize = 46f;
            toast.label.color = Color.white;
            toast.label.enableWordWrapping = true;
            toast.label.raycastTarget = false;

            // Toast song qua cac scene, chi tao 1 lan.
            DontDestroyOnLoad(root);
            return toast;
        }

        private void Display(string message)
        {
            label.text = message;
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            group.alpha = 1f;
            yield return new WaitForSecondsRealtime(ShowSeconds);

            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / FadeSeconds;
                yield return null;
            }

            group.alpha = 0f;
            routine = null;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
