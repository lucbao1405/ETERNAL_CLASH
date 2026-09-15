using UnityEngine;
using EternalClash.Data;

namespace EternalClash.Skill
{
    public class SkillManager : MonoBehaviour
    {
        public SkillBase shieldSkill;
        public SkillBase potionSkill;
        public SkillBase chargeSkill;

        public string EquippedBranchId { get; private set; }
        public string EquippedSkillId { get; private set; }

        private void Awake()
        {
            EnsureSkills();

            Debug.Log("[SkillManager] Auto bind skills on " + gameObject.name);
        }

        /// <summary>
        /// Ensures the runtime player always has the three battle skills. This is
        /// needed because PlayerSpawner creates a clone and scene/prefab references
        /// are not guaranteed to contain every skill component.
        /// </summary>
        public void EnsureSkills()
        {
            shieldSkill = GetComponentInChildren<ShieldSkill>(true);
            if (shieldSkill == null)
                shieldSkill = gameObject.AddComponent<ShieldSkill>();

            potionSkill = GetComponentInChildren<PotionSkill>(true);
            if (potionSkill == null)
                potionSkill = gameObject.AddComponent<PotionSkill>();

            chargeSkill = GetComponentInChildren<ChargeSkill>(true);
            if (chargeSkill == null)
                chargeSkill = gameObject.AddComponent<ChargeSkill>();

            EquippedBranchId = SkillLoadout.BranchId;
            EquippedSkillId = SkillLoadout.SkillId;

            if (string.IsNullOrWhiteSpace(EquippedSkillId))
            {
                EquippedSkillId = EquippedBranchId.Equals("shield", System.StringComparison.OrdinalIgnoreCase)
                    ? "Normal_Shield" : "Normal_Charge";
            }

            ApplyEquippedSkills();

            Debug.Log("[SkillManager] Equipped loadout: " + EquippedBranchId + "/" + EquippedSkillId);
        }

        public bool IsBranchEquipped(string branchId)
        {
            return SkillLoadout.IsBranchEquipped(branchId);
        }

        private void ApplyEquippedSkills()
        {
            SkillData chargeData = SkillLoadout.GetSelectedData("charge");
            ChargeSkill charge = chargeSkill as ChargeSkill;
            if (charge != null)
            {
                charge.baseDamage += SkillLoadout.GetBonusDamage("charge");
                if (SkillLoadout.GetCooldown("charge") > 0f)
                    charge.cooldown = SkillLoadout.GetCooldown("charge");
                charge.skillName = chargeData != null && !string.IsNullOrEmpty(chargeData.skillName)
                    ? chargeData.skillName : SkillLoadout.GetSkillName("charge");
            }

            SkillData shieldData = SkillLoadout.GetSelectedData("shield");
            ShieldSkill shield = shieldSkill as ShieldSkill;
            if (shield != null)
            {
                if (SkillLoadout.GetEffectType("shield") == (int)EternalClash.Data.SpecialEffectType.DamageReduction &&
                    SkillLoadout.GetEffectValue("shield") > 0f)
                {
                    shield.damageReduction = SkillLoadout.GetEffectValue("shield") > 1f
                        ? SkillLoadout.GetEffectValue("shield") / 100f
                        : SkillLoadout.GetEffectValue("shield");
                }
                if (SkillLoadout.GetEffectDuration("shield") > 0f)
                    shield.shieldDuration = SkillLoadout.GetEffectDuration("shield");
                if (SkillLoadout.GetCooldown("shield") > 0f)
                    shield.cooldown = SkillLoadout.GetCooldown("shield");
                if (SkillLoadout.GetEffectType("shield") == (int)EternalClash.Data.SpecialEffectType.ReflectDamage &&
                    SkillLoadout.GetEffectValue("shield") > 0f)
                    shield.reflectDamage = Mathf.RoundToInt(SkillLoadout.GetEffectValue("shield"));
                shield.skillName = shieldData != null && !string.IsNullOrEmpty(shieldData.skillName)
                    ? shieldData.skillName : SkillLoadout.GetSkillName("shield");
            }
        }

        public float GetShieldCooldown()
        {
            return shieldSkill != null ? shieldSkill.CooldownRemaining : 0;
        }

        public float GetPotionCooldown()
        {
            return potionSkill != null ? potionSkill.CooldownRemaining : 0;
        }

        private EternalClash.Combat.PlayerCombatStateMachine combatState;

        /// <summary>
        /// Skill dang bi khoa vi Player bi day lui (trang thai Hit / man hinh dang lui)
        /// hoac da chet. Cung dieu kien voi SkillBase.CanUse(), dung de UI lam mo nut.
        /// </summary>
        public bool AreSkillsLocked
        {
            get
            {
                if (combatState == null)
                    combatState = GetComponent<EternalClash.Combat.PlayerCombatStateMachine>();
                return combatState != null && !combatState.CanUseSkill;
            }
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
