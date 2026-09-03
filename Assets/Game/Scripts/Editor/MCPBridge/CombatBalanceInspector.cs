using UnityEngine;

namespace EternalClash.EditorTools
{
    public class CombatBalanceInspector : MonoBehaviour
    {
        public static void PrintRecommendedCheck()
        {
            Debug.Log("[MCP COMBAT BALANCE] Check: Charge speed, damage, knockback, stun, enemy attack range");
        }

        public static void LogCharge(float speed, float duration)
        {
            Debug.Log($"[COMBAT] Charge speed={speed} duration={duration}");
        }

        public static void LogHit(string enemy, int damage)
        {
            Debug.Log($"[COMBAT] Hit {enemy} damage={damage}");
        }

        public static void LogKnockback(float distance, float stun)
        {
            Debug.Log($"[COMBAT] Knockback distance={distance} stun={stun}");
        }
    }
}
