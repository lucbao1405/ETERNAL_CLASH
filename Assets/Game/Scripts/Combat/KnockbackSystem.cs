using UnityEngine;

namespace EternalClash.Combat
{
    public class KnockbackSystem : MonoBehaviour
    {
        public float knockbackForce = 3f;

        public void ApplyKnockback(Transform target, Vector2 direction)
        {
            target.position += (Vector3)(direction.normalized * knockbackForce);
        }
    }
}
