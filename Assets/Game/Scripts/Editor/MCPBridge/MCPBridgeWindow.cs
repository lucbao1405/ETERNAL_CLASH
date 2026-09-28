using UnityEditor;
using UnityEngine;

public class MCPBridgeWindow : EditorWindow
{
    string command = "";
    string output = "";

    [MenuItem("Tools/MCP Bridge")]
    public static void Open()
    {
        GetWindow<MCPBridgeWindow>("MCP Bridge");
    }

    void OnGUI()
    {
        GUILayout.Label("ETERNAL_CLASH MCP Bridge");

        command = EditorGUILayout.TextField("Command", command);

        if (GUILayout.Button("Execute"))
        {
            output = Execute(command);
        }

        GUILayout.TextArea(output);
    }

    string Execute(string cmd)
    {
        string[] args = cmd.Split(' ');

        if (args.Length >= 2 && args[0] == "find")
            return UnityObjectAPI.FindObject(args[1]);

        if (args.Length >= 3 && args[0] == "add")
            return UnityObjectAPI.AddComponent(args[1], args[2]);

        return "UNKNOWN_COMMAND";
    }
}
