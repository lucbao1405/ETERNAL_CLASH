using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;

namespace EternalClash.UI
{
    /// <summary>Coordinates tap input for the popup chest and its chest-only rewards.</summary>
    public sealed class ChestRewardUI : MonoBehaviour
    {
        [Header("Chest")]
        [SerializeField] private Image chestImage;
        [SerializeField] private Sprite chestClosedSprite;
        [SerializeField] private Sprite chestOpenSprite;
        [SerializeField] private SkeletonGraphic chestSkeleton;
        [SerializeField] private ChestAnimationController chestAnimation;

        [Header("Item Display")]
        [SerializeField] private RectTransform itemDisplayRoot;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemQuantityText;
        [SerializeField] private RectTransform itemStartAnchor;
        [SerializeField] private RectTransform itemEndAnchor;
        [SerializeField] private ChestRewardRevealController revealController;

        [Header("Input")]
        [SerializeField] private Button claimButton;
        [SerializeField] private GameObject tapHint;
        [SerializeField, Min(0f)] private float inputLockDuration = 0.15f;
        [SerializeField, Min(0.01f)] private float flyDuration = 0.4f;

        public bool IsRevealing { get; private set; }

        private Action onFinished;
        private readonly List<ItemReward> pendingChestRewards = new List<ItemReward>();
        private float inputEnabledAt;
        private bool awaitingCloseTap;
        private bool buttonBound;

        private void Awake()
        {
            ResolveControllers();
            BindButton();
        }

        private void OnEnable()
        {
            ResolveControllers();
            BindButton();
        }

        private void OnDisable()
        {
            StopCurrentSequence();
        }

        private void OnDestroy()
        {
            if (buttonBound && claimButton != null)
                claimButton.onClick.RemoveListener(HandleTap);
            if (chestAnimation != null)
                chestAnimation.OpenCompleted -= HandleOpenComplete;
            if (revealController != null)
                revealController.AllItemsRevealed -= HandleAllItemsRevealed;
        }

        private void Update()
        {
            if (IsRevealing && claimButton == null && Input.GetMouseButtonDown(0))
                HandleTap();
        }

        public void ShowRewards(IList<ItemReward> chestRewards, Action finished)
        {
            ResolveControllers();
            StopCurrentSequence();
            onFinished = finished;
            IsRevealing = true;
            awaitingCloseTap = false;
            inputEnabledAt = Time.unscaledTime + inputLockDuration;
            pendingChestRewards.Clear();
            if (chestRewards != null)
            {
                for (int i = 0; i < chestRewards.Count; i++)
                    pendingChestRewards.Add(chestRewards[i]);
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (chestImage != null)
            {
                chestImage.sprite = chestClosedSprite;
                chestImage.enabled = chestSkeleton == null;
            }
            chestAnimation?.PlayClosed();
            SetTapHint(true);
        }

        public void Hide()
        {
            StopCurrentSequence();
            gameObject.SetActive(false);
        }

        private void HandleTap()
        {
            if (!IsRevealing || Time.unscaledTime < inputEnabledAt)
                return;

            if (chestAnimation != null && chestAnimation.State == ChestState.Closed)
            {
                SetTapHint(false);
                inputEnabledAt = float.PositiveInfinity;
                chestAnimation.PlayOpen();
                return;
            }

            if (chestAnimation == null || chestAnimation.State != ChestState.Hold)
                return;

            if (awaitingCloseTap)
            {
                Finish();
                return;
            }

            if (revealController == null || !revealController.RevealNextItem())
                awaitingCloseTap = true;
        }

        private void HandleOpenComplete()
        {
            if (!IsRevealing)
                return;

            if (chestImage != null)
            {
                chestImage.sprite = chestOpenSprite;
                chestImage.enabled = chestSkeleton == null;
            }
            revealController?.StartReveal(pendingChestRewards, chestAnimation);
            inputEnabledAt = Time.unscaledTime + inputLockDuration;
            SetTapHint(true);
        }

        private void HandleAllItemsRevealed()
        {
            awaitingCloseTap = true;
            inputEnabledAt = Time.unscaledTime + inputLockDuration;
            SetTapHint(true);
        }

        private void Finish()
        {
            IsRevealing = false;
            SetTapHint(false);
            Action callback = onFinished;
            onFinished = null;
            pendingChestRewards.Clear();
            callback?.Invoke();
        }

        private void StopCurrentSequence()
        {
            IsRevealing = false;
            awaitingCloseTap = false;
            inputEnabledAt = float.PositiveInfinity;
            onFinished = null;
            pendingChestRewards.Clear();
            revealController?.FinishReveal();
            SetTapHint(false);
        }

        private void ResolveControllers()
        {
            if (chestAnimation == null)
            {
                chestAnimation = GetComponent<ChestAnimationController>();
                if (chestAnimation == null)
                    chestAnimation = gameObject.AddComponent<ChestAnimationController>();
            }
            chestAnimation.Configure(chestSkeleton);
            if (revealController == null)
            {
                revealController = GetComponent<ChestRewardRevealController>();
                if (revealController == null)
                    revealController = gameObject.AddComponent<ChestRewardRevealController>();
            }

            // The item must originate inside the popup chest and land in the
            // display area. Older scene wiring did not serialize these optional
            // anchors, so use the authored popup objects as safe defaults.
            if (itemStartAnchor == null && chestSkeleton != null)
                itemStartAnchor = chestSkeleton.rectTransform;
            if (itemEndAnchor == null && itemDisplayRoot != null)
                itemEndAnchor = itemDisplayRoot;

            revealController.Configure(itemDisplayRoot, itemIcon, itemNameText, itemQuantityText,
                itemStartAnchor != null ? itemStartAnchor : chestSkeleton != null ? chestSkeleton.rectTransform : null,
                itemEndAnchor, flyDuration);
            chestAnimation.OpenCompleted -= HandleOpenComplete;
            chestAnimation.OpenCompleted += HandleOpenComplete;
            revealController.AllItemsRevealed -= HandleAllItemsRevealed;
            revealController.AllItemsRevealed += HandleAllItemsRevealed;
        }

        private void BindButton()
        {
            if (buttonBound || claimButton == null)
                return;
            claimButton.onClick.AddListener(HandleTap);
            buttonBound = true;
        }

        private void SetTapHint(bool visible)
        {
            if (tapHint != null)
                tapHint.SetActive(visible);
        }
    }
}
