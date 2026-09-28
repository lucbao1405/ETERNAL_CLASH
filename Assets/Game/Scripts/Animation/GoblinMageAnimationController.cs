using System;
using System.Collections;
using UnityEngine;
using Spine;
using Spine.Unity;
using EternalClash.Enemy;

namespace EternalClash.Animation
{
    public class GoblinMageAnimationController : MonoBehaviour, IEnemyAnimationFeedback, IEnemyDeathVisual
    {
        [Header("Spine")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        [Header("Locomotion")]
        [SerializeField, Range(0.1f, 1f)] private float idleTimeScale = 0.5f;

        [Header("Facing")]
        [SerializeField] private bool facePlayer = true;
        [SerializeField] private bool facesRightByDefault;

        [Header("Hurt Feedback")]
        [SerializeField] private Color hurtFlashColor = new Color(2.2f, 0.9f, 0.9f, 1f);
        [SerializeField] private float hurtFlashDuration = 0.2f;
        [SerializeField] private int hurtFlashPulses = 2;

        private const string AnimIdle = "stand";
        private const string AnimAttack = "magic shot";
        private const string AnimHurt = "damage";
        private const string AnimDeath = "dead";

        private enum VisualState { None, Attack, Hurt, Death }

        private EnemyHealthSystem healthSystem;
        private Transform player;
        private Transform visualRoot;
        private float baseScaleX = 1f;
        private int facingSign = 1;
        private float[] boundsBuffer;
        private bool visualFitApplied;
        private int lastHealth = -1;
        private int animVersion;
        private bool subscribed;
        private bool dead;
        private bool loopApplied;
        private VisualState state;
        private Coroutine flashRoutine;

        private void Awake() => Resolve();

        private void Start()
        {
            lastPosition = transform.position;
            EnsureSubscribed();
            PlayIdleLoop();
        }

        private void OnEnable()
        {
            EnsureSubscribed();
            if (!dead && state == VisualState.None && !loopApplied)
                PlayIdleLoop();
        }

        private void OnDisable() => Unsubscribe();

        private void Resolve()
        {
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null && autoResolveSkeleton)
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();

            if (skeletonAnimation != null)
            {
                visualRoot = skeletonAnimation.transform;
                baseScaleX = Mathf.Abs(visualRoot.localScale.x);
            }

            if (healthSystem == null)
                healthSystem = GetComponent<EnemyHealthSystem>();
        }

        private void EnsureSubscribed()
        {
            Resolve();
            if (subscribed) return;
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += OnHealthChanged;
                lastHealth = healthSystem.CurrentHealth;
            }
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            if (healthSystem != null)
                healthSystem.OnHealthChanged -= OnHealthChanged;
            subscribed = false;
        }

        private void Update()
        {
            if (dead || skeletonAnimation == null) return;
            UpdateFacing();
            if (state == VisualState.None && !loopApplied)
                PlayIdleLoop();
        }

