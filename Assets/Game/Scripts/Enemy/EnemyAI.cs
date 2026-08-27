using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Enemy
{
    public class EnemyAI : MonoBehaviour
    {
        public enum EnemyType
        {
            Melee,
            Ranged,
            Charger
        }

        public enum AIState
        {
            Chase,
            Attack,
            Hit,
            Dead
        }

        public EnemyType type;
        public AIState currentState = AIState.Chase;

        public float attackInterval = 2f;
        public float attackRange = 1.2f;

        private float timer;
        private Transform player;
        private CharacterStateMachine stateMachine;

        private void Awake()
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null)
                player = obj.transform;

            stateMachine = GetComponent<CharacterStateMachine>();
        }

        private void Update()
        {
            if (currentState == AIState.Dead || player == null)
                return;

            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= attackRange)
            {
                currentState = AIState.Attack;
                AttackState();
            }
            else
            {
                currentState = AIState.Chase;
            }
        }

        private void AttackState()
        {
            timer += Time.deltaTime;

            if (timer >= attackInterval)
            {
                timer = 0;

                if (stateMachine != null)
                    stateMachine.ChangeState(CharacterState.Attack);

                PerformAttack();
            }
        }

        private void PerformAttack()
        {
            switch (type)
            {
                case EnemyType.Melee:
                    Debug.Log("Enemy melee attack");
                    break;

                case EnemyType.Ranged:
                    Debug.Log("Enemy shoot projectile");
                    break;

                case EnemyType.Charger:
                    Debug.Log("Enemy charge attack");
                    break;
            }
        }

        public void SetHitState()
        {
            currentState = AIState.Hit;
        }

        public void Die()
        {
            currentState = AIState.Dead;
        }
    }
}
