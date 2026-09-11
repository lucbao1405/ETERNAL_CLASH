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
            // SkillBase starts its timer after Execute, so resolve the latest saved
            // Witch cooldown at use time instead of relying on an Awake cache.
            cooldown = AlchemistUpgradeSystem.Instance != null
                ? AlchemistUpgradeSystem.Instance.GetCooldownValue()
                : 15f;
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
            int bonusPercent = AlchemistUpgradeSystem.Instance != null
                ? AlchemistUpgradeSystem.Instance.GetHealingBonusPercent()
                : 0;
            finalHeal = Mathf.RoundToInt(finalHeal * (1f + bonusPercent / 100f));

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
