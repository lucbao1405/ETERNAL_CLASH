using UnityEngine;

namespace EternalClash.Combat
{
    public class AttackTrigger : MonoBehaviour
    {
        public BasicAttackSystem basicAttackSystem;

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                basicAttackSystem.SetTarget(other.gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                basicAttackSystem.ClearTarget();
            }
        }
    }
}
