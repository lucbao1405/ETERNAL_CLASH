using System;
using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.UI
{
    /// <summary>Canvas-only chest reward flow. No world chest is instantiated.</summary>
    public class ChestOpenPopupController : MonoBehaviour
    {
        [SerializeField] private GameObject popup;
        [SerializeField] private SkeletonGraphic chestSkeleton;
        [SerializeField] private RectTransform itemDisplay;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemName;
        [SerializeField] private TextMeshProUGUI itemQuantity;
        [SerializeField] private Button tapButton;
        [SerializeField] private GameObject tapHint;
        [SerializeField] private float revealDuration = 0.45f;
        [SerializeField] private string closedAnimation = "ruong";
        [SerializeField] private string openAnimation = "open";
        [SerializeField] private string holdAnimation = "hold";

        public EternalClash.BattleResult.ChestState State { get; private set; } = EternalClash.BattleResult.ChestState.Complete;
        private readonly List<ItemReward> rewards = new List<ItemReward>();
        private Action onFinished;
        private int nextRewardIndex;
        private Coroutine revealRoutine;
        private TrackEntry openTrack;
        private bool tapBound;

        private void Awake() { ResolveReferences(); BindTap(); CloseChest(); }

        private void OnDestroy()
        {
            UnsubscribeOpenTrack();
            if (tapBound && tapButton != null) tapButton.onClick.RemoveListener(HandleTap);
        }

        public void Configure(GameObject ignoredPrefab, Transform ignoredSpawnPoint, GameObject popupRoot)
        {
            popup = popupRoot != null ? popupRoot : popup;
            ResolveReferences();
            BindTap();
        }

        public void BeginReward(IList<ItemReward> chestRewards, Action finished)
        {
            StopSequence();
            onFinished = finished;
            rewards.Clear();
            if (chestRewards != null)
                for (int i = 0; i < chestRewards.Count; i++)
                    if (chestRewards[i] != null && chestRewards[i].item != null && chestRewards[i].quantity > 0)
                        rewards.Add(chestRewards[i]);

            nextRewardIndex = 0;
            ResolveReferences();
            if (popup != null) { popup.SetActive(true); popup.transform.SetAsLastSibling(); }
            SetItemVisible(false);
            SetTapHint(true);
            State = EternalClash.BattleResult.ChestState.Closed;
            SetChestAnimation(closedAnimation, true);
        }

        public void ShowChest() { BeginReward(rewards, onFinished); }

        public void PlayOpen()
        {
            if (State != EternalClash.BattleResult.ChestState.Closed || chestSkeleton == null) return;
            State = EternalClash.BattleResult.ChestState.Opening;
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.ChestOpen);
            SetTapHint(false);
            UnsubscribeOpenTrack();
            openTrack = SetChestAnimation(openAnimation, false);
            if (openTrack == null) { PlayHold(); return; }
            openTrack.Complete += HandleOpenComplete;
        }

        public void PlayHold()
        {
            UnsubscribeOpenTrack();
            State = EternalClash.BattleResult.ChestState.Hold;
            SetChestAnimation(holdAnimation, true);
            SetTapHint(true);
        }

        public void RevealNextItem()
        {
            if (State != EternalClash.BattleResult.ChestState.Hold || revealRoutine != null) return;
            if (nextRewardIndex >= rewards.Count) { CloseChest(); return; }
            State = EternalClash.BattleResult.ChestState.Revealing;
            revealRoutine = StartCoroutine(RevealItem(rewards[nextRewardIndex++]));
        }

        public void CloseChest()
        {
            StopSequence();
            State = EternalClash.BattleResult.ChestState.Complete;
            if (popup != null) popup.SetActive(false);
            Action callback = onFinished;
            onFinished = null;
            callback?.Invoke();
        }

        public void CancelReward() { CloseChest(); }

        private void HandleTap()
        {
            if (State == EternalClash.BattleResult.ChestState.Closed) PlayOpen();
            else if (State == EternalClash.BattleResult.ChestState.Hold) RevealNextItem();
        }

        private void HandleOpenComplete(TrackEntry entry) { if (entry == openTrack) PlayHold(); }

        private IEnumerator RevealItem(ItemReward reward)
        {
            PopulateItem(reward);
            SetItemVisible(true);
            Vector2 origin = chestSkeleton != null ? chestSkeleton.rectTransform.anchoredPosition : Vector2.zero;
            Vector2 destination = origin + Vector2.up * 270f;
            CanvasGroup group = itemDisplay != null ? itemDisplay.GetComponent<CanvasGroup>() : null;
            if (itemDisplay != null)
            {
                itemDisplay.anchoredPosition = origin;
                itemDisplay.localScale = Vector3.zero;
                if (group == null) group = itemDisplay.gameObject.AddComponent<CanvasGroup>();
            }
            if (group != null) group.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, revealDuration));
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                if (itemDisplay != null)
                {
                    itemDisplay.anchoredPosition = Vector2.Lerp(origin, destination, eased) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 45f;
                    itemDisplay.localScale = Vector3.one * eased;
                }
                if (group != null) group.alpha = eased;
                yield return null;
            }
            if (itemDisplay != null) { itemDisplay.anchoredPosition = destination; itemDisplay.localScale = Vector3.one; }
            if (group != null) group.alpha = 1f;
            revealRoutine = null;
            if (nextRewardIndex >= rewards.Count) CloseChest();
            else State = EternalClash.BattleResult.ChestState.Hold;
        }

        private void ResolveReferences()
        {
            if (popup == null) popup = gameObject;
            Transform root = popup.transform;
            if (chestSkeleton == null) chestSkeleton = FindChildComponent<SkeletonGraphic>(root, "ChestSkeleton");
            if (itemDisplay == null) itemDisplay = FindChildRect(root, "ItemDisplay");
            if (itemIcon == null) itemIcon = FindChildComponent<Image>(root, "Icon");
            if (itemName == null) itemName = FindChildComponent<TextMeshProUGUI>(root, "ItemName") ?? FindChildComponent<TextMeshProUGUI>(root, "Name");
            if (itemQuantity == null) itemQuantity = FindChildComponent<TextMeshProUGUI>(root, "ItemQty") ?? FindChildComponent<TextMeshProUGUI>(root, "Quantity");
            if (tapButton == null) tapButton = FindChildComponent<Button>(root, "ClaimArea") ?? FindChildComponent<Button>(root, "OpenButton");
            if (tapHint == null) tapHint = FindChild(root, "TapHint")?.gameObject;
        }

        private void BindTap() { if (!tapBound && tapButton != null) { tapButton.onClick.AddListener(HandleTap); tapBound = true; } }

        private void PopulateItem(ItemReward reward)
        {
            Sprite icon = reward.item.icon;
            if (icon == null && !string.IsNullOrEmpty(reward.item.iconSpriteName)) icon = Resources.Load<Sprite>(reward.item.iconSpriteName);
            if (itemIcon != null) { itemIcon.sprite = icon; itemIcon.enabled = icon != null; }
            if (itemName != null) itemName.text = string.IsNullOrEmpty(reward.item.itemName) ? reward.item.itemId : reward.item.itemName;
            if (itemQuantity != null) itemQuantity.text = "x" + reward.quantity;
        }

        private TrackEntry SetChestAnimation(string name, bool loop)
        {
            if (chestSkeleton == null) return null;
            chestSkeleton.Initialize(false);
            if (chestSkeleton.AnimationState == null || chestSkeleton.SkeletonData == null || chestSkeleton.SkeletonData.FindAnimation(name) == null)
            { Debug.LogWarning("[ChestOpenPopupController] Missing Spine animation: " + name, this); return null; }
            return chestSkeleton.AnimationState.SetAnimation(0, name, loop);
        }

        private void StopSequence()
        {
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            revealRoutine = null;
            UnsubscribeOpenTrack();
            SetItemVisible(false);
            SetTapHint(false);
        }

        private void UnsubscribeOpenTrack() { if (openTrack != null) openTrack.Complete -= HandleOpenComplete; openTrack = null; }
        private void SetItemVisible(bool value) { if (itemDisplay != null) itemDisplay.gameObject.SetActive(value); }
        private void SetTapHint(bool value) { if (tapHint != null) tapHint.SetActive(value); }
        private static Transform FindChild(Transform root, string name) { if (root.name == name) return root; for (int i = 0; i < root.childCount; i++) { Transform found = FindChild(root.GetChild(i), name); if (found != null) return found; } return null; }
        private static T FindChildComponent<T>(Transform root, string name) where T : Component { Transform child = FindChild(root, name); return child != null ? child.GetComponent<T>() : null; }
        private static RectTransform FindChildRect(Transform root, string name) { return FindChildComponent<RectTransform>(root, name); }
    }
}
