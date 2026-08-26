using UnityEngine;

namespace EternalClash.Enemy
{
    public class EnemyMover : MonoBehaviour
    {
        public float moveSpeed = 2.5f;

        private void Update()
        {
            transform.position += Vector3.left * moveSpeed * Time.deltaTime;
        }
    }
}
