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
        private float lockedY;
        private EnemyMover mover;

        private void Awake()
        {
            mover = GetComponent<EnemyMover>();
        }

        private void Update()
        {
            if (!knocked) return;

            Vector3 current = transform.position;
            Vector2 next = Vector2.MoveTowards(new Vector2(current.x, lockedY), target, knockbackSpeed * Time.deltaTime);
            transform.position = new Vector3(next.x, lockedY, current.z);

            if (Mathf.Abs(transform.position.x - target.x) < 0.05f)
            {
                transform.position = new Vector3(target.x, lockedY, current.z);
                knocked = false;
            }
        }

        public void ApplyEnemyKnockback(Vector2 direction)
        {
            lockedY = transform.position.y;

            float xDirection = Mathf.Sign(direction.x);
            if (Mathf.Abs(xDirection) < 0.01f)
                xDirection = 1f;

            target = new Vector2(transform.position.x + xDirection * knockbackDistance, lockedY);
            knocked = true;

            if (mover != null)
                mover.PauseMovement(0.5f);
        }
    }
}
