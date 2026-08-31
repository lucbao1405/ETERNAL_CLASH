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

        public void DealDamage(GameObject target, int amount, DamageSource source)
        {
            if (target == null || amount <= 0)
                return;

            // Player damage can have shield/block/defense processing.
            // Route it through DamageReceiver instead of directly reducing HP.
            if (source == DamageSource.EnemyAttack)
            {
                var receiver = target.GetComponent<DamageReceiver>();
                if (receiver != null)
                {
                    receiver.TakeDamage(amount);
                    Debug.Log($"[DAMAGE] {source}: {amount}");
                    return;
                }
            }

            var enemyReceiver = target.GetComponent<EnemyDamageReceiver>();
            if (enemyReceiver != null && source != DamageSource.EnemyAttack)
            {
                enemyReceiver.TakeDamage(amount);
                Debug.Log($"[DAMAGE] {source}: {amount}");
                return;
            }

            var health = target.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(amount);
                Debug.Log($"[DAMAGE] {source}: {amount}");
            }
        }
    }
}
