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
    /// Available Player spine animations:
    ///   Idle = "stand", Run = "run", Attack = "choc", Shield = "shield",
    ///   Potion = "heal", Hurt = "damage", Death = "damage" (no dedicated death clip).
    /// </summary>
    public class PlayerAnimationController : MonoBehaviour, IPlayerAnimationFeedback
    {
        [Header("Spine")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        private const string AnimIdle = "stand";
        private const string AnimRun = "run";
        private const string AnimAttack = "choc";
        private const string AnimShield = "shield";
        private const string AnimPotion = "heal";
        private const string AnimHurt = "damage";
        private const string AnimDeath = "damage";

        private enum VisualState
        {
            None,
            Attack,
            Shield,
            Potion,
            Hurt,
            Death
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
                skill.SkillExecuted += OnSkillExecuted;
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
                    skill.SkillExecuted -= OnSkillExecuted;
            }
            boundSkills.Clear();

            subscribed = false;
        }

        private void Update()
        {
            if (dead || skeletonAnimation == null)
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

        public void NotifyAttack()
        {
            if (dead || state == VisualState.Death)
                return;
            if (state == VisualState.Shield)
                return; // keep the shield pose while blocking
            PlayOneShot(AnimAttack, VisualState.Attack);
        }

        public void NotifyPotion()
        {
            if (dead || state == VisualState.Death)
                return;
            if (state == VisualState.Shield)
                return;
            PlayOneShot(AnimPotion, VisualState.Potion);
        }

        public void NotifyShield(SkillBase source)
        {
            if (dead || state == VisualState.Death)
                return;

            shieldSource = source as ShieldSkill;
            state = VisualState.Shield;
            PlayLoop(AnimShield);
        }

        public void NotifyHurt()
        {
            if (dead || state == VisualState.Death)
                return;
            if (state == VisualState.Shield)
                return; // shield pose already conveys blocking
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

        private void PlayLoop(string name)
        {
            if (!HasAnimation(name))
                return;
            animVersion++;
            skeletonAnimation.AnimationState.SetAnimation(0, name, true);
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
        private void PlayOneShot(string name, VisualState visualState)
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
                entry.Complete += _ => OnOneShotComplete(version, visualState);
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
            if (dead)
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

        private void OnSkillExecuted(string skillName)
        {
            if (dead)
                return;

            if (string.Equals(skillName, "Shield", StringComparison.OrdinalIgnoreCase))
            {
                // Find the skill instance that executed so we can watch its end.
                shieldSource = null;
                foreach (SkillBase skill in boundSkills)
                {
                    if (skill is ShieldSkill s && string.Equals(s.skillName, skillName, StringComparison.OrdinalIgnoreCase))
                    {
                        shieldSource = s;
                        break;
                    }
                }
                NotifyShield(shieldSource);
            }
            else if (string.Equals(skillName, "Potion", StringComparison.OrdinalIgnoreCase))
            {
                NotifyPotion();
            }
            // Charge and any other skills are intentionally ignored here.
        }
    }
}
