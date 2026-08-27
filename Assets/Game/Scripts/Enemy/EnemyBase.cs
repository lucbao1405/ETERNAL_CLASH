using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Enemy
{
    public class EnemyBase : MonoBehaviour
    {
        public int attackDamage = 5;
        public float moveSpeed = 1f;

        private HealthSystem healthSystem;
        private CharacterStateMachine stateMachine;

        protected virtual void Awake()
        {
            healthSystem = GetComponent<HealthSystem>();
            stateMachine = GetComponent<CharacterStateMachine>();
        }

        public virtual void ReceiveDamage(int damage)
        {
            if (healthSystem == null)
                return;

            healthSystem.TakeDamage(damage);

            if (stateMachine != null)
                stateMachine.ChangeState(CharacterState.Hit);

            if (healthSystem.IsDead)
                Die();
        }

        protected virtual void Die()
        {
            if (stateMachine != null)
                stateMachine.ChangeState(CharacterState.Dead);

            Destroy(gameObject, 0.5f);
        }

        public int GetCurrentHP()
        {
            return healthSystem != null ? healthSystem.CurrentHealth : 0;
        }
    }
}
