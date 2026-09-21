using UnityEngine;
using System;
using EternalClash.Combat;
using EternalClash.Enemy;

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

        // RevivePending dang mo offer hoi sinh: chua coi la chet de PlayerDeathHandler
        // (event + Update poll) chua kip chay flow thua trong luc offer hien.
        public bool RevivePending { get; private set; }
        public bool IsDead => currentHealth <= 0 && !RevivePending;

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

                // Tran moi -> cho phep offer hoi sinh quay lai.
                EternalClash.Monetization.ReviveOffer.ResetForBattle();

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

            // Dong bo voi Town: vao tran voi dung luong mau dang co (dang hoi mau thi
            // khong day). Dat thang o day, khong qua TakeDamage de khong phat am thanh /
            // animation bi danh luc moi vao tran.
            if (CompareTag("Player"))
            {
                var condition = EternalClash.Core.PlayerConditionSystem.Instance;
                if (condition != null)
                    currentHealth = condition.GetBattleStartHp(maxHealth);
            }

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

            // Victory/defeat owns the result and dialogue flow. Ignore late
            // enemy hits while the battle scene is being wrapped up; otherwise
            // a delayed hit can open the revive offer over the NPC dialogue.
            if (CompareTag("Player") && StageManager.Instance != null &&
                StageManager.Instance.CurrentState != StageManager.StageState.Running)
                return;

            currentHealth -= damage;

            if (currentHealth < 0)
                currentHealth = 0;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
            else if (CompareTag("Player"))
            {
                EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.PlayerHurt);
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

            // Result/chest/dialogue owns the battle after the stage leaves
            // Running. A delayed damage callback must not open Revive over it.
            if (StageManager.Instance != null &&
                StageManager.Instance.CurrentState != StageManager.StageState.Running)
                return;

            // Offer hoi sinh (monetization): chan lai truoc khi danh dau chet.
            // Trong luc offer mo, TakeDamage -> Die() goi lai thi RevivePending
            // chan de khong mo offer lan thu hai.
            if (RevivePending)
                return;

            if (EternalClash.Monetization.ReviveOffer.TryOffer(this))
                return;

            ConfirmDeath();
        }

        internal void MarkRevivePending()
        {
            RevivePending = true;
        }

        /// <summary>Hoi sinh voi 50% HP - goi khi nguoi choi xem xong quang cao.</summary>
        public void ReviveAtHalfHealth()
        {
            RevivePending = false;
            currentHealth = Mathf.Max(1, maxHealth / 2);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            PushThreatsAwayOnRevive();
        }

        /// <summary>
        /// Vua hoi sinh: day quai gan ra xa khoang cach an toan, huy don danh
        /// dang vung va don docbay trong khong de khong an damage ngay lap tuc
        /// khi song lai (tran bi dong bang khi offer mo nen dan van giu nguyen
        /// vi tri va se bay tiep ngay sau khi tran chay lai).
        /// </summary>
        private void PushThreatsAwayOnRevive()
        {
            const float safeDistance = 4f;

            foreach (EnemyMover mover in FindObjectsOfType<EnemyMover>())
            {
                mover.PushAwayFrom(transform.position, safeDistance);
                // World scroll keo quai quay lai rat nhanh (~2,5 u/s) nen cho
                // quai dung yen them mot nhip de nguoi choi co khoang tho.
                mover.PauseMovement(1.5f);

                var attack = mover.GetComponent<EnemyAttack>();
                if (attack != null)
                    attack.CancelPendingAttack();
            }

            foreach (EnemyProjectile projectile in FindObjectsOfType<EnemyProjectile>())
                Destroy(projectile.gameObject);
        }

        /// <summary>
        /// Chot cai chet nhu ban cu (dung boi flow thua). Goi khi offer hoi sinh
        /// bi tu choi hoac quang cao that bai.
        /// </summary>
        public void ConfirmDeath()
        {
            RevivePending = false;

            // The defeat result flow uses unscaled time. Disable combat through its
            // controller instead of freezing the process before LosePopup can appear.
            PlayerDead = true;
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.PlayerDeath);
            Debug.Log("[GAME] Player died - stop battle");

            OnDeath?.Invoke();
        }
    }
}
