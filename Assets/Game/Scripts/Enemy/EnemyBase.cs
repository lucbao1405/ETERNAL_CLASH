using UnityEngine;

namespace EternalClash.Enemy
{
    public class EnemyBase : MonoBehaviour
    {
        [Header("Enemy Stats")]
        public int maxHP = 20;
        public int attackDamage = 5;
        public float moveSpeed = 1f;

        protected int currentHP;

        protected virtual void Awake()
        {
            currentHP = maxHP;
        }

        public virtual void TakeDamage(int damage)
        {
            currentHP -= damage;

            if (currentHP <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            Destroy(gameObject);
        }

        public int GetCurrentHP()
        {
            return currentHP;
        }
    }
}
