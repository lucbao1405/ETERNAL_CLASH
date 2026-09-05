using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.Chest
{
    public class ChestOpenEffectController : MonoBehaviour
    {
        public static ChestOpenEffectController Instance { get; private set; }

        [Header("Overlay")]
        [SerializeField] private Canvas overlayCanvas;
        [SerializeField] private Image overlayImage;
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float blackHoldDuration = 0.1f;
        [SerializeField] private Color overlayColor = Color.black;

        [Header("Sprite Swap")]
        [SerializeField] private Sprite closedChestSprite;
        [SerializeField] private Sprite openedChestSprite;

        public bool IsRunning { get; private set; }

        public Canvas OverlayCanvas => overlayCanvas;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Start idle: hidden and never blocking input. PlayEffect toggles it on/off.
            if (overlayImage != null)
            {
                overlayImage.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f);
                overlayImage.raycastTarget = false;
            }

            if (overlayCanvas != null)
                overlayCanvas.enabled = false;
        }

        /// <summary>
        /// Wire up a runtime-created overlay. Safe to call after Awake.
        /// </summary>
        public void Initialize(Canvas canvas, Image image, Color color)
        {
            overlayCanvas = canvas;
            overlayImage = image;
            overlayColor = color;

            if (overlayImage != null)
            {
                overlayImage.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f);
                overlayImage.raycastTarget = false;
            }

            if (overlayCanvas != null)
                overlayCanvas.enabled = false;
        }

        /// <summary>
        /// Creates and returns a scene-level full-screen overlay used by the open effect,
        /// so no manual scene/prefab wiring is required.
        /// </summary>
        public static ChestOpenEffectController EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            GameObject root = new GameObject("ChestOpenEffectController");

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            GameObject imageObject = new GameObject("OverlayImage");
            imageObject.transform.SetParent(root.transform, false);

            Image image = imageObject.AddComponent<Image>();
            RectTransform rt = (RectTransform)imageObject.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            ChestOpenEffectController controller = root.AddComponent<ChestOpenEffectController>();
            controller.Initialize(canvas, image, Color.black);
            return controller;
        }

        public void Configure(Sprite closed, Sprite opened)
        {
            closedChestSprite = closed;
            openedChestSprite = opened;
        }

        public IEnumerator PlayEffect(ChestController chest)
        {
            if (IsRunning)
                yield break;

            IsRunning = true;

            // Activate the overlay and block clicks while the flash is on screen.
            if (overlayImage != null)
            {
                overlayImage.raycastTarget = true;
                SetOverlayAlpha(0f);
            }
            if (overlayCanvas != null)
                overlayCanvas.enabled = true;

            yield return StartCoroutine(FadeOverlay(0f, 1f, fadeInDuration));

            if (chest != null && openedChestSprite != null)
                chest.SetOpenedSpriteImmediate(openedChestSprite);

            yield return new WaitForSeconds(blackHoldDuration);

            yield return StartCoroutine(FadeOverlay(1f, 0f, fadeOutDuration));

            // Deactivate the overlay so it never blocks the reward UI afterwards.
            if (overlayImage != null)
            {
                SetOverlayAlpha(0f);
                overlayImage.raycastTarget = false;
            }
            if (overlayCanvas != null)
                overlayCanvas.enabled = false;

            IsRunning = false;
        }

        public void OpenChestEffect(ChestController chest)
        {
            StartCoroutine(PlayEffect(chest));
        }

        private void SetOverlayAlpha(float a)
        {
            if (overlayImage == null) return;
            Color c = overlayImage.color;
            c.a = a;
            overlayImage.color = c;
        }

        private IEnumerator FadeOverlay(float from, float to, float duration)
        {
            if (overlayImage == null || duration <= 0f)
            {
                SetOverlayAlpha(to);
                yield break;
            }

            float elapsed = 0f;
            Color c = overlayImage.color;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                c.a = Mathf.Lerp(from, to, t);
                overlayImage.color = c;
                yield return null;
            }

            c.a = to;
            overlayImage.color = c;
        }
    }
}
