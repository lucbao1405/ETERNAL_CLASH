using UnityEngine;
using System;

namespace EternalClash.Enemy
{
    public class EnemyHealthSystem : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 50;
        private int currentHealth;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => currentHealth <= 0;

        public event Action<int, int> OnHealthChanged;
        public event Action OnDeath;

        private void Awake()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(int damage)
        {
            if (IsDead) return;

            currentHealth -= damage;

            if (currentHealth < 0)
                currentHealth = 0;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            EternalClash.Audio.GameAudio.PlayEnemy(gameObject, EternalClash.Audio.EnemySound.Hit);

            if (currentHealth <= 0)
                Die();
        }

        private void Die()
        {
            EternalClash.Audio.GameAudio.PlayEnemy(gameObject, EternalClash.Audio.EnemySound.Death);
            Debug.Log("[ENEMY] " + gameObject.name + " died");

            // Viec destroy GameObject do EnemyBase.Die() quyet dinh (qua EnemyDeathHandler neu co)
            OnDeath?.Invoke();
        }
    }
}
