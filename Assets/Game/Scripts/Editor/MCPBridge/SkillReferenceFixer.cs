using UnityEngine;
using UnityEditor;

public static class SkillReferenceFixer
{
    public static void CheckSkillManager(GameObject player)
    {
        if (player == null)
        {
            Debug.Log("[MCP] Player not found");
            return;
        }

        Component skillManager = player.GetComponent("SkillManager");

        if (skillManager == null)
        {
            Debug.Log("[MCP] Missing SkillManager on " + player.name);
            return;
        }

        Debug.Log("[MCP] SkillManager found on " + player.name);
        Debug.Log("[MCP] Checking skill references...");
    }

    public static GameObject FindRuntimePlayer()
    {
        GameObject clone = GameObject.Find("Player(Clone)");
        if (clone != null)
            return clone;

        return GameObject.Find("Player");
    }
}
