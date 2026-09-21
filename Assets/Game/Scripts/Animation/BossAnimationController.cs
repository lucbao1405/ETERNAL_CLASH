using System;
using Spine;
using Spine.Unity;
using UnityEngine;
using EternalClash.Enemy;

namespace EternalClash.Animation
{
    /// <summary>
    /// Spine animation layer cho Boss (Assets/Game/Animations/boss/spine_SkeletonData.asset).
    ///
    /// Cac clip co san: "run", "stand", "shot" (danh), "khieu khich" (dieu cau),
    /// "damage" (trung don), "dead" (chet).
    ///
    /// Pure presentation layer: chi phan hoi tin hieu gameplay (BossController goi
    /// PlayTaunt, EnemyAttack goi NotifyAttack...), khong bao gio doi damage/timing.
    /// Loop co so chay theo van toc that: di -> "run", dung yen -> "stand".
    /// Boss luon quay mat ve phia Player (chay lui van mat ve player nhu Postknight).
    /// </summary>
    public class BossAnimationController : MonoBehaviour, IEnemyAnimationFeedback, IEnemyDeathVisual
    {
        [Header("Spine")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        [Header("Locomotion")]
        [SerializeField] private float moveSpeedThreshold = 0.45f;

        [Header("Facing")]
        [SerializeField, Tooltip("True neu skeleton mac dinh quay mat sang phai (scale.x duong).")]
        private bool facesRightByDefault;

        private const string AnimRun = "run";
        private const string AnimStand = "stand";
        private const string AnimAttack = "shot";
        private const string AnimTaunt = "khieu khich";
        private const string AnimHurt = "damage";
        private const string AnimDeath = "dead";

        private enum VisualState
        {
            None,
            Attack,
            Taunt,
            Hurt,
            Death
        }

        private EnemyHealthSystem healthSystem;
        private Transform player;
        private Transform visualRoot;
        private Vector3 lastPosition;
        private float baseScaleX = 1f;
        private int facingSign = 1;
        private float smoothSpeed;
        private int lastHealth = -1;
        private int animVersion;
        private bool subscribed;
        private bool dead;
        private bool moving;
        private bool faceAway; // true khi dang chay lui: quay mat di thi run moi tu nhien
        private VisualState state = VisualState.None;
        private string currentBase;

        // Animator (Boss.controller) chi la MIRROR de xem/canh ba trang thai trong
        // Animator window - clip Spine van do layer nay dieu khien truc tiep.
        private Animator animator;

        private void Awake()
        {
            Resolve();
        }

        private void Start()
        {
            lastPosition = transform.position;
            EnsureSubscribed();
        }

        private void OnEnable()
        {
            EnsureSubscribed();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (dead || skeletonAnimation == null)
                return;

            UpdateMotion();
            UpdateFacing();

            if (animator != null)
                animator.SetBool("Move", moving);

            if (state == VisualState.None)
                PlayBaseLoop();
        }

        private void Resolve()
        {
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();

            if (skeletonAnimation != null)
            {
                visualRoot = skeletonAnimation.transform;
                baseScaleX = Mathf.Abs(visualRoot.localScale.x);
            }

            if (healthSystem == null)
                healthSystem = GetComponent<EnemyHealthSystem>();

            if (animator == null)
                animator = GetComponent<Animator>();
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

        // ----- Gameplay feedback (animation layer only) --------------------

        public void NotifyAttack()
        {
            if (dead || state == VisualState.Death)
                return;
            PlayOneShot(AnimAttack, VisualState.Attack);
            SetAnimatorTrigger("Attack");
        }

        public float GetAttackDuration()
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null ||
                skeletonAnimation.Skeleton.Data == null)
                return 0f;

            Spine.Animation clip = skeletonAnimation.Skeleton.Data.FindAnimation(AnimAttack);
            return clip != null ? clip.Duration : 0f;
        }

