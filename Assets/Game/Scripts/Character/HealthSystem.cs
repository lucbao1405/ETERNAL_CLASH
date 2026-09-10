using UnityEngine;
using System;

namespace EternalClash.Character
{
    public class HealthSystem : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        private int currentHealth;

        /// <summary>
        /// Max HP goc tren prefab, chua cong bonus. Giu rieng de khi ap bonus moi
        /// co the tinh lai tu dau thay vi cong don len gia tri da co bonus.
        /// </summary>
        private int baseMaxHealth;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => currentHealth <= 0;

        public static bool PlayerDead { get; private set; }

        public event Action<int, int> OnHealthChanged;
        public event Action OnDeath;

        private void Awake()
        {
            baseMaxHealth = maxHealth;

            // Chi reset khi dung Player, khong reset khi Enemy spawn
            if (CompareTag("Player"))
            {
                PlayerDead = false;

                // Cong Max HP tu diem VIT da luu. Phai lam o day thay vi de
                // PlayerStatSystem day vao: Player duoc spawn lai moi tran nen
                // maxHealth luon tro ve gia tri prefab, bonus cong truoc do se mat.
                var stats = EternalClash.Village.PlayerStatSystem.Instance;
                if (stats != null)
                {
                    // Bao lai mau goc de UI o Town (khong co Player) hien dung tong mau.
                    stats.RegisterPlayerBaseMaxHealth(baseMaxHealth);
                    maxHealth = baseMaxHealth + stats.BonusMaxHealth;
                }
            }

            currentHealth = maxHealth;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Dat lai Max HP = mau goc tren prefab + bonus. Dat lai chu khong cong don,
        /// nen goi bao nhieu lan cung ra cung mot ket qua. Mau hien tai duoc cong
        /// them dung phan bonus vua tang, va khong bao gio vuot qua Max HP moi.
        /// </summary>
        public void ApplyBonusMaxHealth(int bonus)
        {
            if (baseMaxHealth <= 0)
                baseMaxHealth = maxHealth;

            int previousMax = maxHealth;
            maxHealth = baseMaxHealth + Mathf.Max(0, bonus);

            int gained = maxHealth - previousMax;
            if (gained > 0)
                currentHealth += gained;

            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
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

            // The defeat result flow uses unscaled time. Disable combat through its
            // controller instead of freezing the process before LosePopup can appear.
            PlayerDead = true;
            Debug.Log("[GAME] Player died - stop battle");

            OnDeath?.Invoke();
        }
    }
}
