using System;
using UnityEngine;
using Spine;
using Spine.Unity;
using EternalClash.Character;
using EternalClash.Player;
using EternalClash.Skill;

namespace EternalClash.Animation
{
    /// <summary>
    /// Spine animation layer for the Player (EternalClash.Animation.Player/spine_SkeletonData.asset).
    ///
    /// Pure presentation layer: it only reacts to gameplay signals and never
    /// changes combat/damage/skill behaviour.
    ///
    /// Available Player spine animations (bo Spine moi, commit "nhanvatchinh"):
    ///   Idle = "stand", Run = "run", Attack = "attack", Charge = "charge",
    ///   Shield = "shield", Potion = "healing", Hurt = "hit", Death = "dead",
    ///   Victory = "victory".
    /// </summary>
    public class PlayerAnimationController : MonoBehaviour, IPlayerAnimationFeedback
    {
        [Header("Spine")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        private const string AnimIdle = "stand";
        private const string AnimRun = "run";
        private const string AnimAttack = "attack";
        private const string AnimCharge = "charge";
        private const string AnimShield = "shield";
        private const string AnimPotion = "healing";
        private const string AnimHurt = "hit";
        private const string AnimDeath = "dead";
        private const string AnimVictory = "victory";

        private enum VisualState
        {
            None,
            Attack,
            Charge,
            Shield,
            Potion,
            Hurt,
            Death,
            Victory
        }

        private HealthSystem healthSystem;
        private AutoRunner autoRunner;
        private readonly System.Collections.Generic.List<SkillBase> boundSkills =
            new System.Collections.Generic.List<SkillBase>();
        private ShieldSkill shieldSource;
        private int lastHealth = -1;
        private int animVersion;
        private VisualState state = VisualState.None;
        private bool subscribed;
        private bool resolved;
        private bool dead;
        private string lastBaseAnim;

        private void Awake()
        {
            Resolve();
        }

        private void Start()
        {
            EnsureSubscribed();
            if (state == VisualState.None)
                PlayBaseLoop();
        }

        private void OnEnable()
        {
            EnsureSubscribed();
            if (state == VisualState.None)
                PlayBaseLoop();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Resolve()
        {
            if (resolved)
                return;

            if (skeletonAnimation == null)
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();

            healthSystem = GetComponent<HealthSystem>();
            autoRunner = GetComponent<AutoRunner>();

            resolved = true;
        }

        private void EnsureSubscribed()
        {
            Resolve();
            if (subscribed)
                return;

            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += OnHealthChanged;
                healthSystem.OnDeath += OnDeath;
                lastHealth = healthSystem.CurrentHealth;
            }

            // Bind to every skill instance under the Player so whichever
            // skill is actually executed (via SkillManager) fires its event.
            boundSkills.Clear();
            SkillBase[] skills = GetComponentsInChildren<SkillBase>(true);
            foreach (SkillBase skill in skills)
            {
                if (skill == null)
                    continue;
                boundSkills.Add(skill);
                skill.SkillExecutedSource += OnSkillExecutedSource;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged -= OnHealthChanged;
                healthSystem.OnDeath -= OnDeath;
            }

            foreach (SkillBase skill in boundSkills)
            {
                if (skill != null)
                    skill.SkillExecutedSource -= OnSkillExecutedSource;
            }
            boundSkills.Clear();

            subscribed = false;
        }

        private void Update()
        {
            if (dead || state == VisualState.Victory || skeletonAnimation == null)
                return;

            // Shield pose is a looping pose driven by the skill duration.
            if (state == VisualState.Shield)
            {
                if (shieldSource == null || !shieldSource.Active)
                {
                    shieldSource = null;
                    state = VisualState.None;
                    PlayBaseLoop();
                }
                return;
            }

            // Loop base (Run / Idle) while nothing else is playing.
            if (state == VisualState.None)
            {
                string baseAnim = GetBaseAnimation();
                if (!string.Equals(lastBaseAnim, baseAnim, StringComparison.Ordinal))
                {
                    lastBaseAnim = baseAnim;
                    PlayLoop(baseAnim);
                }
            }
        }

        private string GetBaseAnimation()
        {
            // Running while the world auto-scrolls, standing still in combat.
            if (autoRunner != null && !autoRunner.IsRunning)
                return AnimIdle;
            return AnimRun;
        }

        // ----- Gameplay feedback (animation layer only) --------------------

        /// <summary>Chet hoac thang man: giu nguyen animation cuoi, khong doi nua.</summary>
        private bool IsFinished => dead || state == VisualState.Death || state == VisualState.Victory;

        public void NotifyAttack(float speedScale = 1f)
        {
            if (IsFinished)
                return;
            if (state == VisualState.Shield || state == VisualState.Charge)
                return; // giu tu the do khien / luot kiem
            float clipDuration = GetAttackDuration();
            // speedScale > 1: cho clip gan xong som hon de don ke tiep kip nhau (mash).
            float fitDuration = speedScale > 1f && clipDuration > 0f ? clipDuration / speedScale : 0f;
            PlayOneShot(AnimAttack, VisualState.Attack, fitDuration);
        }

        public float GetAttackDuration()
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null ||
                skeletonAnimation.Skeleton.Data == null)
                return 0f;
            Spine.Animation clip = skeletonAnimation.Skeleton.Data.FindAnimation(AnimAttack);
            return clip != null ? clip.Duration : 0f;
        }

