using UnityEngine;
using EternalClash.Character;
using EternalClash.Enemy;
using EternalClash.World;

namespace EternalClash.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class KnockbackReceiver : MonoBehaviour
    {
        public float knockbackDistance = 1.2f;
        public float knockbackDuration = 0.3f;
        public float recoveryTime = 0.25f;
        public float enemyStunTime = 0.5f;

        private Rigidbody2D rb;
        private CharacterStateMachine stateMachine;
        private EnemyMover enemyMover;
        private bool isKnockback;
        private Vector2 targetPosition;
        private Vector2 originalPosition;
        private float knockbackElapsed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stateMachine = GetComponent<CharacterStateMachine>();
            enemyMover = GetComponent<EnemyMover>();
        }

        private bool isPlayerHitActive;

        private void Update()
        {
            if (!isKnockback) return;

            knockbackElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(knockbackElapsed / Mathf.Max(0.01f, knockbackDuration));
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            Vector2 nextPosition = Vector2.Lerp(originalPosition, targetPosition, easedT);
            nextPosition.y = originalPosition.y;
            transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);

            if (knockbackElapsed >= knockbackDuration)
            {
                transform.position = new Vector3(targetPosition.x, originalPosition.y, transform.position.z);
                isKnockback = false;
                knockbackElapsed = 0f;
                if (stateMachine != null)
                    Invoke(nameof(RecoverFromHit), recoveryTime);
            }
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            if (isKnockback) return;
            if (CompareTag("Player") && isPlayerHitActive) return;

            originalPosition = transform.position;
            knockbackElapsed = 0f;

            if (stateMachine != null)
                stateMachine.ChangeState(CharacterState.Hit);
            if (enemyMover != null)
                enemyMover.PauseMovement(enemyStunTime);

            if (CompareTag("Player"))
            {
                float horizontalDirection = Mathf.Sign(direction.x);
                if (Mathf.Abs(horizontalDirection) < 0.01f)
                    horizontalDirection = 1f;

                isPlayerHitActive = true;

                WorldPullbackApplier.ShiftWorldAndEnemies(
                    new Vector3(-horizontalDirection * Mathf.Min(knockbackDistance, 0.35f), 0f, 0f));

                if (DistanceProgress.Instance != null)
                    DistanceProgress.Instance.ReduceDistance(knockbackDistance);

                Invoke(nameof(RecoverFromHit), recoveryTime);
                return;
            }

            targetPosition = new Vector2(
                originalPosition.x + Mathf.Sign(direction.x) * knockbackDistance,
                originalPosition.y);
            isKnockback = true;
        }

        private void RecoverFromHit()
        {
            if (CompareTag("Player"))
                isPlayerHitActive = false;

            if (stateMachine != null && !isKnockback)
                stateMachine.ChangeState(CharacterState.Idle);
        }
    }

    public static class WorldPullbackApplier
    {
        public static void ShiftWorldAndEnemies(Vector3 delta)
        {
            delta.y = 0f;
            var scroller = Object.FindObjectOfType<WorldScroller>();
            if (scroller != null)
                scroller.ApplyKnockbackShift(delta);
        }
    }
}
