using UnityEngine;

namespace EternalClash.Enemy
{
    [CreateAssetMenu(menuName = "ETERNAL CLASH/Enemy Combat Profile")]
    public class EnemyCombatProfile : ScriptableObject
    {
        public float moveSpeed = 1.5f;
        public int attackDamage = 5;
        public float prepareTime = 0.6f;
        public float attackCooldown = 1.2f;
        public float stunDuration = 1f;
        public float knockbackPower = 2f;
    }
}
