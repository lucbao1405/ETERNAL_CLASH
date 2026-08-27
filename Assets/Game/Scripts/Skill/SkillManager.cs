using UnityEngine;

namespace EternalClash.Skill
{
    public class SkillManager : MonoBehaviour
    {
        public SkillBase dashSkill;
        public SkillBase shieldSkill;
        public SkillBase potionSkill;

        public void UseDash()
        {
            if (dashSkill != null)
                dashSkill.UseSkill();
        }

        public void UseShield()
        {
            if (shieldSkill != null)
                shieldSkill.UseSkill();
        }

        public void UsePotion()
        {
            if (potionSkill != null)
                potionSkill.UseSkill();
        }
    }
}
