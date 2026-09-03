using UnityEngine;
using UnityEditor;
using EternalClash.Skill;
using EternalClash.Character;

public static class AutoSetupFixer
{
    public static void InspectAndFixPlayer(GameObject player)
    {
        if (player == null)
        {
            Debug.Log("[MCP] Player not found");
            return;
        }

        Debug.Log("[MCP] Inspect " + player.name);

        var skill = player.GetComponent<SkillManager>();
        if (skill == null)
        {
            Debug.LogWarning("[MCP] Missing SkillManager");
        }
        else
        {
            Debug.Log("[MCP] SkillManager OK");
        }

        var health = player.GetComponent<HealthSystem>();
        if (health == null)
            Debug.LogWarning("[MCP] Missing HealthSystem");
        else
            Debug.Log("[MCP] HealthSystem OK");
    }

    public static void AddComponent(GameObject target, string typeName)
    {
        if (target == null) return;

        var type = System.Type.GetType(typeName);
        if (type == null)
        {
            Debug.LogWarning("[MCP] Cannot find type " + typeName);
            return;
        }

        if (target.GetComponent(type) == null)
        {
            target.AddComponent(type);
            Debug.Log("[MCP] Added " + typeName);
        }
    }
}
