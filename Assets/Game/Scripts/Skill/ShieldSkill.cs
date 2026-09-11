using UnityEngine;
using EternalClash.Player;
using EternalClash.Combat;
using EternalClash.World;

namespace EternalClash.Skill
{
    public class ShieldSkill : SkillBase
    {
        [Header("Shield Stats")]
        public float damageReduction = 0.8f;
        public float shieldDuration = 1.0f;
        public int reflectDamage = 5;

        public bool Active { get; private set; }

        private PlayerController playerController;
        private WorldScroller worldScroller;

        // Chi cho cuon lai neu chinh Khien la thu da dung no. Tranh truong hop man
        // da ket thuc (StageCompleteController goi StopScroll) ma Khien lai bat len.
        private bool stoppedScroll;

        protected override void Awake()
        {
            skillName = "Shield";
            cooldown = 5.0f;
            base.Awake();
            playerController = GetComponentInParent<PlayerController>();
        }

        public bool IsActive()
        {
            return Active;
        }

        protected override void Execute()
        {
            Active = true;
            Debug.Log("[SKILL] Shield ACTIVATED (Duration: 1s, -80% DMG, Reflect 5 DMG)");

            if (playerController != null)
                playerController.StopMovement();

            StopWorld();

            CancelInvoke(nameof(DisableShield));
            Invoke(nameof(DisableShield), shieldDuration);
        }

        private void DisableShield()
        {
            Active = false;
            Debug.Log("[SKILL] Shield DEACTIVATED");

            if (playerController != null)
                playerController.ResumeMovement();

            ResumeWorld();
        }

        /// <summary>
        /// "Khung lai 1s" cua GDD 3.3. Day la auto-scroller: nguoi choi dung yen con
        /// NEN cuon, nen dung buoc chan = dung cuon nen. Truoc day chi goi
        /// PlayerController.StopMovement(), ma chuoi do dan toi AutoRunner.isRunning -
        /// mot co khong dieu khien chuyen dong nao, nen khien khong he lam cham tien do.
        /// </summary>
        private void StopWorld()
        {
            if (worldScroller == null)
                worldScroller = FindObjectOfType<WorldScroller>();

            if (worldScroller == null || !worldScroller.IsScrolling)
                return;

            worldScroller.StopScroll();
            stoppedScroll = true;
        }

        private void ResumeWorld()
        {
            if (!stoppedScroll)
                return;

            stoppedScroll = false;

            if (worldScroller != null)
                worldScroller.ResumeScroll();
        }

        public int BlockDamage(int incomingDamage, GameObject attacker = null)
        {
            if (!Active)
                return incomingDamage;

            int mitigatedDamage = Mathf.RoundToInt(incomingDamage * (1f - damageReduction));
            if (mitigatedDamage < 1 && incomingDamage > 0)
                mitigatedDamage = 1;

            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.PlayerShieldBlock);

            // Reflect damage to attacker
            if (attacker != null && reflectDamage > 0)
            {
                CombatDamageResolver.Instance?.DealDamage(
                    attacker,
                    reflectDamage,
                    DamageSource.Reflect
                );
            }

            return mitigatedDamage;
        }

        public bool IsPerfectCounterWindow()
        {
            return Active;
        }
    }
}
