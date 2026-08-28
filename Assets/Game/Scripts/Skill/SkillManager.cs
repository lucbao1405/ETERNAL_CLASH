using UnityEngine;

namespace EternalClash.Skill
{
    public class SkillManager : MonoBehaviour
    {
        public SkillBase shieldSkill;
        public SkillBase potionSkill;
        public SkillBase chargeSkill;

        private void Awake()
        {
            // Player is spawned as Clone, so do not keep references from prefab/old object.
            shieldSkill = GetComponentInChildren<ShieldSkill>();
            potionSkill = GetComponentInChildren<PotionSkill>();
            chargeSkill = GetComponentInChildren<ChargeSkill>();

            Debug.Log("[SkillManager] Auto bind skills on " + gameObject.name);
        }

        public float GetShieldCooldown()
        {
            return shieldSkill != null ? shieldSkill.CooldownRemaining : 0;
        }

        public float GetPotionCooldown()
        {
            return potionSkill != null ? potionSkill.CooldownRemaining : 0;
        }

        public float GetChargeCooldown()
        {
            return chargeSkill != null ? chargeSkill.CooldownRemaining : 0;
        }

        public void UseShield()
        {
            Debug.Log("[UI] Use Shield Button Pressed");
            shieldSkill?.UseSkill();
        }

        public void UsePotion()
        {
            Debug.Log("[UI] Use Potion Button Pressed");
            potionSkill?.UseSkill();
        }

        public void UseCharge()
        {
            Debug.Log("[UI] Use Charge Button Pressed");
            chargeSkill?.UseSkill();
        }
    }
}
