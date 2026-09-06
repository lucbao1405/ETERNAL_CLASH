using UnityEngine;
using EternalClash.Character;
using EternalClash.UI;
using EternalClash.Village;

namespace EternalClash.Skill
{
    public class PotionSkill : SkillBase
    {
        [Header("Heal Stats")]
        public int baseHealAmount = 50;

        private HealthSystem health;

        protected override void Awake()
        {
            skillName = "Potion";
            cooldown = 15.0f;
            base.Awake();

            health = GetComponentInParent<HealthSystem>();
            if (health == null)
                health = GetComponent<HealthSystem>();
        }

        protected override void Execute()
        {
            if (health == null)
            {
                health = GetComponentInParent<HealthSystem>();
                if (health == null)
                    health = GetComponent<HealthSystem>();
            }

            if (health == null)
            {
                Debug.LogWarning("[SKILL] Potion USED but HealthSystem missing");
                return;
            }

            int finalHeal = PlayerStatSystem.Instance != null 
                ? PlayerStatSystem.Instance.PotionHealAmount 
                : baseHealAmount;

            Debug.Log($"[SKILL] Potion USED - Heal amount: {finalHeal}");
            health.Heal(finalHeal);

            if (DamagePopupManager.Instance != null)
            {
                DamagePopupManager.Instance.ShowHeal(
                    transform.position,
                    finalHeal
                );
            }
        }
    }
}
