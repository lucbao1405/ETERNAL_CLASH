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

            Vector3 prevPos = transform.position;
            transform.position = Vector2.MoveTowards(
                transform.position,
                targetPosition,
                knockbackDistance / knockbackDuration * Time.deltaTime
            );

            // When PLAYER is knocked back, shift only the world root by the same delta so the
            // map visibly recoils with the player. Enemies are NOT shifted: melee mobs
            // keep chasing and ranged mobs (e.g. Goblin Archer) keep attacking at range.
            // World scroll continues normally; once the player recovers the map resumes.
            if (CompareTag("Player"))
            {
                Vector3 delta = transform.position - prevPos;
                if (delta.sqrMagnitude > 0f)
                    WorldPullbackApplier.ShiftWorldAndEnemies(delta);
            }

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

            if (enemyMover != null)
                enemyMover.PauseMovement(enemyStunTime);

            if (CompareTag("Player"))
            {
                var scroller = Object.FindObjectOfType<EternalClash.World.WorldScroller>();
                if (scroller != null)
                {
                    Vector3 recoilDelta = new Vector3(-direction.x * knockbackDistance, 0f, 0f);
                    scroller.ApplyKnockbackShift(recoilDelta);
                    Debug.Log("[KNOCKBACK] Player recoil applied to mountain/tree/ground: " + (-direction.x * knockbackDistance));
                }

                if (DistanceProgress.Instance != null)
                    DistanceProgress.Instance.ReduceDistance(knockbackDistance);

                return;
            }

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

    /// <summary>
    /// Static helper: when the player gets knocked back, shift the world root by the
    /// same delta so the map visibly recoils with the player. Enemies keep their world
    /// position and keep attacking normally; archer-type enemies handle their own
    /// "stop when in range" logic inside EnemyMover.
    /// </summary>
    public static class WorldPullbackApplier
    {
        public static void ShiftWorldAndEnemies(Vector3 delta)
        {
            // Only the world root (background + level geometry) shifts. Enemies are
            // intentionally NOT moved: they continue to act in world space so melee mobs
            // keep chasing and ranged mobs (e.g. Goblin Archer) keep attacking at range.
            var scroller = Object.FindObjectOfType<WorldScroller>();
            if (scroller != null)
                scroller.transform.position += delta;
        }
    }
}
