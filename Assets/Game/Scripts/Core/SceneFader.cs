using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EternalClash.Core
{
    /// <summary>
    /// Hieu ung den (fade) khi chuyen scene. Tu tao Canvas overlay toan man den
    /// giau cac UI khac, khong can them gi vao scene. Chay theo thoi gian thuc
    /// (unscaled) vi timeScale co the bang 0 khi Player chet truoc khi thoat.
    /// </summary>
    public sealed class SceneFader : MonoBehaviour
    {
        private const float FadeOutSeconds = 0.35f;
        private const float FadeInSeconds = 0.35f;
        private const int OverlaySortingOrder = 30000;

        private static SceneFader instance;

        private CanvasGroup group;
        private Coroutine routine;

        /// <summary>
        /// Mo dan man hinh sang den, tai sceneName, roi mo dan man hinh moi.
        /// </summary>
        public static void Transition(string sceneName)
        {
            if (instance == null)
                instance = Create();
            instance.Run(sceneName);
        }

        private static SceneFader Create()
        {
            var root = new GameObject("SceneFader", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;

            var fader = root.AddComponent<SceneFader>();
            fader.group = root.GetComponent<CanvasGroup>();
            fader.group.alpha = 0f;
            fader.group.blocksRaycasts = false;
            fader.group.interactable = false;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(root.transform, false);
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = panel.GetComponent<Image>();
            panelImage.color = Color.black;
            panelImage.raycastTarget = false;

            // Fader song qua cac scene, chi tao 1 lan.
            DontDestroyOnLoad(root);
            return fader;
        }

        private void Run(string sceneName)
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(RunRoutine(sceneName));
        }

        private IEnumerator RunRoutine(string sceneName)
        {
            // Chan input trong luc man den de khong bam nham nut cua scene cu.
            group.blocksRaycasts = true;

            for (float t = 0f; t < FadeOutSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / FadeOutSeconds;
                yield return null;
            }
            group.alpha = 1f;

            SceneManager.LoadScene(sceneName);

            // LoadScene dong bo: scene moi da nap xong khi den day, giam dan
            // alpha de lo dan man hinh moi (frame dau van den nen khong lo rap).
            for (float t = 0f; t < FadeInSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / FadeInSeconds;
                yield return null;
            }

            group.alpha = 0f;
            group.blocksRaycasts = false;
            routine = null;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
