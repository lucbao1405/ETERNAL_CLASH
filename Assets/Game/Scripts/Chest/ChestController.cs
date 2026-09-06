using System;
using UnityEngine;
using System.Collections;
using EternalClash.Data;

namespace EternalClash.Chest
{
    public enum ChestState
    {
        Closed,
        Opening,
        Opened
    }

    /// <summary>
    /// World reward chest: floats to the center of the screen, waits for the player
    /// to tap it, then plays the open effect (ChestOpenEffectController) and swaps
    /// the closed sprite with the opened sprite.
    ///
    /// The reward itself is displayed/granted by the existing reward flow
    /// (ChestRewardUI -> StageCompleteController). This controller never grants rewards.
    /// </summary>
    public class ChestController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float floatToCenterDuration = 1.5f;
        [SerializeField] private float floatHeight = 0.3f;

        [Header("Open Effect")]
        [SerializeField] private float openScaleDuration = 0.4f;
        [SerializeField] private AnimationCurve openScaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1.2f);
        [SerializeField] private ParticleSystem openParticles;

        [Header("Chest Sprites")]
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openedSprite;
        [SerializeField] private SpriteRenderer chestSpriteRenderer;

        [Header("Interaction")]
        [Tooltip("Screen-space radius (pixels) used to detect a tap/click on the chest.")]
        [SerializeField] private float clickRadiusScreenPixels = 120f;

        public ChestState State { get; private set; } = ChestState.Closed;
        public bool opened { get; private set; }

        private RewardData currentReward;

        public bool IsReadyForInteraction => State == ChestState.Closed && arrivedAtCenter;

        /// Raised when the player taps the closed chest (used to show the open prompt UI).
        public event Action OnClicked;

        /// Raised once the opening effect finished and the sprite was swapped to opened.
        public event Action OnOpened;

        public Sprite ClosedSprite
        {
            get
            {
                if (chestSpriteRenderer != null && chestSpriteRenderer.sprite != null)
                    return chestSpriteRenderer.sprite;
                return closedSprite;
            }
        }

        public Sprite OpenedSprite => openedSprite;

        private Camera cam;
        private Vector3 startPosition;
        private Vector3 currentCenterTarget;
        private float floatTimer;
        private bool arrivedAtCenter;
        private bool clickedHandled;

        private void Awake()
        {
            cam = Camera.main;
            startPosition = transform.position;

            if (chestSpriteRenderer == null)
                chestSpriteRenderer = GetComponent<SpriteRenderer>();

            if (closedSprite == null && chestSpriteRenderer != null)
                closedSprite = chestSpriteRenderer.sprite;
        }

        private void Update()
        {
            if (State == ChestState.Closed && !arrivedAtCenter)
            {
                floatTimer += Time.deltaTime;
                float t = Mathf.Clamp01(floatTimer / floatToCenterDuration);
                MoveToCenter(t);

                if (t >= 1f)
                {
                    arrivedAtCenter = true;
                    transform.position = currentCenterTarget;
                }
                else
                {
                    BobAnimation();
                }
            }
            else if (State == ChestState.Closed && arrivedAtCenter)
            {
                TrackInteraction();
            }
        }

        private void MoveToCenter(float t)
        {
            if (cam == null) return;

            Vector3 screenCenter = new Vector3(0.5f, 0.5f, 10f);
            Vector3 worldCenter = cam.ViewportToWorldPoint(screenCenter);
            worldCenter.z = startPosition.z;

            currentCenterTarget = worldCenter;
            transform.position = Vector3.Lerp(startPosition, worldCenter, t);
        }

        private void BobAnimation()
        {
            if (floatTimer < 0f) return;
            float bob = Mathf.Sin(floatTimer * Mathf.PI * 1f) * floatHeight;
            transform.position = new Vector3(transform.position.x, currentCenterTarget.y + bob, transform.position.z);
        }

        private void TrackInteraction()
        {
            if (clickedHandled || cam == null) return;
            if (!Input.GetMouseButtonDown(0)) return;

            Vector3 chestScreen = cam.WorldToScreenPoint(transform.position);
            chestScreen.z = 0f;
            Vector2 mouseScreen = Input.mousePosition;

            if (Vector2.Distance(mouseScreen, chestScreen) <= clickRadiusScreenPixels)
            {
                clickedHandled = true;
                OnClicked?.Invoke();
            }
        }

        // Compatibility entry point kept for any existing caller.
        public void Open()
        {
            OpenChest();
        }

        /// <summary>
        /// Opens the chest. Safe to call more than once (double-open protected).
        /// Plays the chest open effect, swaps closed -> opened sprite and then
        /// fires <see cref="OnOpened"/> so the UI can reveal the reward.
        /// </summary>
        public void OpenChest()
        {
            if (opened || State == ChestState.Opening)
                return;

            // If OPEN was pressed while the chest was still floating in, snap it to
            // the center so the opening effect/sprite swap happen at the expected spot.
            if (State == ChestState.Closed && !arrivedAtCenter && cam != null)
            {
                Vector3 screenCenter = new Vector3(0.5f, 0.5f, 10f);
                Vector3 worldCenter = cam.ViewportToWorldPoint(screenCenter);
                worldCenter.z = startPosition.z;
                transform.position = worldCenter;
            }

            opened = true;
            clickedHandled = true;
            arrivedAtCenter = true;
            State = ChestState.Opening;

            StartCoroutine(OpenRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            ChestOpenEffectController effect = ChestOpenEffectController.Instance;
            Color rarityColor = GetRarityColor();

            if (effect != null)
            {
                effect.Configure(ClosedSprite, OpenedSprite);
                yield return StartCoroutine(effect.PlayEffect(this, rarityColor));
            }
            else
            {
                yield return StartCoroutine(PlayLocalOpenEffect());
            }

            ApplyOpenedSprite();

            PlayOpenParticles();

            State = ChestState.Opened;
            opened = true;

            Debug.Log("[CHEST] OPENED");
            OnOpened?.Invoke();
        }

        private IEnumerator PlayLocalOpenEffect()
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < openScaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / openScaleDuration);
                float scaleValue = openScaleCurve.Evaluate(t);
                transform.localScale = startScale * scaleValue;
                yield return null;
            }

            transform.localScale = startScale;
        }

        private void PlayOpenParticles()
        {
            if (openParticles != null)
                openParticles.Play();
        }

        private void ApplyOpenedSprite()
        {
            if (chestSpriteRenderer != null && openedSprite != null)
                chestSpriteRenderer.sprite = openedSprite;
        }

        /// <summary>
        /// Receives a reward icon sprite for display purposes. The icon is shown by the
        /// reward UI, not baked onto the chest sprite (the chest always swaps to its
        /// opened-chest sprite).
        /// </summary>
        public void SetRewardSprite(Sprite sprite)
        {
            // Intentionally a display hint only; reward visuals live in ChestRewardUI.
        }

        public void SetOpenedSpriteImmediate(Sprite sprite)
        {
            if (chestSpriteRenderer != null && sprite != null)
                chestSpriteRenderer.sprite = sprite;
        }

        public void SetRewardData(RewardData reward)
        {
            currentReward = reward;
        }

        private Color GetRarityColor()
        {
            if (currentReward == null)
                return Color.white;

            if (currentReward.HasItem)
                return RarityToColor(currentReward.item.rarity);

            switch (currentReward.type)
            {
                case RewardType.Gold:
                    return new Color(1f, 0.84f, 0f);
                case RewardType.Gem:
                    return new Color(0f, 0.8f, 1f);
                case RewardType.Material:
                    return new Color(0.5f, 0.8f, 0.5f);
                default:
                    return Color.white;
            }
        }

        private static Color RarityToColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common:
                    return new Color(0.7f, 0.7f, 0.7f);
                case ItemRarity.Uncommon:
                    return new Color(0.3f, 0.8f, 0.3f);
                case ItemRarity.Rare:
                    return new Color(0.2f, 0.5f, 1f);
                case ItemRarity.Epic:
                    return new Color(0.7f, 0.2f, 0.9f);
                case ItemRarity.Legendary:
                    return new Color(1f, 0.6f, 0.1f);
                default:
                    return Color.white;
            }
        }

        public IEnumerator ScaleUpRoutine(float duration, float targetScale)
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 endScale = startScale * targetScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveT = openScaleCurve.Evaluate(t);
                transform.localScale = Vector3.Lerp(startScale, endScale, curveT);
                yield return null;
            }

            transform.localScale = endScale;
        }
    }
}
