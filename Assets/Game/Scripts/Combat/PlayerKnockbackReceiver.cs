using UnityEngine;
using EternalClash.World;

namespace EternalClash.Combat
{
    public class PlayerKnockbackReceiver : MonoBehaviour
    {
        [SerializeField] private float knockbackDistance = 0.45f;

        public void ApplyKnockback(Vector2 direction, float force)
        {
            var scroller = FindObjectOfType<WorldScroller>();
            if (scroller != null)
                scroller.TriggerKnockback(force);

            DistanceProgress.Instance?.ReduceDistance(force);
        }

        public void ApplyPlayerHit(Vector2 direction)
        {
            if (direction == Vector2.zero)
                return;

            var scroller = FindObjectOfType<WorldScroller>();
            if (scroller != null)
                scroller.TriggerKnockback(knockbackDistance);

            DistanceProgress.Instance?.ReduceDistance(knockbackDistance);
            Debug.Log("[PLAYER] Hit knockback applied via environment scroll");
        }
    }
}
