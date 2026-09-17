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

        [Header("Chest Open Background")]
        [SerializeField] private Image chestopenbg;
        [SerializeField] private float chestopenbgFadeInDuration = 0.3f;
        [SerializeField] private float chestopenbgFadeOutDuration = 0.5f;

        [Header("Rarity Flash")]
        [SerializeField] private Image rarityFlashImage;
        [SerializeField] private float rarityFlashDuration = 1f;
        [SerializeField] private float rarityFlashFadeIn = 0.2f;
        [SerializeField] private float rarityFlashFadeOut = 0.3f;

        [Header("Sprite Swap")]
        [SerializeField] private Sprite closedChestSprite;
        [SerializeField] private Sprite openedChestSprite;

        [Header("Chest Scale")]
        [SerializeField] private float openScaleDuration = 0.4f;
        [SerializeField] private float targetOpenScale = 1.4f;

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

            if (overlayImage != null)
            {
                overlayImage.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f);
                overlayImage.raycastTarget = false;
            }

            if (overlayCanvas != null)
                overlayCanvas.enabled = false;
        }

        public void SetChestOpenBg(Image bg)
        {
            chestopenbg = bg;
        }

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
            image.color = Color.clear;

            GameObject rarityFlashObject = new GameObject("RarityFlash");
            rarityFlashObject.transform.SetParent(root.transform, false);
            Image rarityFlashImg = rarityFlashObject.AddComponent<Image>();
            RectTransform flashRt = (RectTransform)rarityFlashObject.transform;
            flashRt.anchorMin = new Vector2(0.5f, 0.5f);
            flashRt.anchorMax = new Vector2(0.5f, 0.5f);
            flashRt.pivot = new Vector2(0.5f, 0.5f);
            flashRt.offsetMin = new Vector2(-220f, -220f);
            flashRt.offsetMax = new Vector2(220f, 220f);
            flashRt.localScale = Vector3.one;
            rarityFlashImg.color = new Color(1f, 1f, 1f, 0f);

            ChestOpenEffectController controller = root.AddComponent<ChestOpenEffectController>();
            controller.overlayCanvas = canvas;
            controller.overlayImage = image;
            controller.rarityFlashImage = rarityFlashImg;
            return controller;
        }

        public void Configure(Sprite closed, Sprite opened)
        {
            closedChestSprite = closed;
            openedChestSprite = opened;
        }

        public IEnumerator PlayEffect(ChestController chest, Color rarityColor)
        {
            if (IsRunning)
                yield break;

            IsRunning = true;

            if (overlayCanvas != null)
                overlayCanvas.enabled = true;

            if (overlayImage != null)
            {
                overlayImage.raycastTarget = true;
                SetOverlayAlpha(0f);
            }

            if (chestopenbg != null)
            {
                // ChestRewardUI hides this image while OPEN is shown; bring it back
                // so the fade-in/out of the chest-open background is actually visible.
                if (chestopenbg.gameObject != null)
                    chestopenbg.gameObject.SetActive(true);

                SetChestOpenBgAlpha(0f);
                chestopenbg.raycastTarget = false;
            }

            if (rarityFlashImage != null)
            {
                rarityFlashImage.enabled = true;
                rarityFlashImage.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0f);
                SetRarityFlashAlpha(0f);
                rarityFlashImage.raycastTarget = false;
            }

            if (chest != null)
                yield return StartCoroutine(chest.ScaleUpRoutine(openScaleDuration, targetOpenScale));

            if (chestopenbg != null)
                yield return StartCoroutine(FadeChestOpenBg(0f, 1f, chestopenbgFadeInDuration));

            if (rarityFlashImage != null)
                yield return StartCoroutine(FadeRarityFlash(0f, 0.8f, rarityFlashFadeIn));

            yield return new WaitForSeconds(rarityFlashDuration);

            if (rarityFlashImage != null)
                yield return StartCoroutine(FadeRarityFlash(0.8f, 0f, rarityFlashFadeOut));

            if (chest != null && openedChestSprite != null)
                chest.SetOpenedSpriteImmediate(openedChestSprite);

            if (chestopenbg != null)
                yield return StartCoroutine(FadeChestOpenBg(1f, 0f, chestopenbgFadeOutDuration));

            if (overlayImage != null)
            {
                SetOverlayAlpha(0f);
                overlayImage.raycastTarget = false;
            }

            if (rarityFlashImage != null)
            {
                SetRarityFlashAlpha(0f);
                rarityFlashImage.enabled = false;
            }

            if (overlayCanvas != null)
                overlayCanvas.enabled = false;

            IsRunning = false;
        }

        public void OpenChestEffect(ChestController chest)
        {
            StartCoroutine(PlayEffect(chest, Color.white));
        }

        private void SetOverlayAlpha(float a)
        {
            if (overlayImage == null) return;
            Color c = overlayImage.color;
            c.a = a;
            overlayImage.color = c;
        }

        private void SetChestOpenBgAlpha(float a)
        {
            if (chestopenbg == null) return;
            Color c = chestopenbg.color;
            c.a = a;
            chestopenbg.color = c;
        }

        private void SetRarityFlashAlpha(float a)
        {
            if (rarityFlashImage == null) return;
            Color c = rarityFlashImage.color;
            c.a = a;
            rarityFlashImage.color = c;
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

        private IEnumerator FadeChestOpenBg(float from, float to, float duration)
        {
            if (chestopenbg == null || duration <= 0f)
            {
                SetChestOpenBgAlpha(to);
                yield break;
            }

            float elapsed = 0f;
            Color c = chestopenbg.color;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                c.a = Mathf.Lerp(from, to, t);
                chestopenbg.color = c;
                yield return null;
            }

            c.a = to;
            chestopenbg.color = c;
        }

        private IEnumerator FadeRarityFlash(float from, float to, float duration)
        {
            if (rarityFlashImage == null || duration <= 0f)
            {
                SetRarityFlashAlpha(to);
                yield break;
            }

            float elapsed = 0f;
            Color c = rarityFlashImage.color;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                c.a = Mathf.Lerp(from, to, t);
                rarityFlashImage.color = c;
                yield return null;
            }

            c.a = to;
            rarityFlashImage.color = c;
        }
    }
}
