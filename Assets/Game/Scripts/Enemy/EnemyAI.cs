using UnityEngine;
using EternalClash.Character;
using EternalClash.Combat;

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
            enemyAttack = GetComponent<EnemyAttack>();
        }

        private void Update()
        {
            if (currentState == AIState.Dead || player == null)
                return;

            DamageReceiver playerReceiver = player.GetComponent<DamageReceiver>();
            if (playerReceiver != null && playerReceiver.IsDead())
            {
                currentState = AIState.Dead;
                return;
            }

            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= attackRange)
            {
                currentState = AIState.Attack;

                EnemyMover mover = GetComponent<EnemyMover>();
                if (mover != null)
                    mover.StopMovement();

                if (EnemyFormationManager.Instance != null)
                    EnemyFormationManager.Instance.LockFormation();

                AttackState();
            }
            else
            {
                currentState = AIState.Chase;
            }
        }

        private EnemyAttack enemyAttack;

        private void AttackState()
        {
            timer += Time.deltaTime;

            if (timer >= attackInterval)
            {
                timer = 0;

                if (stateMachine != null)
                    stateMachine.ChangeState(CharacterState.Attack);

                if (enemyAttack != null)
                    enemyAttack.AttackPlayer();
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
