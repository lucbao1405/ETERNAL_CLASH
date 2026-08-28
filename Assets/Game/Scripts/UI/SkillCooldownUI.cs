using UnityEngine;
using TMPro;
using EternalClash.Skill;

namespace EternalClash.UI
{
    public class SkillCooldownUI : MonoBehaviour
    {
        public TMP_Text cooldownText;

        public enum SkillType
        {
            Charge,
            Shield,
            Potion
        }

        public SkillType skillType;

        private SkillManager skillManager;

        private void Start()
        {
            FindRuntimePlayer();
        }

        private void FindRuntimePlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player == null)
            {
                Debug.LogWarning("[CooldownUI] Cannot find runtime Player");
                return;
            }

            skillManager = player.GetComponent<SkillManager>();

            if (skillManager == null)
            {
                Debug.LogWarning("[CooldownUI] Player has no SkillManager");
                return;
            }

            Debug.Log("[CooldownUI] Connected to " + player.name);
        }

        private void Update()
        {
            if (skillManager == null)
            {
                FindRuntimePlayer();
                return;
            }

            if (cooldownText == null)
                return;

            float cd = 0f;

            switch(skillType)
            {
                case SkillType.Charge:
                    cd = skillManager.GetChargeCooldown();
                    break;

                case SkillType.Shield:
                    cd = skillManager.GetShieldCooldown();
                    break;

                case SkillType.Potion:
                    cd = skillManager.GetPotionCooldown();
                    break;
            }

            cooldownText.text = cd > 0.05f ? cd.ToString("0.0") : "";
        }
    }
}
