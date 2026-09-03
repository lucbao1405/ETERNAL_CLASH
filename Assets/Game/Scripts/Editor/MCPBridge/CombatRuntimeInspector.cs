using UnityEngine;

namespace EternalClash.EditorTools.MCPBridge
{
    public static class CombatRuntimeInspector
    {
        public static void InspectCombatObject(GameObject obj)
        {
            if (obj == null)
            {
                Debug.Log("[MCP] Combat inspect failed: object null");
                return;
            }

            Debug.Log("[MCP] Combat Inspect: " + obj.name);

            CheckComponent<Collider2D>(obj, "Collider2D");
            CheckComponent<Rigidbody2D>(obj, "Rigidbody2D");
            CheckComponent<Animator>(obj, "Animator");

            Debug.Log("[MCP] Combat inspection finished");
        }

        private static void CheckComponent<T>(GameObject obj, string name) where T : Component
        {
            bool exist = obj.GetComponent<T>() != null;
            Debug.Log("[MCP] " + name + ": " + (exist ? "OK" : "MISSING"));
        }
    }
}
