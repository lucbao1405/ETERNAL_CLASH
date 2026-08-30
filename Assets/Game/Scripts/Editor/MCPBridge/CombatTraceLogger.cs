using UnityEngine;

namespace EternalClash.EditorTools
{
    public static class CombatTraceLogger
    {
        private static bool enabledTrace = true;

        public static void Log(string message)
        {
            if (!enabledTrace) return;
            Debug.Log("[COMBAT TRACE] " + message);
        }

        public static void ChargeStart(GameObject player, float speed)
        {
            Log($"Charge Start | Player={player.name} | Speed={speed}");
        }

        public static void EnemyHit(GameObject enemy, float damage)
        {
            Log($"Enemy Hit | Target={enemy.name} | Damage={damage}");
        }

        public static void Knockback(GameObject enemy, Vector2 direction, float distance)
        {
            Log($"Knockback | Target={enemy.name} | Dir={direction} | Distance={distance}");
        }

        public static void EnemyDeath(GameObject enemy)
        {
            Log($"Enemy Death | Target={enemy.name}");
        }

        public static void Drop(GameObject item)
        {
            Log($"Drop Item | Item={item.name}");
        }
    }
}
