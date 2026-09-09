using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Spine.Unity;

namespace EternalClash.UI
{
    /// <summary>
    /// Chest Reward Popup - điều khiển việc hiện item từ rương theo từng cái.
    ///
    /// STATE 1: Chest đóng (ShowClosed) -> STATE 2: Chest mở (ShowOpen) ->
    /// STATE 3: Hold. Khi Player tap, item hiện tại bay lên rồi chuyển sang item
    /// kế tiếp. Hết toàn bộ item -> gọi onFinished (= CompleteChestReward).
    ///
    /// KHÔNG tạo animation mới cho chest: rương đóng/mở chỉ là đổi sprite do
    /// Inspector gán. Item "bay lên" là UI tween đơn giản; nếu không gán các
    /// reference hiển thị item thì popup tự động chạy qua từng item (auto advance)
    /// để flow không bao giờ bị kẹt.
    /// </summary>
    public class ChestRewardUI : MonoBehaviour
    {
        [Header("Chest States")]
        [SerializeField] private Image chestImage;
        [SerializeField] private Sprite chestClosedSprite;
        [SerializeField] private Sprite chestOpenSprite;
        [SerializeField] private GameObject chestEffectRoot;

        // Dùng animation rương có sẵn (Assets/Game/Animations/chest - Spine). Khi gán
        // chestSkeleton thì rương đóng/mở được phát bằng animation thay vì đổi sprite.
        [SerializeField] private SkeletonGraphic chestSkeleton;
        [SerializeField] private string chestClosedAnim = "ruong";
        [SerializeField] private string chestOpenAnim = "open";

        [SerializeField] private float chestOpenDelay = 0.45f;

        [Header("Item Reveal")]
        [SerializeField] private RectTransform itemDisplayRoot;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemQuantityText;
        [SerializeField] private Button claimButton;
        [SerializeField] private GameObject tapHint;
        [SerializeField] private RectTransform itemStartAnchor;
        [SerializeField] private RectTransform itemEndAnchor;

        [Header("Timing")]
        [SerializeField] private float revealDelay = 0.35f;
        [SerializeField] private float claimLockDuration = 0.3f;
        [SerializeField] private float flyDuration = 0.4f;
        [SerializeField] private float autoAdvanceInterval = 0.9f;

        public bool IsRevealing { get; private set; }
        public bool HasItems => queue.Count > 0;

        private readonly List<ItemReward> queue = new List<ItemReward>();
        private int currentIndex = -1;
        private Action onFinished;
        private Coroutine revealRoutine;
        private Coroutine flyRoutine;
        private bool advanceRequested;
        private bool finishSent;
        private float tapEnabledAt = -1f;
        private bool buttonBound;

        private void Awake()
        {
            BindClaimButton();
        }

        private void OnDisable()
        {
            StopReveal();
        }

        private void BindClaimButton()
        {
            if (buttonBound) return;
            buttonBound = true;
            if (claimButton != null)
                claimButton.onClick.AddListener(OnClaimPressed);
        }

        // ------------------------------------------------------------------
        // Chest states
        // ------------------------------------------------------------------

        public void ShowClosed()
        {
            if (chestSkeleton != null && chestSkeleton.AnimationState != null)
            {
                chestSkeleton.gameObject.SetActive(true);
                if (chestImage != null)
                    chestImage.enabled = false;
                chestSkeleton.AnimationState.SetAnimation(0, chestClosedAnim, true);
            }
            else if (chestImage != null && chestClosedSprite != null)
            {
                chestImage.sprite = chestClosedSprite;
                chestImage.enabled = true;
            }

            if (chestEffectRoot != null)
                chestEffectRoot.SetActive(false);
            HideItemDisplay();
        }

        public void ShowOpen()
        {
            if (chestSkeleton != null && chestSkeleton.AnimationState != null)
            {
                chestSkeleton.gameObject.SetActive(true);
                if (chestImage != null)
                    chestImage.enabled = false;
                chestSkeleton.AnimationState.SetAnimation(0, chestOpenAnim, false);
            }
            else if (chestImage != null && chestOpenSprite != null)
            {
                chestImage.sprite = chestOpenSprite;
                chestImage.enabled = true;
            }

            if (chestEffectRoot != null)
                chestEffectRoot.SetActive(true);
        }

        // ------------------------------------------------------------------
        // Reveal flow
        // ------------------------------------------------------------------

        /// <summary>
        /// Hiện popup và reveal từng item. Mỗi item chờ Player tap (hoặc button)
        /// để chuyển sang item kế tiếp. Khi hết item -> onFinished được gọi.
        /// </summary>
        public void ShowRewards(IList<ItemReward> rewards, Action finished)
        {
            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                revealRoutine = null;
            }
            if (flyRoutine != null)
            {
                StopCoroutine(flyRoutine);
                flyRoutine = null;
            }

            queue.Clear();
            if (rewards != null)
                queue.AddRange(rewards);

            onFinished = finished;
            currentIndex = -1;
            finishSent = false;
            advanceRequested = false;
            tapEnabledAt = -1f;
            IsRevealing = true;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            revealRoutine = StartCoroutine(RevealRoutine());
        }

        public void Hide()
        {
            StopReveal();
            if (gameObject != null)
                gameObject.SetActive(false);
        }

