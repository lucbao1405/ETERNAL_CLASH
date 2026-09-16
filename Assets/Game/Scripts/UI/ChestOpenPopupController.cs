using System;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.UI
{
    /// <summary>
    /// Canvas-only chest reward flow. Mot lan cham: choi animation mo rong roi
    /// dong ngay -> callback -> win popup (vat pham hien tren win popup).
    /// Khong con phan tap tung vat pham bay ra.
    /// </summary>
    public class ChestOpenPopupController : MonoBehaviour
    {
        [SerializeField] private GameObject popup;
        [SerializeField] private SkeletonGraphic chestSkeleton;
        [SerializeField] private Button tapButton;
        [SerializeField] private GameObject tapHint;
        [SerializeField] private string closedAnimation = "ruong";
        [SerializeField] private string openAnimation = "open";

        public EternalClash.BattleResult.ChestState State { get; private set; } = EternalClash.BattleResult.ChestState.Complete;
        private Action onFinished;
        private TrackEntry openTrack;
        private bool tapBound;

        private void Awake()
        {
            ResolveReferences();
            BindTap();
            // Controller co the nam chung root voi BattleResultFlowController:
            // khong bao gio tat gameObject nay (se kill coroutine cua flow),
            // chi tat popup khi no la object khac.
            if (popup != null && popup != gameObject)
                popup.SetActive(false);
        }

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
            ResolveReferences();
            if (tapButton == null)
            {
                // ponytail: popup chua duoc wire (khong co ClaimArea) -> khong the
                // cho tap; bo qua man hinh ruong, ket thuc ngay de flow chay tiep
                // sang win popup thay vi treo vo thoi han. Upgrade path: wire popup.
                CloseChest();
                return;
            }
            if (popup != null) { popup.SetActive(true); popup.transform.SetAsLastSibling(); }
            SetTapHint(true);
            State = EternalClash.BattleResult.ChestState.Closed;
            SetChestAnimation(closedAnimation, true);
        }

        public void PlayOpen()
        {
            if (State != EternalClash.BattleResult.ChestState.Closed) return;
            State = EternalClash.BattleResult.ChestState.Opening;
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.ChestOpen);
            SetTapHint(false);
            UnsubscribeOpenTrack();
            openTrack = SetChestAnimation(openAnimation, false);
            if (openTrack == null) { CloseChest(); return; }
            openTrack.Complete += HandleOpenComplete;
        }

        public void CloseChest()
        {
            StopSequence();
            State = EternalClash.BattleResult.ChestState.Complete;
            if (popup != null && popup != gameObject) popup.SetActive(false);
            Action callback = onFinished;
            onFinished = null;
            callback?.Invoke();
        }

        public void CancelReward() { CloseChest(); }

        private void HandleTap()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            if (State == EternalClash.BattleResult.ChestState.Closed) PlayOpen();
        }

        private void HandleOpenComplete(TrackEntry entry) { if (entry == openTrack) CloseChest(); }

        private void ResolveReferences()
        {
            if (popup == null) popup = gameObject;
            Transform root = popup.transform;
            if (chestSkeleton == null) chestSkeleton = FindChildComponent<SkeletonGraphic>(root, "ChestSkeleton");
            if (tapButton == null) tapButton = FindChildComponent<Button>(root, "ClaimArea") ?? FindChildComponent<Button>(root, "OpenButton");
            if (tapHint == null) tapHint = FindChild(root, "TapHint")?.gameObject;
        }

        private void BindTap() { if (!tapBound && tapButton != null) { tapButton.onClick.AddListener(HandleTap); tapBound = true; } }

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
            UnsubscribeOpenTrack();
            SetTapHint(false);
        }

        private void UnsubscribeOpenTrack() { if (openTrack != null) openTrack.Complete -= HandleOpenComplete; openTrack = null; }
        private void SetTapHint(bool value) { if (tapHint != null) tapHint.SetActive(value); }
        private static Transform FindChild(Transform root, string name) { if (root.name == name) return root; for (int i = 0; i < root.childCount; i++) { Transform found = FindChild(root.GetChild(i), name); if (found != null) return found; } return null; }
        private static T FindChildComponent<T>(Transform root, string name) where T : Component { Transform child = FindChild(root, name); return child != null ? child.GetComponent<T>() : null; }
    }
}
