using System.Collections;
using Spine;
using Spine.Unity;
using UnityEngine;
using EternalClash.Enemy;

namespace EternalClash.Animation
{
    public class SlimeAnimationController : MonoBehaviour, IEnemyAnimationFeedback, IEnemyDeathVisual
    {
        [Header("Spine")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private bool autoResolveSkeleton = true;

        [Header("Locomotion")]
        [SerializeField, Range(0.1f, 1f)] private float idleTimeScale = 0.35f;
        [SerializeField] private float moveSpeedThreshold = 0.45f;

        [Header("Visual Fitting")]
        [SerializeField] private bool autoFitSize = true;
        [SerializeField] private float targetVisualHeight = 0.85f;
        [SerializeField] private bool autoCenterOnRoot = true;

        [Header("Facing")]
        [SerializeField] private bool facePlayer = true;
        [SerializeField, Tooltip("True neu skeleton mac dinh quay mat sang phai (scale.x duong)")] private bool facesRightByDefault;

        [Header("Hurt Feedback")]
        [SerializeField] private Color hurtFlashColor = new Color(2.2f, 0.9f, 0.9f, 1f);
        [SerializeField] private float hurtFlashDuration = 0.2f;
        [SerializeField] private int hurtFlashPulses = 2;

        private const string AnimRun = "run";
        private const string AnimAttack = "attack";
        private const string AnimDeath = "die";

        private enum MotionState
        {
            Idle,
            Move
        }

        private enum VisualState
        {
            None,
            Attack,
            Death
        }

        private EnemyHealthSystem healthSystem;
        private Transform player;
        private Transform visualRoot;
        private Vector3 lastPosition;
        private float baseScaleX = 1f;
        private int facingSign = 1;
        private float smoothSpeed;
        private float[] boundsBuffer;
        private bool visualFitApplied;
        private int lastHealth = -1;
        private int animVersion;
        private bool subscribed;
        private bool dead;
        private bool loopApplied;
        private MotionState motion;
        private MotionState appliedMotion;
        private VisualState state;
        private Coroutine flashRoutine;

        private void Awake()
        {
            Resolve();
        }

        private void Start()
        {
            lastPosition = transform.position;
            EnsureSubscribed();
            PlayRunLoop();
        }

        private void OnEnable()
        {
            EnsureSubscribed();
            if (!dead && state == VisualState.None && !loopApplied)
                PlayRunLoop();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

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

            if (!visualFitApplied && skeletonAnimation.Skeleton != null)
                ApplyVisualFit();

            UpdateMotion();

            if (state == VisualState.None && (!loopApplied || appliedMotion != motion))
                PlayRunLoop();

            UpdateFacing();
        }

        private void ApplyVisualFit()
        {
            if (skeletonAnimation == null || visualRoot == null)
                return;

            Skeleton skeleton = skeletonAnimation.Skeleton;
            if (skeleton == null)
                return;

            float minX, minY, width, height;
            skeleton.GetBounds(out minX, out minY, out width, out height, ref boundsBuffer);
            if (width <= 0.0001f || height <= 0.0001f)
                return;

            Vector3 localScale = visualRoot.localScale;
            if (autoFitSize)
            {
                float worldHeight = height * Mathf.Abs(visualRoot.lossyScale.y);
                if (worldHeight > 0.0001f)
                {
                    float factor = targetVisualHeight / worldHeight;
                    localScale.x *= factor;
                    localScale.y *= factor;
                    localScale.z *= factor;
                    visualRoot.localScale = localScale;
                }
            }

            if (autoCenterOnRoot && visualRoot.parent != null)
            {
                skeleton.GetBounds(out minX, out minY, out width, out height, ref boundsBuffer);
                float centerX = minX + width * 0.5f;
                float centerY = minY + height * 0.5f;
                float worldOffsetX = centerX * Mathf.Abs(visualRoot.lossyScale.x);
                float worldOffsetY = centerY * Mathf.Abs(visualRoot.lossyScale.y);
                Vector3 parentScale = visualRoot.parent.lossyScale;
                Vector3 localPosition = visualRoot.localPosition;
                localPosition.x = -worldOffsetX / (Mathf.Abs(parentScale.x) + 0.0001f);
                localPosition.y = -worldOffsetY / (Mathf.Abs(parentScale.y) + 0.0001f);
                visualRoot.localPosition = localPosition;
            }

            baseScaleX = Mathf.Abs(visualRoot.localScale.x);
            visualFitApplied = true;
        }

        private void UpdateMotion()
        {
            Vector3 position = transform.position;
            float frameDelta = (position - lastPosition).magnitude;
            lastPosition = position;

            float measured = frameDelta / Mathf.Max(Time.deltaTime, 1e-4f);
            float alpha = 1f - Mathf.Exp(-Time.deltaTime * 10f);
            smoothSpeed = Mathf.Lerp(smoothSpeed, measured, alpha);

            motion = smoothSpeed >= moveSpeedThreshold ? MotionState.Move : MotionState.Idle;
        }

        private void UpdateFacing()
        {
            if (!facePlayer || visualRoot == null)
                return;

            if (player == null)
            {
                GameObject go = GameObject.FindGameObjectWithTag("Player");
                player = go != null ? go.transform : null;
            }

            if (player == null)
                return;

            float dx = player.position.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.05f)
                return;

            int sign = dx >= 0f ? 1 : -1;
            if (!facesRightByDefault)
                sign = -sign;
            if (sign == facingSign)
                return;

            facingSign = sign;
            Vector3 scale = visualRoot.localScale;
            scale.x = baseScaleX * sign;
            visualRoot.localScale = scale;
        }

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

