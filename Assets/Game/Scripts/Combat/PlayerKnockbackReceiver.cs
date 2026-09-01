using UnityEngine;

namespace EternalClash.Combat
{
    public class PlayerKnockbackReceiver : MonoBehaviour
    {
        [SerializeField] private float knockbackDistance = 0.45f;
        [SerializeField] private float knockbackSpeed = 6f;

        private Vector2 target;
        private bool active;

        private void Update()
        {
            if (!active) return;

            transform.position = Vector2.MoveTowards(
                transform.position,
                target,
                knockbackSpeed * Time.deltaTime
            );

            if (Vector2.Distance(transform.position, target) < 0.02f)
                active = false;
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            var scroller = FindObjectOfType<EternalClash.World.WorldScroller>();
            if (scroller != null)
                scroller.transform.position += new Vector3(-direction.x * force, 0f, 0f);

            DistanceProgress.Instance?.ReduceDistance(force);
        }

        public void ApplyPlayerHit(Vector2 direction)
        {
            if (direction == Vector2.zero)
                return;

            target = (Vector2)transform.position + direction.normalized * knockbackDistance;
            active = true;

            // Apply slight progress penalty on hit
            DistanceProgress.Instance?.ReduceDistance(knockbackDistance);
            Debug.Log("[PLAYER] Hit knockback applied with progress penalty");
        }
    }
}
