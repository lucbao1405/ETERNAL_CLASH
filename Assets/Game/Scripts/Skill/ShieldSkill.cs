using UnityEngine;
using EternalClash.Player;
using EternalClash.Combat;

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

            CancelInvoke(nameof(DisableShield));
            Invoke(nameof(DisableShield), shieldDuration);
        }

        private void DisableShield()
        {
            Active = false;
            Debug.Log("[SKILL] Shield DEACTIVATED");

            if (playerController != null)
                playerController.ResumeMovement();
        }

        public int BlockDamage(int incomingDamage, GameObject attacker = null)
        {
            if (!Active)
                return incomingDamage;

            int mitigatedDamage = Mathf.RoundToInt(incomingDamage * (1f - damageReduction));
            if (mitigatedDamage < 1 && incomingDamage > 0)
                mitigatedDamage = 1;

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
