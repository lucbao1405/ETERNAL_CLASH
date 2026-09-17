using UnityEngine;
using EternalClash.Data;
using System.Collections.Generic;

namespace EternalClash.Skill
{
    /// <summary>
    /// Persists the skill branch and sub-skill selected in the Town skill panel.
    /// </summary>
    public static class SkillLoadout
    {
        private static SkillData selectedData;
        private static readonly Dictionary<string, SkillData> knownSkills =
            new Dictionary<string, SkillData>(System.StringComparer.OrdinalIgnoreCase);
        public static SkillData SelectedData
        {
            get
            {
                if (selectedData == null)
                {
                    if (knownSkills.TryGetValue(SkillId, out SkillData knownSkill))
                        selectedData = knownSkill;

                    foreach (SkillData skill in Resources.FindObjectsOfTypeAll<SkillData>())
                    {
                        if (skill != null && string.Equals(skill.skillId, SkillId,
                            System.StringComparison.OrdinalIgnoreCase))
                        {
                            selectedData = skill;
                            break;
                        }
                    }
                }
                return selectedData;
            }
        }

        public static SkillData GetSelectedData(string branchId)
        {
            if (string.IsNullOrWhiteSpace(branchId))
                return null;

            string skillId = PlayerPrefs.GetString(
                SkillKey + "_" + branchId.ToLowerInvariant(),
                branchId.Equals("shield", System.StringComparison.OrdinalIgnoreCase)
                    ? "Normal_Shield" : "Normal_Charge");

            if (knownSkills.TryGetValue(skillId, out SkillData knownSkill))
                return knownSkill;

            foreach (SkillData skill in Resources.FindObjectsOfTypeAll<SkillData>())
            {
                if (skill != null && string.Equals(skill.skillId, skillId,
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    knownSkills[skill.skillId] = skill;
                    return skill;
                }
            }

            return null;
        }
        private const string BranchKey = "EQUIPPED_SKILL_BRANCH";
        private const string SkillKey = "EQUIPPED_SKILL_ID";
        private const string SkillNameKey = "EQUIPPED_SKILL_NAME";
        private const string BonusDamageKey = "EQUIPPED_SKILL_BONUS_DAMAGE";
        private const string CooldownKey = "EQUIPPED_SKILL_COOLDOWN";
        private const string EffectValueKey = "EQUIPPED_SKILL_EFFECT_VALUE";
        private const string EffectDurationKey = "EQUIPPED_SKILL_EFFECT_DURATION";
        private const string EffectTypeKey = "EQUIPPED_SKILL_EFFECT_TYPE";

        public static string BranchId => PlayerPrefs.GetString(BranchKey, "charge");
        public static string SkillId
        {
            get
            {
                string branchKey = SkillKey + "_" + BranchId.ToLowerInvariant();
                string fallback = BranchId.Equals("shield", System.StringComparison.OrdinalIgnoreCase)
                    ? "Normal_Shield" : "Normal_Charge";
                return PlayerPrefs.GetString(branchKey, PlayerPrefs.GetString(SkillKey, fallback));
            }
        }
        public static int BonusDamage => PlayerPrefs.GetInt(GetBranchKey(BonusDamageKey), PlayerPrefs.GetInt(BonusDamageKey, 0));
        public static float CustomCooldown => PlayerPrefs.GetFloat(GetBranchKey(CooldownKey), PlayerPrefs.GetFloat(CooldownKey, 0f));
        public static float EffectValue => PlayerPrefs.GetFloat(GetBranchKey(EffectValueKey), PlayerPrefs.GetFloat(EffectValueKey, 0f));
        public static float EffectDuration => PlayerPrefs.GetFloat(GetBranchKey(EffectDurationKey), PlayerPrefs.GetFloat(EffectDurationKey, 0f));
        public static int EffectType => PlayerPrefs.GetInt(GetBranchKey(EffectTypeKey), PlayerPrefs.GetInt(EffectTypeKey, 0));
        public static string SkillName => PlayerPrefs.GetString(SkillNameKey, SkillId);

        public static int GetBonusDamage(string branchId)
        {
            return PlayerPrefs.GetInt(BonusDamageKey + "_" + branchId.ToLowerInvariant(), 0);
        }

        public static float GetCooldown(string branchId)
        {
            return PlayerPrefs.GetFloat(CooldownKey + "_" + branchId.ToLowerInvariant(), 0f);
        }

        public static float GetEffectValue(string branchId)
        {
            return PlayerPrefs.GetFloat(EffectValueKey + "_" + branchId.ToLowerInvariant(), 0f);
        }

        public static float GetEffectDuration(string branchId)
        {
            return PlayerPrefs.GetFloat(EffectDurationKey + "_" + branchId.ToLowerInvariant(), 0f);
        }

        public static int GetEffectType(string branchId)
        {
            return PlayerPrefs.GetInt(EffectTypeKey + "_" + branchId.ToLowerInvariant(), 0);
        }

        public static string GetSkillName(string branchId)
        {
            string skillId = PlayerPrefs.GetString(
                SkillKey + "_" + branchId.ToLowerInvariant(),
                branchId.Equals("shield", System.StringComparison.OrdinalIgnoreCase)
                    ? "Normal_Shield" : "Normal_Charge");
            return PlayerPrefs.GetString(SkillNameKey + "_" + branchId.ToLowerInvariant(), skillId);
        }

        public static void SetSelectedData(SkillData data)
        {
            selectedData = data;
        }

        public static void RegisterData(SkillData data)
        {
            if (data != null && !string.IsNullOrWhiteSpace(data.skillId))
                knownSkills[data.skillId] = data;
        }

        public static void SelectBranch(string branchId)
        {
            if (string.IsNullOrWhiteSpace(branchId))
                return;

            PlayerPrefs.SetString(BranchKey, branchId.ToLowerInvariant());
            PlayerPrefs.Save();
            selectedData = null;
        }

        public static void Save(string branchId, string skillId, SkillData data = null)
        {
            if (!string.IsNullOrWhiteSpace(branchId))
                PlayerPrefs.SetString(BranchKey, branchId.ToLowerInvariant());
            if (!string.IsNullOrWhiteSpace(skillId))
            {
                PlayerPrefs.SetString(SkillKey, skillId);
                PlayerPrefs.SetString(SkillKey + "_" + branchId.ToLowerInvariant(), skillId);
            }
            selectedData = data;
            if (data != null && !string.IsNullOrWhiteSpace(data.skillName))
                PlayerPrefs.SetString(SkillNameKey, data.skillName);
            if (data != null && !string.IsNullOrWhiteSpace(branchId))
                PlayerPrefs.SetString(SkillNameKey + "_" + branchId.ToLowerInvariant(), data.skillName);
            PlayerPrefs.SetInt(BonusDamageKey, data != null ? data.bonusDamage : 0);
            PlayerPrefs.SetFloat(CooldownKey, data != null ? data.customCooldown : 0f);
            PlayerPrefs.SetFloat(EffectValueKey, data != null ? data.effectValue : 0f);
            PlayerPrefs.SetFloat(EffectDurationKey, data != null ? data.effectDuration : 0f);
            PlayerPrefs.SetInt(EffectTypeKey, data != null ? (int)data.specialEffectType : 0);
            PlayerPrefs.SetInt(GetBranchKey(BonusDamageKey), data != null ? data.bonusDamage : 0);
            PlayerPrefs.SetFloat(GetBranchKey(CooldownKey), data != null ? data.customCooldown : 0f);
            PlayerPrefs.SetFloat(GetBranchKey(EffectValueKey), data != null ? data.effectValue : 0f);
            PlayerPrefs.SetFloat(GetBranchKey(EffectDurationKey), data != null ? data.effectDuration : 0f);
            PlayerPrefs.SetInt(GetBranchKey(EffectTypeKey), data != null ? (int)data.specialEffectType : 0);
            PlayerPrefs.Save();
        }

        private static string GetBranchKey(string key)
        {
            return key + "_" + BranchId.ToLowerInvariant();
        }

        public static bool IsBranchEquipped(string branchId)
        {
            return string.Equals(BranchId, branchId, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
