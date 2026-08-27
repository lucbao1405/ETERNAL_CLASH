using UnityEngine;
using System;

namespace EternalClash.Character
{
    public class HealthSystem : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        private int currentHealth;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => currentHealth <= 0;

        public event Action<int, int> OnHealthChanged;

        private void Awake()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(int damage)
        {
            currentHealth -= damage;

            if (currentHealth < 0)
                currentHealth = 0;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        protected virtual void Die()
        {
            Debug.Log(gameObject.name + " died");
        }
    }
}
