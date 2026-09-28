using UnityEngine;
using UnityEditor;
using System.Text;

public static class ETERNALClashInspector
{
    public static string InspectObject(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj == null)
            return "[MCP] Object not found: " + objectName;

        StringBuilder result = new StringBuilder();
        result.AppendLine("OBJECT: " + obj.name);
        result.AppendLine("COMPONENTS:");

        Component[] components = obj.GetComponents<Component>();
        foreach(Component c in components)
        {
            if(c != null)
                result.AppendLine("- " + c.GetType().Name);
        }

        return result.ToString();
    }

    public static string InspectPlayerRuntime()
    {
        GameObject player = GameObject.Find("Player(Clone)");

        if(player == null)
            player = GameObject.Find("Player");

        if(player == null)
            return "[MCP] Player runtime not found";

        return InspectObject(player.name);
    }
}
