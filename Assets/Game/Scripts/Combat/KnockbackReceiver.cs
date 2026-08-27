using UnityEngine;
using EternalClash.Character;
using EternalClash.Enemy;

namespace EternalClash.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class KnockbackReceiver : MonoBehaviour
    {
        public float knockbackDistance = 1.2f;
        public float knockbackDuration = 0.15f;
        public float recoveryTime = 0.25f;
        public float enemyStunTime = 0.5f;

        private Rigidbody2D rb;
        private CharacterStateMachine stateMachine;
        private EnemyMover enemyMover;

        private bool isKnockback;
        private Vector2 targetPosition;
        private Vector2 originalPosition;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stateMachine = GetComponent<CharacterStateMachine>();
            enemyMover = GetComponent<EnemyMover>();
        }

        private void Update()
        {
            if (!isKnockback)
                return;

            transform.position = Vector2.MoveTowards(
                transform.position,
                targetPosition,
                knockbackDistance / knockbackDuration * Time.deltaTime
            );

            if (Vector2.Distance(transform.position, targetPosition) < 0.01f)
            {
                isKnockback = false;
                Invoke(nameof(ReturnToCombatPosition), recoveryTime);
            }
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            if (isKnockback)
                return;

            originalPosition = transform.position;

            if (stateMachine != null)
                stateMachine.ChangeState(CharacterState.Hit);

            // Enemy bi stun khi trung don, khong chase ngay lap tuc
            if (enemyMover != null)
                enemyMover.PauseMovement(enemyStunTime);

            targetPosition = originalPosition + direction.normalized * knockbackDistance;
            isKnockback = true;
        }

        private void ReturnToCombatPosition()
        {
            transform.position = originalPosition;

            if (stateMachine != null)
                stateMachine.ChangeState(CharacterState.Idle);
        }
    }
}
