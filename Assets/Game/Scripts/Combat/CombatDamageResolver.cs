using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Combat
{
    public enum DamageSource
    {
        BasicAttack,
        Charge,
        EnemyAttack,
        Reflect
    }

    public class CombatDamageResolver : MonoBehaviour
    {
        private static CombatDamageResolver _instance;

        public static CombatDamageResolver Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<CombatDamageResolver>();

                if (_instance == null)
                {
                    var obj = new GameObject("_CombatDamageResolver");
                    _instance = obj.AddComponent<CombatDamageResolver>();
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else if (_instance != this)
                Destroy(gameObject);
        }

        /// <summary>
        /// <paramref name="attacker"/> la doi tuong gay sat thuong. Chi can khi muon
        /// Khien phan don ve nguoi danh (GDD 3.3). De trong thi khong phan.
        /// Tham so co gia tri mac dinh nen moi loi goi cu van bien dich binh thuong.
        /// </summary>
        public void DealDamage(GameObject target, int amount, DamageSource source,
                               GameObject attacker = null)
        {
            if (target == null || amount <= 0)
                return;

            // Chi mang chi ap cho don danh cua player (GDD 3.5 - LUCK tang critical).
            // Khong ap cho EnemyAttack va Reflect.
            bool isCritical = false;
            if (source == DamageSource.BasicAttack || source == DamageSource.Charge)
            {
                var stats = Village.PlayerStatSystem.Instance;
                if (stats != null)
                    amount = stats.ApplyCritical(amount, out isCritical);
            }

            // Player damage can have shield/block/defense processing.
            // Route it through DamageReceiver instead of directly reducing HP.
            if (source == DamageSource.EnemyAttack)
            {
                var receiver = target.GetComponent<DamageReceiver>();
                if (receiver != null)
                {
                    receiver.TakeDamage(amount, attacker);
                    Debug.Log($"[DAMAGE] {source}: {amount}{(isCritical ? " (CRIT)" : "")}");
                    return;
                }
            }

            var enemyReceiver = target.GetComponent<EnemyDamageReceiver>();
            if (enemyReceiver != null && source != DamageSource.EnemyAttack)
            {
                enemyReceiver.TakeDamage(amount, isCritical);
                Debug.Log($"[DAMAGE] {source}: {amount}{(isCritical ? " (CRIT)" : "")}");
                return;
            }

            var health = target.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(amount);
                Debug.Log($"[DAMAGE] {source}: {amount}{(isCritical ? " (CRIT)" : "")}");
            }
        }
    }
}
