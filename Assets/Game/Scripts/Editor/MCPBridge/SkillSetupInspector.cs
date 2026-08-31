using UnityEngine;
using System.Text;
using EternalClash.Skill;

public static class SkillSetupInspector
{
    public static string CheckSkillSetup()
    {
        StringBuilder result = new StringBuilder();
        GameObject player = GameObject.Find("Player(Clone)");

        if(player == null)
            player = GameObject.Find("Player");

        if(player == null)
            return "[MCP] Player not found";

        result.AppendLine("SKILL SETUP:");

        var manager = player.GetComponent<SkillManager>();

        if(manager == null)
        {
            result.AppendLine("SkillManager : MISSING");
            return result.ToString();
        }

        result.AppendLine("SkillManager : OK");
        result.AppendLine("Player runtime : " + player.name);

        return result.ToString();
    }
}
