using UnityEngine;
using EternalClash.Enemy;

namespace EternalClash.Combat
{
    public class EnemyKnockbackReceiver : MonoBehaviour
    {
        public float knockbackDistance = 1.0f;
        public float knockbackSpeed = 8f;

        private Vector2 target;
        private bool knocked;
        private EnemyMover mover;

        private void Awake()
        {
            mover = GetComponent<EnemyMover>();
        }

        private void Update()
        {
            if (!knocked)
                return;

            transform.position = Vector2.MoveTowards(
                transform.position,
                target,
                knockbackSpeed * Time.deltaTime
            );

            if (Vector2.Distance(transform.position, target) < 0.05f)
                knocked = false;
        }

        public void ApplyEnemyKnockback(Vector2 direction)
        {
            if (direction == Vector2.zero)
                return;

            target = (Vector2)transform.position + direction.normalized * knockbackDistance;
            knocked = true;

            if (mover != null)
                mover.PauseMovement(0.5f);

            Debug.Log("[ENEMY] Knockback applied");
        }
    }
}
