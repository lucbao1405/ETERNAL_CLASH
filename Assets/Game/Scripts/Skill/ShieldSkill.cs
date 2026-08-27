using UnityEngine;

namespace EternalClash.Skill
{
    public class ShieldSkill : SkillBase
    {
        public float damageMultiplier = 0.2f;
        public float duration = 3f;

        public bool active { get; private set; }

        protected override void Execute()
        {
            active = true;
            Debug.Log("Shield activated");
            Invoke(nameof(DisableShield), duration);
        }

        private void DisableShield()
        {
            active = false;
            Debug.Log("Shield ended");
        }

        public float ReduceDamage(float damage)
        {
            return active ? damage * damageMultiplier : damage;
        }
    }
}
