using UnityEngine;

namespace EternalClash.Combat
{
    /// <summary>
    /// Chi phat hien muc tieu trong tam danh.
    /// Damage duoc xu ly boi AnimationCombatEvent tai hit frame.
    /// Khong gay damage truc tiep tu collision.
    /// </summary>
    public class PlayerHitCollisionHandler : MonoBehaviour
    {
        private GameObject currentTarget;

        public GameObject CurrentTarget => currentTarget;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                currentTarget = other.gameObject;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject == currentTarget)
            {
                currentTarget = null;
            }
        }
    }
}
