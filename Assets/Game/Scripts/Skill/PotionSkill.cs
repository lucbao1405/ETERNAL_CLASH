using UnityEngine;
using EternalClash.Character;
using EternalClash.UI;

namespace EternalClash.Skill
{
    public class PotionSkill : SkillBase
    {
        [Header("Heal Stats")]
        public int healAmount = 30;

        private HealthSystem health;

        protected override void Awake()
        {
            base.Awake();

            health = GetComponent<HealthSystem>();
        }

        protected override void Execute()
        {
            if (health == null)
            {
                Debug.LogWarning("[SKILL] Potion USED but HealthSystem missing");
                return;
            }

            Debug.Log("[SKILL] Potion/Heal USED - Heal amount: " + healAmount);

            health.Heal(healAmount);

            if (DamagePopupManager.Instance != null)
            {
                DamagePopupManager.Instance.ShowHeal(
                    transform.position,
                    healAmount
                );
            }
        }

        public void UpgradeHeal()
        {
            healAmount += 25;
        }
    }
}
