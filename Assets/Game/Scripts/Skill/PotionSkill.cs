using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Skill
{
    public class PotionSkill : SkillBase
    {
        public int healAmount = 30;
        private HealthSystem health;

        private void Awake()
        {
            health = GetComponent<HealthSystem>();
        }

        protected override void Execute()
        {
            if (health == null)
                return;

            health.Heal(healAmount);
            Debug.Log("Potion used: +" + healAmount + " HP");
        }
    }
}
