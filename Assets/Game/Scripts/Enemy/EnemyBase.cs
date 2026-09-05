using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Enemy
{
    public class EnemyBase : MonoBehaviour
    {
        public int attackDamage = 5;
        public float moveSpeed = 1f;

        [SerializeField] private GameObject coinPrefab;

        private EnemyHealthSystem healthSystem;
        private CharacterStateMachine stateMachine;
        private bool isDead;

        protected virtual void Awake()
        {
            healthSystem = GetComponent<EnemyHealthSystem>();
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

            // Kich hoat roi do theo bang loot (neu Enemy co gan EnemyLootDropper).
            // Kiem tra null an toan - Enemy nao chua gan component nay se don gian bo qua.
            EnemyLootDropper lootDropper = GetComponent<EnemyLootDropper>();
            if (lootDropper != null)
                lootDropper.DropLoot();

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