        private IEnumerator RevealRoutine()
        {
            // STATE 1: chest đóng -> STATE 2: chest mở
            ShowClosed();
            if (HasChestVisuals() && chestOpenDelay > 0f)
                yield return new WaitForSecondsRealtime(chestOpenDelay);
            ShowOpen();
            ShowTapHint(false);

            if (queue.Count == 0)
            {
                Complete();
                yield break;
            }

            yield return new WaitForSecondsRealtime(revealDelay);

            for (currentIndex = 0; currentIndex < queue.Count; currentIndex++)
            {
                if (finishSent) yield break;
                ShowCurrentItem();

                yield return new WaitForSecondsRealtime(claimLockDuration);

                if (HasItemVisuals())
                {
                    tapEnabledAt = Time.unscaledTime;
                    advanceRequested = false;
                    ShowTapHint(true);

                    while (!advanceRequested && !finishSent)
                    {
                        if (claimButton == null &&
                            Input.GetMouseButtonDown(0) &&
                            Time.unscaledTime >= tapEnabledAt + 0.05f)
                        {
                            advanceRequested = true;
                        }
                        yield return null;
                    }

                    ShowTapHint(false);
                    if (finishSent) yield break;
                }
                else
                {
                    // Không gán visual item -> tự chạy qua item để flow không kẹt.
                    yield return new WaitForSecondsRealtime(autoAdvanceInterval);
                }
            }

            Complete();
        }

        private void ShowCurrentItem()
        {
            ItemReward entry = CurrentEntry();
            if (entry == null || entry.item == null)
            {
                HideItemDisplay();
                return;
            }

            if (itemDisplayRoot != null)
                itemDisplayRoot.gameObject.SetActive(true);

            if (itemIcon != null)
            {
                Sprite sprite = LoadItemIcon(entry);
                itemIcon.sprite = sprite;
                itemIcon.enabled = sprite != null;
            }

            if (itemNameText != null)
            {
                itemNameText.text = string.IsNullOrEmpty(entry.item.itemName)
                    ? entry.item.itemId
                    : entry.item.itemName;
                itemNameText.gameObject.SetActive(true);
            }

            if (itemQuantityText != null)
            {
                itemQuantityText.text = "x" + Mathf.Max(1, entry.quantity);
                itemQuantityText.gameObject.SetActive(true);
            }

            PlayFlyIn();
        }

        private void PlayFlyIn()
        {
            if (itemDisplayRoot == null) return;
            if (flyRoutine != null)
            {
                StopCoroutine(flyRoutine);
                flyRoutine = null;
            }

            Vector3 from;
            Vector3 to;
            if (itemStartAnchor != null && itemEndAnchor != null)
            {
                from = itemStartAnchor.position;
                to = itemEndAnchor.position;
            }
            else
            {
                from = itemDisplayRoot.position + Vector3.down * 120f;
                to = itemDisplayRoot.position;
            }

            flyRoutine = StartCoroutine(FlyTo(itemDisplayRoot, from, to));
        }

        private IEnumerator FlyTo(RectTransform target, Vector3 from, Vector3 to)
        {
            target.position = from;
            float elapsed = 0f;
            while (elapsed < flyDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / flyDuration));
                target.position = Vector3.Lerp(from, to, t);
                yield return null;
            }
            target.position = to;
        }

        private void HideItemDisplay()
        {
            if (itemDisplayRoot != null)
                itemDisplayRoot.gameObject.SetActive(false);
            if (itemIcon != null)
                itemIcon.enabled = false;
            if (itemNameText != null)
                itemNameText.gameObject.SetActive(false);
            if (itemQuantityText != null)
                itemQuantityText.gameObject.SetActive(false);
        }

        private bool HasItemVisuals()
        {
            return itemDisplayRoot != null ||
                   itemIcon != null ||
                   itemNameText != null ||
                   itemQuantityText != null;
        }

        private bool HasChestVisuals()
        {
            return chestImage != null || chestSkeleton != null ||
                   chestClosedSprite != null || chestOpenSprite != null;
        }

        private ItemReward CurrentEntry()
        {
            if (currentIndex < 0 || currentIndex >= queue.Count)
                return null;
            return queue[currentIndex];
        }

        private void OnClaimPressed()
        {
            if (!IsRevealing) return;
            if (Time.unscaledTime < tapEnabledAt) return;
            advanceRequested = true;
        }

        private void ShowTapHint(bool visible)
        {
            if (tapHint != null && tapHint.activeSelf != visible)
                tapHint.SetActive(visible);
        }

        private void Complete()
        {
            if (finishSent) return;
            finishSent = true;
            IsRevealing = false;
            ShowTapHint(false);

            Action callback = onFinished;
            onFinished = null;
            callback?.Invoke();
        }

        private void StopReveal(bool hideItems = true)
        {
            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                revealRoutine = null;
            }
            if (flyRoutine != null)
            {
                StopCoroutine(flyRoutine);
                flyRoutine = null;
            }
            IsRevealing = false;
            finishSent = true;
            if (hideItems)
                HideItemDisplay();
        }

        private static Sprite LoadItemIcon(ItemReward reward)
        {
            if (reward == null || reward.item == null)
                return null;
            if (string.IsNullOrEmpty(reward.item.iconSpriteName))
                return null;
            return Resources.Load<Sprite>(reward.item.iconSpriteName);
        }
    }
}
