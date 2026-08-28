using UnityEngine;
using System;

namespace EternalClash.Enemy
{
    public class EnemyHealthSystem : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 50;
        private int currentHealth;

        public int CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0;

        public event Action OnEnemyDead;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(int damage)
        {
            if (IsDead) return;

            currentHealth -= damage;

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die();
            }
        }

        private void Die()
        {
            Debug.Log("[ENEMY] " + gameObject.name + " died");

            // Cho he thong vat pham nhan event roi do vat
            OnEnemyDead?.Invoke();

            Destroy(gameObject, 0.5f);
        }
    }
}
