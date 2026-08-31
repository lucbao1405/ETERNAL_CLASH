using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Enemy
{
    public class EnemyBase : MonoBehaviour
    {
        public int attackDamage = 5;
        public float moveSpeed = 1f;

        [SerializeField] private GameObject coinPrefab;

        private HealthSystem healthSystem;
        private CharacterStateMachine stateMachine;
        private bool isDead;

        protected virtual void Awake()
        {
            healthSystem = GetComponent<HealthSystem>();
            stateMachine = GetComponent<CharacterStateMachine>();

            if (healthSystem != null)
                healthSystem.OnDeath += Die;
        }

        protected virtual void OnDestroy()
        {
            if (healthSystem != null)
                healthSystem.OnDeath -= Die;
        }

        public virtual void ReceiveDamage(int damage)
        {
            if (healthSystem == null || isDead)
                return;

            healthSystem.TakeDamage(damage);

            if (stateMachine != null)
                stateMachine.ChangeState(CharacterState.Hit);
        }

        protected virtual void Die()
        {
            if (isDead) return;

            isDead = true;
            EnemyDeathEvent.Raise(gameObject);

            if (EnemyManager.Instance != null)
                EnemyManager.Instance.UnregisterEnemy(gameObject);

            if (stateMachine != null)
                stateMachine.ChangeState(CharacterState.Dead);

            if (coinPrefab != null)
                Instantiate(coinPrefab, transform.position, Quaternion.identity);

            EnemyDeathHandler deathHandler = GetComponent<EnemyDeathHandler>();
            if (deathHandler != null)
                deathHandler.Die();
            else
                Destroy(gameObject);
        }

        public int GetCurrentHP()
        {
            return healthSystem != null ? healthSystem.CurrentHealth : 0;
        }
    }
}