        private void UpdateFacing()
        {
            if (!facePlayer || visualRoot == null) return;
            if (player == null)
            {
                GameObject go = GameObject.FindGameObjectWithTag("Player");
                player = go != null ? go.transform : null;
            }
            if (player == null) return;
            float dx = player.position.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.05f) return;
            int sign = dx >= 0f ? 1 : -1;
            if (!facesRightByDefault) sign = -sign;
            if (sign == facingSign) return;
            facingSign = sign;
            Vector3 scale = visualRoot.localScale;
            scale.x = baseScaleX * sign;
            visualRoot.localScale = scale;
        }

        public void NotifyAttack()
        {
            if (dead || state == VisualState.Death) return;
            PlayOneShot(AnimAttack, VisualState.Attack);
        }

        public float GetAttackDuration()
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null ||
                skeletonAnimation.Skeleton.Data == null) return 0f;
            Spine.Animation clip = skeletonAnimation.Skeleton.Data.FindAnimation(AnimAttack);
            return clip != null ? clip.Duration : 2f;
        }

        public void NotifyHurt()
        {
            if (dead || state == VisualState.Death) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(HurtFlashRoutine());
        }

        public float PlayDeath()
        {
            if (dead) return 0f;
            dead = true;
            state = VisualState.Death;
            Unsubscribe();
            if (flashRoutine != null) { StopCoroutine(flashRoutine); flashRoutine = null; RestoreSlotColors(); }
            if (skeletonAnimation == null || !HasAnimation(AnimDeath)) return 1f;
            animVersion++;
            TrackEntry entry = skeletonAnimation.AnimationState.SetAnimation(0, AnimDeath, false);
            if (entry == null || entry.Animation == null) return 1f;
            return entry.Animation.Duration > 0f ? entry.Animation.Duration : 1f;
        }

        private void PlayIdleLoop()
        {
            if (dead || skeletonAnimation == null) return;
            animVersion++;
            TrackEntry entry = skeletonAnimation.AnimationState.SetAnimation(0, AnimIdle, true);
            if (entry != null) entry.TimeScale = idleTimeScale;
            loopApplied = true;
        }

        private void PlayOneShot(string name, VisualState visualState)
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null ||
                skeletonAnimation.Skeleton.Data == null) return;
            if (skeletonAnimation.Skeleton.Data.FindAnimation(name) == null) return;
            state = visualState;
            loopApplied = false;
            int version = ++animVersion;
            TrackEntry entry = skeletonAnimation.AnimationState.SetAnimation(0, name, false);
            if (entry != null) entry.Complete += _ => OnOneShotComplete(version, visualState);
        }

        private void OnOneShotComplete(int version, VisualState visualState)
        {
            if (dead) return;
            if (version != animVersion || state != visualState) return;
            state = VisualState.None;
            PlayIdleLoop();
        }

        private IEnumerator HurtFlashRoutine()
        {
            Skeleton skeleton = skeletonAnimation?.Skeleton;
            if (skeleton == null || skeleton.Slots.Count == 0) yield break;
            int count = skeleton.Slots.Count;
            float[] baseR = new float[count], baseG = new float[count], baseB = new float[count];
            for (int i = 0; i < count; i++)
            {
                Slot slot = skeleton.Slots.Items[i];
                baseR[i] = slot.R; baseG[i] = slot.G; baseB[i] = slot.B;
            }
            int pulses = Mathf.Max(1, hurtFlashPulses);
            float pulseDuration = hurtFlashDuration / (pulses * 2f);
            float elapsed = 0f, total = hurtFlashDuration;
            while (elapsed < total)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / total;
                float intensity = t < 0.5f ? t * 2f : (1f - t) * 2f;
                intensity = Mathf.Clamp01(intensity);
                for (int i = 0; i < count; i++)
                {
                    Slot slot = skeleton.Slots.Items[i];
                    slot.R = Mathf.Lerp(baseR[i], hurtFlashColor.r, intensity);
                    slot.G = Mathf.Lerp(baseG[i], hurtFlashColor.g, intensity);
                    slot.B = Mathf.Lerp(baseB[i], hurtFlashColor.b, intensity);
                }
                yield return new WaitForSeconds(pulseDuration);
            }
            for (int i = 0; i < count; i++) { Slot slot = skeleton.Slots.Items[i]; slot.R = baseR[i]; slot.G = baseG[i]; slot.B = baseB[i]; }
            flashRoutine = null;
        }

        private void RestoreSlotColors()
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null) return;
            foreach (Slot slot in skeletonAnimation.Skeleton.Slots) { slot.R = 1f; slot.G = 1f; slot.B = 1f; }
        }

        private bool HasAnimation(string name)
        {
            return skeletonAnimation != null && skeletonAnimation.Skeleton != null &&
                   skeletonAnimation.Skeleton.Data != null &&
                   skeletonAnimation.Skeleton.Data.FindAnimation(name) != null;
        }

        private void OnHealthChanged(int current, int max)
        {
            if (dead) return;
            if (lastHealth >= 0 && current < lastHealth) NotifyHurt();
            lastHealth = current;
        }

        private Vector3 lastPosition;
        [SerializeField] private bool autoResolveSkeleton = true;
    }
}
