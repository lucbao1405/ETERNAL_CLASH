using UnityEngine;

namespace EternalClash.EditorTools
{
    public static class SkillReferenceAutoFixer
    {
        public static void Fix(GameObject player)
        {
            if (player == null)
            {
                Debug.Log("[MCP] Player not found");
                return;
            }

            var manager = player.GetComponent<EternalClash.Skill.SkillManager>();
            if (manager == null)
            {
                Debug.Log("[MCP] SkillManager missing on " + player.name);
                return;
            }

            var shield = player.GetComponentInChildren<EternalClash.Skill.ShieldSkill>(true);
            var potion = player.GetComponentInChildren<EternalClash.Skill.PotionSkill>(true);
            var charge = player.GetComponentInChildren<EternalClash.Skill.ChargeSkill>(true);

            manager.shieldSkill = shield;
            manager.potionSkill = potion;
            manager.chargeSkill = charge;

            Debug.Log("[MCP] Skill references fixed on " + player.name);
            Debug.Log("Shield: " + (shield != null));
            Debug.Log("Potion: " + (potion != null));
            Debug.Log("Charge: " + (charge != null));
        }
    }
}
