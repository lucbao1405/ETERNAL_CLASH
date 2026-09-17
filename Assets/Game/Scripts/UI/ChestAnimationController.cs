using System;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace EternalClash.UI
{
    public enum ChestState
    {
        Closed,
        Open,
        Hold
    }

    /// <summary>Owns the popup chest's Spine state machine only.</summary>
    public sealed class ChestAnimationController : MonoBehaviour
    {
        [SerializeField] private SkeletonGraphic skeletonGraphic;
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private string closedAnimation = "ruong";
        [SerializeField] private string openAnimation = "open";
        [SerializeField] private string holdAnimation = "hold";

        public ChestState State { get; private set; } = ChestState.Closed;
        public bool IsOpening { get; private set; }
        public event Action OpenCompleted;

        private TrackEntry openTrack;

        public void Configure(SkeletonGraphic graphic)
        {
            skeletonGraphic = graphic;
        }

        private void Awake()
        {
            ResolveSkeleton();
        }

        private void OnDestroy()
        {
            UnsubscribeOpenTrack();
        }

        public void PlayClosed()
        {
            UnsubscribeOpenTrack();
            IsOpening = false;
            State = ChestState.Closed;
            SetAnimation(closedAnimation, true);
        }

        public bool PlayOpen()
        {
            if (State != ChestState.Closed || IsOpening)
                return false;

            UnsubscribeOpenTrack();
            IsOpening = true;
            State = ChestState.Open;
            openTrack = SetAnimation(openAnimation, false);
            if (openTrack == null)
            {
                IsOpening = false;
                PlayHold();
                OpenCompleted?.Invoke();
                return false;
            }

            openTrack.Complete += HandleOpenComplete;
            return true;
        }

        public void PlayHold()
        {
            UnsubscribeOpenTrack();
            IsOpening = false;
            State = ChestState.Hold;
            SetAnimation(holdAnimation, true);
        }

        private void HandleOpenComplete(TrackEntry entry)
        {
            if (entry != openTrack)
                return;

            PlayHold();
            OpenCompleted?.Invoke();
        }

        private TrackEntry SetAnimation(string animationName, bool loop)
        {
            ResolveSkeleton();
            if (string.IsNullOrEmpty(animationName))
                return null;

            if (skeletonGraphic != null)
            {
                skeletonGraphic.Initialize(false);
                if (skeletonGraphic.AnimationState != null &&
                    skeletonGraphic.SkeletonData != null &&
                    skeletonGraphic.SkeletonData.FindAnimation(animationName) != null)
                    return skeletonGraphic.AnimationState.SetAnimation(0, animationName, loop);
            }

            if (skeletonAnimation != null)
            {
                skeletonAnimation.Initialize(false);
                if (skeletonAnimation.AnimationState != null)
                    return skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
            }

            Debug.LogWarning("[ChestAnimationController] Missing Spine animation: " + animationName, this);
            return null;
        }

        private void ResolveSkeleton()
        {
            if (skeletonGraphic == null)
                skeletonGraphic = GetComponent<SkeletonGraphic>();
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        private void UnsubscribeOpenTrack()
        {
            if (openTrack != null)
                openTrack.Complete -= HandleOpenComplete;
            openTrack = null;
        }
    }
}
