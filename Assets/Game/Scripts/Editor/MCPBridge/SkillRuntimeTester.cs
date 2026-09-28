using UnityEngine;
using EternalClash.Skill;

namespace EternalClash.MCP
{
    public static class SkillRuntimeTester
    {
        public static void TestPlayerSkills()
        {
            GameObject player = GameObject.Find("Player(Clone)");

            if (player == null)
                player = GameObject.Find("Player");

            if (player == null)
            {
                Debug.Log("[MCP] Player not found");
                return;
            }

            Debug.Log("[MCP] Testing skills on " + player.name);

            SkillManager manager = player.GetComponent<SkillManager>();

            if (manager == null)
            {
                Debug.Log("[MCP] SkillManager missing");
                return;
            }

            Debug.Log("[MCP] Shield reference: " + (manager.shieldSkill != null));
            Debug.Log("[MCP] Potion reference: " + (manager.potionSkill != null));
            Debug.Log("[MCP] Charge reference: " + (manager.chargeSkill != null));

            if (manager.chargeSkill != null)
                Debug.Log("[MCP] Charge ready cooldown=" + manager.GetChargeCooldown());

            if (manager.shieldSkill != null)
                Debug.Log("[MCP] Shield ready cooldown=" + manager.GetShieldCooldown());

            if (manager.potionSkill != null)
                Debug.Log("[MCP] Potion ready cooldown=" + manager.GetPotionCooldown());
        }
    }
}