        public void NotifyPotion()
        {
            if (IsFinished)
                return;
            if (state == VisualState.Shield || state == VisualState.Charge)
                return;
            PlayOneShot(AnimPotion, VisualState.Potion);
        }

        public void NotifyCharge(float chargeDuration)
        {
            if (IsFinished)
                return;

            shieldSource = null;
            // Clip "charge" dai ~2.1s, tang toc cho vua khit thoi gian luot cua skill.
            PlayOneShot(AnimCharge, VisualState.Charge, chargeDuration);
        }

        public void NotifyShield(SkillBase source)
        {
            if (IsFinished)
                return;

            shieldSource = source as ShieldSkill;
            state = VisualState.Shield;

            // Clip "shield" dai ~2.1s: 1 vong clip = dung thoi gian khien, khien
            // keo dai hon thi lap lai. Update() tra ve Run/Idle khi khien tat.
            float duration = shieldSource != null ? shieldSource.shieldDuration : 0f;
            TrackEntry entry = PlayLoop(AnimShield);
            FitToDuration(entry, duration);
        }

        public void NotifyHurt()
        {
            if (IsFinished)
                return;
            if (state == VisualState.Shield || state == VisualState.Charge)
                return; // dang do khien / luot thi khong ngat tu the
            PlayOneShot(AnimHurt, VisualState.Hurt);
        }

        public void NotifyDeath()
        {
            if (dead)
                return;
            dead = true;
            state = VisualState.Death;
            PlayOnce(AnimDeath);
        }

        /// <summary>Thang man: goi tu StageCompleteController.ProceedToVictoryFlow
        /// (sau hoi thoai NPC neu co) thay vi OnStageComplete de nhan vat con
        /// chay toi NPC voi animation run truoc khi khai cuu.</summary>
        public void NotifyVictory()
        {
            if (IsFinished)
                return;
            state = VisualState.Victory;
            shieldSource = null;
            PlayLoop(AnimVictory);
        }

        public void PlayIdle() => PlayLoop(AnimIdle);
        public void PlayRun() => PlayLoop(AnimRun);

        // ----- Spine helpers ----------------------------------------------

        private bool HasAnimation(string name)
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null ||
                skeletonAnimation.Skeleton.Data == null)
                return false;
            return skeletonAnimation.Skeleton.Data.FindAnimation(name) != null;
        }

        private TrackEntry PlayLoop(string name)
        {
            if (!HasAnimation(name))
                return null;
            animVersion++;
            return skeletonAnimation.AnimationState.SetAnimation(0, name, true);
        }

        /// <summary>
        /// Doi toc do phat de 1 vong clip dai dung "duration" giay.
        /// duration <= 0 thi giu toc do goc.
        /// </summary>
        private static void FitToDuration(TrackEntry entry, float duration)
        {
            if (entry == null || entry.Animation == null || duration <= 0f)
                return;

            float clipDuration = entry.Animation.Duration;
            if (clipDuration > 0f)
                entry.TimeScale = clipDuration / duration;
        }

        private void PlayOnce(string name)
        {
            if (!HasAnimation(name))
                return;
            skeletonAnimation.AnimationState.SetAnimation(0, name, false);
        }

        /// <summary>
        /// Plays a one-shot animation and automatically returns to the base
        /// loop when it completes. A newer play call bumps animVersion so a
        /// stale completion callback can never override a newer state.
        /// </summary>
        private void PlayOneShot(string name, VisualState visualState, float fitDuration = 0f)
        {
            if (skeletonAnimation == null)
                return;

            if (state == VisualState.Death)
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
            {
                FitToDuration(entry, fitDuration);
                entry.Complete += _ => OnOneShotComplete(version, visualState);
            }
        }

        private void OnOneShotComplete(int version, VisualState visualState)
        {
            if (version != animVersion || state != visualState)
                return; // superseded by a newer animation
            state = VisualState.None;
            PlayBaseLoop();
        }

        private void PlayBaseLoop()
        {
            if (dead || state == VisualState.Victory)
                return;
            if (skeletonAnimation == null)
                return;

            string baseAnim = GetBaseAnimation();
            lastBaseAnim = baseAnim;
            PlayLoop(baseAnim);
        }

        private void OnHealthChanged(int current, int max)
        {
            if (dead)
                return;

            if (lastHealth >= 0 && current < lastHealth)
                NotifyHurt();

            lastHealth = current;
        }

        private void OnDeath()
        {
            NotifyDeath();
        }

        private void OnSkillExecutedSource(SkillBase skill)
        {
            if (dead || skill == null)
                return;

            if (skill is ShieldSkill shield)
            {
                shieldSource = shield;
                NotifyShield(shield);
            }
            else if (skill is PotionSkill)
            {
                NotifyPotion();
            }
            else if (skill is ChargeSkill charge)
            {
                NotifyCharge(charge.GetChargeDuration());
            }
        }
    }
}
