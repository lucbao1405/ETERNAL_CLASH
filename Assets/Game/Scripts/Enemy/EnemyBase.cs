using UnityEngine;
using System;
using EternalClash.Character;

namespace EternalClash.Enemy
{
    public class EnemyBase : MonoBehaviour
    {
        public int maxHP = 20;
        public int attackDamage = 5;
        public float moveSpeed = 1f;

        protected int currentHP;
        private CharacterStateMachine stateMachine;

        public event Action<int, int> OnHealthChanged;

        protected virtual void Awake()
        {
            currentHP = maxHP;
            stateMachine = GetComponent<CharacterStateMachine>();
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }

        public virtual void TakeDamage(int damage)
        {
            currentHP -= damage;

            if (currentHP < 0)
                currentHP = 0;

            OnHealthChanged?.Invoke(currentHP, maxHP);

            if (stateMachine != null)
                stateMachine.ChangeState(CharacterState.Hit);

            if (currentHP <= 0)
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
            return currentHP;
        }
    }
}