            if (flashRoutine != null)
                StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(HurtFlashRoutine());
        }

        public float PlayDeath()
        {
            if (dead)
                return 0f;

            dead = true;
            state = VisualState.Death;
            Unsubscribe();

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
                RestoreSlotColors();
            }

            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null ||
                skeletonAnimation.Skeleton.Data == null)
                return 1.2f;

            if (skeletonAnimation.Skeleton.Data.FindAnimation(AnimDeath) == null)
                return 1.2f;

            animVersion++;
            TrackEntry entry = skeletonAnimation.AnimationState.SetAnimation(0, AnimDeath, false);
            if (entry == null || entry.Animation == null)
                return 1.2f;

            float duration = entry.Animation.Duration;
            return duration > 0f ? duration : 1.2f;
        }

        private void PlayRunLoop()
        {
            if (dead || skeletonAnimation == null)
                return;

            float timeScale = motion == MotionState.Move ? 1f : idleTimeScale;
            animVersion++;
            TrackEntry entry = skeletonAnimation.AnimationState.SetAnimation(0, AnimRun, true);
            if (entry != null)
                entry.TimeScale = timeScale;

            appliedMotion = motion;
            loopApplied = true;
        }

        private void PlayOneShot(string name, VisualState visualState)
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null ||
                skeletonAnimation.Skeleton.Data == null)
                return;

            if (skeletonAnimation.Skeleton.Data.FindAnimation(name) == null)
                return;

            state = visualState;
            loopApplied = false;

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
            PlayRunLoop();
        }

        private IEnumerator HurtFlashRoutine()
        {
            Skeleton skeleton = skeletonAnimation != null ? skeletonAnimation.Skeleton : null;
            if (skeleton == null || skeleton.Slots.Count == 0)
                yield break;

            int count = skeleton.Slots.Count;
            float[] baseR = new float[count];
            float[] baseG = new float[count];
            float[] baseB = new float[count];

            for (int i = 0; i < count; i++)
            {
                Slot slot = skeleton.Slots.Items[i];
                baseR[i] = slot.R;
                baseG[i] = slot.G;
                baseB[i] = slot.B;
            }

            int pulses = Mathf.Max(1, hurtFlashPulses);
            float pulseDuration = hurtFlashDuration / (pulses * 2f);
            float elapsed = 0f;
            float total = hurtFlashDuration;

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

            for (int i = 0; i < count; i++)
            {
                Slot slot = skeleton.Slots.Items[i];
                slot.R = baseR[i];
                slot.G = baseG[i];
                slot.B = baseB[i];
            }

            flashRoutine = null;
        }

        private void RestoreSlotColors()
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null)
                return;

            Skeleton skeleton = skeletonAnimation.Skeleton;
            for (int i = 0; i < skeleton.Slots.Count; i++)
            {
                Slot slot = skeleton.Slots.Items[i];
                slot.R = 1f;
                slot.G = 1f;
                slot.B = 1f;
            }
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
