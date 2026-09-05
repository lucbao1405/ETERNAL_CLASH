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

        public static bool PlayerDead { get; private set; }

        public event Action<int, int> OnHealthChanged;
        public event Action OnDeath;

        private void Awake()
        {
            currentHealth = maxHealth;

            // Chi reset khi dung Player, khong reset khi Enemy spawn
            if (CompareTag("Player"))
            {
                PlayerDead = false;
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(int damage)
        {
            if (IsDead) return;

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
            if (IsDead) return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void IncreaseMaxHealth(int amount)
        {
            maxHealth += amount;
            currentHealth += amount;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        protected virtual void Die()
        {
            // Enemy chet chi xu ly chet cua Enemy
            if (!CompareTag("Player"))
            {
                Debug.Log(gameObject.name + " died");
                OnDeath?.Invoke();
                return;
            }

            // Chi Player chet moi dung game
            PlayerDead = true;
            Debug.Log("[GAME] Player died - stop battle");

            Time.timeScale = 0f;
            OnDeath?.Invoke();
        }
    }
}
