using UnityEngine;

namespace EternalClash.Player
{
    public class ChargeCollisionController : MonoBehaviour
    {
        [SerializeField] private float stopDistance = 0.7f;

        public Vector3 GetStopPosition(Vector3 playerPosition, Vector3 enemyPosition)
        {
            Vector3 direction = (playerPosition - enemyPosition).normalized;
            return enemyPosition + direction * stopDistance;
        }
    }
}
