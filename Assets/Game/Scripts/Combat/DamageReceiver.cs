using UnityEngine;
using EternalClash.Character;
using EternalClash.Skill;

namespace EternalClash.Combat
{
    public class DamageReceiver : MonoBehaviour
    {
        private HealthSystem healthSystem;
        private ShieldSkill shieldSkill;

        private float damageMultiplier = 1f;

        private void Awake()
        {
            healthSystem = GetComponent<HealthSystem>();
            shieldSkill = GetComponent<ShieldSkill>();
        }

        public void TakeDamage(int damage)
        {
            if (healthSystem == null)
                return;

            float finalDamage = damage * damageMultiplier;

            if (shieldSkill != null)
                finalDamage = shieldSkill.ReduceDamage(finalDamage);

            healthSystem.TakeDamage(Mathf.RoundToInt(finalDamage));
        }

        public void SetDamageMultiplier(float multiplier)
        {
            damageMultiplier = multiplier;
        }

        public void ResetDamageMultiplier()
        {
            damageMultiplier = 1f;
        }
    }
}
