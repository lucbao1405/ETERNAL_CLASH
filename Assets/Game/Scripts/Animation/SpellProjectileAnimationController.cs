using UnityEngine;
using Spine;
using Spine.Unity;

namespace EternalClash.Animation
{
    public class SpellProjectileAnimationController : MonoBehaviour
    {
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        private void Awake()
        {
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        private void Start()
        {
            PlayLoop();
        }

        private void PlayLoop()
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null ||
                skeletonAnimation.Skeleton.Data == null) return;

            // "bay" la clip cua Spell; projectile khac (vd Bullet chi co "shot")
            // tu quay ve clip dau tien cua skeleton.
            string animName = "bay";
            if (skeletonAnimation.Skeleton.Data.FindAnimation(animName) == null)
                animName = FindFirstAnimation();
            if (animName == null) return;
            skeletonAnimation.AnimationState.SetAnimation(0, animName, true);
        }

        private string FindFirstAnimation()
        {
            var anims = skeletonAnimation.Skeleton.Data.Animations;
            if (anims.Count > 0) return anims.Items[0].Name;
            return null;
        }
    }
}
