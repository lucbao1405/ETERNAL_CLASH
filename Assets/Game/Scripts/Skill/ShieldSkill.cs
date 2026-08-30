using UnityEngine;

namespace EternalClash.Skill
{
    public class ShieldSkill : SkillBase
    {
        [Header("Shield Stats")]
        public float damageReduction = 0.5f;
        public float duration = 5f;

        public bool Active { get; private set; }

        public bool IsActive()
        {
            return Active;
        }

        protected override void Execute()
        {
            Active = true;
            Debug.Log("[SKILL] Shield USED - Shield ON");
            Invoke(nameof(DisableShield), duration);
        }

        private void DisableShield()
        {
            Active = false;
            Debug.Log("Shield OFF");
        }

        public bool IsPerfectCounterWindow()
        {
            // Shield state alone does not determine perfect timing.
            // CounterSkillResolver checks enemy timing window.
            return Active;
        }

        public int BlockDamage(int damage)
        {
            if (!Active)
                return 0;

            return Mathf.RoundToInt(damage * damageReduction);
        }

        public void UpgradeShield()
        {
            damageReduction = Mathf.Clamp(damageReduction + 0.1f, 0, 0.9f);
            duration += 1f;
        }
    }
}
