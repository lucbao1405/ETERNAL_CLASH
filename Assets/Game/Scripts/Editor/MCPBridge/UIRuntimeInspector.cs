using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.MCPBridge
{
    public static class UIRuntimeInspector
    {
        public static void CheckSkillButtons()
        {
            Button[] buttons = Object.FindObjectsOfType<Button>(true);

            Debug.Log("[MCP] Checking skill UI buttons...");

            foreach (Button button in buttons)
            {
                Debug.Log($"[MCP] Button: {button.name} listeners={button.onClick.GetPersistentEventCount()}");

                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    Object target = button.onClick.GetPersistentTarget(i);
                    string method = button.onClick.GetPersistentMethodName(i);

                    Debug.Log($"[MCP] -> Target:{target} Method:{method}");
                }
            }
        }
    }
}
