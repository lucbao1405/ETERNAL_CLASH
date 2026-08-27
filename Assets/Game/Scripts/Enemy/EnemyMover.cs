using UnityEngine;

namespace EternalClash.Enemy
{
    public class EnemyMover : MonoBehaviour
    {
        public float stopDistance = 1.2f;

        private EnemyBase enemyBase;
        private EnemyController controller;
        private Transform player;
        private bool isRetreating;

        private void Awake()
        {
            enemyBase = GetComponent<EnemyBase>();
            controller = GetComponent<EnemyController>();
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        private void Update()
        {
            if (player == null || enemyBase == null)
                return;

            if (controller != null && !controller.canMove)
                return;

            if (isRetreating)
                return;

            float distance = Vector2.Distance(transform.position, player.position);

            // Da vao tam danh: dung ap sat
            if (distance <= stopDistance)
                return;

            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                enemyBase.moveSpeed * Time.deltaTime
            );
        }

        public void PauseMovement(float duration)
        {
            isRetreating = true;
            Invoke(nameof(ResumeMovement), duration);
        }

        private void ResumeMovement()
        {
            isRetreating = false;
        }
    }
}