        public void NotifyHurt()
        {
            // Chi play clip thuong khi boss dang dung yen; khi dang run (lui/vao)
            // bo qua de khong ngat clip di chuyen.
            if (dead || state != VisualState.None || moving)
                return;
            PlayOneShot(AnimHurt, VisualState.Hurt);
            SetAnimatorTrigger("Hurt");
        }

        /// <summary>
        /// Play clip khieu khich (goi boi BossController khi boss dung ngoai man hinh).
        /// Tra ve do dai clip de BossController cho dung het roi moi lao vao.
        /// </summary>
        /// <summary>
        /// Boss luon quay mat ve phia Player, TRU khi dang chay lui (BossController goi
        /// voi true) thi quay mat di cho clip run tu nhien, khieu khich lai quay vao.
        /// </summary>
        public void SetFacingAway(bool value)
        {
            faceAway = value;
        }

        public float PlayTaunt()
        {
            if (dead || state == VisualState.Death)
                return 0f;

            if (skeletonAnimation == null || !HasAnimation(AnimTaunt))
                return 0f;

            state = VisualState.Taunt;
            currentBase = null;
            SetAnimatorTrigger("Taunt");

            int version = ++animVersion;
            TrackEntry entry = skeletonAnimation.AnimationState.SetAnimation(0, AnimTaunt, false);

            float duration = 1.2f;
            if (entry != null)
            {
                if (entry.Animation != null && entry.Animation.Duration > 0f)
                    duration = entry.Animation.Duration;
                entry.Complete += _ => OnOneShotComplete(version, VisualState.Taunt);
            }

            return duration;
        }

        /// <summary>Plays the death clip and returns its length (seconds).</summary>
        public float PlayDeath()
        {
            if (dead)
                return 0f;

            dead = true;
            state = VisualState.Death;
            Unsubscribe();
            SetAnimatorTrigger("Dead");

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

        private void UpdateMotion()
        {
            Vector3 position = transform.position;
            float frameDelta = (position - lastPosition).magnitude;
            lastPosition = position;

            float measured = frameDelta / Mathf.Max(Time.deltaTime, 1e-4f);
            float alpha = 1f - Mathf.Exp(-Time.deltaTime * 10f);
            smoothSpeed = Mathf.Lerp(smoothSpeed, measured, alpha);
            moving = smoothSpeed >= moveSpeedThreshold;
        }

        private void UpdateFacing()
        {
            if (visualRoot == null)
                return;

            if (player == null)
            {
                GameObject obj = GameObject.FindGameObjectWithTag("Player");
                player = obj != null ? obj.transform : null;
            }

            if (player == null)
                return;

            float dx = player.position.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.05f)
                return;

            int sign = dx >= 0f ? 1 : -1;
            if (faceAway)
                sign = -sign;
            if (!facesRightByDefault)
                sign = -sign;
            if (sign == facingSign)
                return;

            facingSign = sign;
            Vector3 scale = visualRoot.localScale;
            scale.x = baseScaleX * sign;
            visualRoot.localScale = scale;
        }

        private bool HasAnimation(string name)
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null ||
                skeletonAnimation.Skeleton.Data == null)
                return false;
            return skeletonAnimation.Skeleton.Data.FindAnimation(name) != null;
        }

        private void SetAnimatorTrigger(string name)
        {
            if (animator != null)
                animator.SetTrigger(name);
        }

        /// <summary>Loop co so: di -> "run", dung -> "stand". Chi doi clip khi khac hien tai.</summary>
        private void PlayBaseLoop()
        {
            string clip = moving ? AnimRun : AnimStand;
            if (string.Equals(currentBase, clip, StringComparison.Ordinal))
                return;

            if (!HasAnimation(clip))
                return;

            currentBase = clip;
            animVersion++;
            skeletonAnimation.AnimationState.SetAnimation(0, clip, true);
        }

        private void PlayOneShot(string name, VisualState visualState)
        {
            if (skeletonAnimation == null || !HasAnimation(name))
                return;

            if (dead || state == VisualState.Death)
                return;

            state = visualState;
            currentBase = null;

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

            state = VisualState.None; // Update se tu play lai loop co so
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
