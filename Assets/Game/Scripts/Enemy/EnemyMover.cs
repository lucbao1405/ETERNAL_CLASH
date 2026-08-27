using UnityEngine;

namespace EternalClash.Enemy
{
    public class EnemyMover : MonoBehaviour
    {
        public float stopDistance = 1.2f;

        private EnemyBase enemyBase;
        private Transform player;

        private void Awake()
        {
            enemyBase = GetComponent<EnemyBase>();
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        private void Update()
        {
            if (player == null || enemyBase == null)
                return;

            float distance = Vector2.Distance(transform.position, player.position);

            if (distance > stopDistance)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    player.position,
                    enemyBase.moveSpeed * Time.deltaTime
                );
            }
        }
    }
}
