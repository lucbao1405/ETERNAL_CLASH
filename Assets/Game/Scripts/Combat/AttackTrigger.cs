using UnityEngine;

namespace EternalClash.Combat
{
    public class AttackTrigger : MonoBehaviour
    {
        public BasicAttack basicAttack;

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                basicAttack.SetTarget(other.gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                basicAttack.ClearTarget();
            }
        }
    }
}
