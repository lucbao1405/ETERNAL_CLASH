using System;
using UnityEngine;
using Spine;
using Spine.Unity;
using EternalClash.Enemy;

namespace EternalClash.Animation
{
    /// <summary>
    /// Spine animation layer for the Wolf enemy
    /// (EternalClash.Animation.Enemy/Wolf/spine_SkeletonData.asset).
    ///
    /// Pure presentation layer: reacts to enemy movement / attack / hurt /
    /// death signals and never changes combat logic.
    ///
    /// Available Wolf spine animations:
    ///   Run = "run", Attack = "bite", Hurt = "damage", Death = "dead".
    /// </summary>
    public class WolfAnimationController : MonoBehaviour, IEnemyAnimationFeedback, IEnemyDeathVisual
    {
        [Header("Spine")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        private const string AnimRun = "run";
        private const string AnimAttack = "bite";
        private const string AnimHurt = "damage";
        private const string AnimDeath = "dead";

        private enum VisualState
        {
            None,
            Attack,
            Hurt,
            Death
        }

        private EnemyHealthSystem healthSystem;
        private int lastHealth = -1;
        private int animVersion;
        private VisualState state = VisualState.None;
        private bool subscribed;
        private bool dead;
        private string lastBaseAnim;

        private void Awake()
        {
            Resolve();
        }

        private void Start()
        {
            EnsureSubscribed();
            if (state == VisualState.None && !dead)
                PlayBaseLoop();
        }

        private void OnEnable()
        {
            EnsureSubscribed();
            if (state == VisualState.None && !dead)
                PlayBaseLoop();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Resolve()
        {
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();

            if (healthSystem == null)
                healthSystem = GetComponent<EnemyHealthSystem>();
        }

        private void EnsureSubscribed()
        {
            Resolve();
            if (subscribed)
                return;

            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += OnHealthChanged;
                lastHealth = healthSystem.CurrentHealth;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            if (healthSystem != null)
                healthSystem.OnHealthChanged -= OnHealthChanged;

            subscribed = false;
        }

        private void Update()
        {
            if (dead || skeletonAnimation == null)
                return;

            if (state == VisualState.None)
            {
                string baseAnim = AnimRun;
                if (!string.Equals(lastBaseAnim, baseAnim, StringComparison.Ordinal))
                {
                    lastBaseAnim = baseAnim;
                    PlayLoop(baseAnim);
                }
            }
        }

        // ----- Gameplay feedback (animation layer only) --------------------

        public void NotifyAttack()
        {
            if (dead || state == VisualState.Death)
                return;
            PlayOneShot(AnimAttack, VisualState.Attack);
        }

        public void NotifyHurt()
        {
            if (dead || state == VisualState.Death)
                return;
            PlayOneShot(AnimHurt, VisualState.Hurt);
        }

        /// <summary>Plays the death clip and returns its length (seconds).</summary>
        public float PlayDeath()
        {
            if (dead)
                return 0f;

            dead = true;
            state = VisualState.Death;
            Unsubscribe();

            if (skeletonAnimation == null || !HasAnimation(AnimDeath))
                return 1f;

            animVersion++;
            TrackEntry entry = skeletonAnimation.AnimationState.SetAnimation(0, AnimDeath, false);
            if (entry == null || entry.Animation == null)
                return 1f;

            float duration = entry.Animation.Duration;
            return duration > 0f ? duration : 1f;
        }

        // ----- Spine helpers ----------------------------------------------

        private bool HasAnimation(string name)
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null ||
                skeletonAnimation.Skeleton.Data == null)
                return false;
            return skeletonAnimation.Skeleton.Data.FindAnimation(name) != null;
        }

        private void PlayLoop(string name)
        {
            if (!HasAnimation(name))
                return;
            animVersion++;
            skeletonAnimation.AnimationState.SetAnimation(0, name, true);
        }

        private void PlayOneShot(string name, VisualState visualState)
        {
            if (skeletonAnimation == null)
                return;

            if (dead || state == VisualState.Death)
                return;

            state = visualState;

            if (!HasAnimation(name))
            {
                state = VisualState.None;
                return;
            }

            int version = ++animVersion;
            TrackEntry entry = skeletonAnimation.AnimationState.SetAnimation(0, name, false);
            if (entry != null)
                entry.Complete += _ => OnOneShotComplete(version, visualState);
        }

        private void OnOneShotComplete(int version, VisualState visualState)
        {
            if (dead)
                return;
            if (version != animVersion || state != visualState)
                return;
            state = VisualState.None;
            PlayBaseLoop();
        }

        private void PlayBaseLoop()
        {
            if (dead)
                return;
            if (skeletonAnimation == null)
                return;

            lastBaseAnim = AnimRun;
            PlayLoop(AnimRun);
        }

        private void OnHealthChanged(int current, int max)
        {
            if (dead)
                return;

            if (lastHealth >= 0 && current < lastHealth)
                NotifyHurt();

            lastHealth = current;
        }
    }
}
