using UnityEngine;

namespace EternalClash.Enemy
{
    public class EnemyMover : MonoBehaviour
    {
        public float stopDistance = 1.2f;

        [SerializeField] private float moveSpeed = 1f;
        private EnemyController controller;
        private Transform player;
        private bool isPaused;

        private void Awake()
        {
            controller = GetComponent<EnemyController>();
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        private void Update()
        {
            if (player == null || isPaused)
                return;

            if (EnemyFormationManager.Instance != null && EnemyFormationManager.Instance.IsLocked())
                return;

            if (controller != null && !controller.canMove)
                return;

            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= stopDistance)
                return;

            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                moveSpeed * Time.deltaTime
            );
        }

        public void StopMovement()
        {
            isPaused = true;
        }

        public void ResumeMovement()
        {
            isPaused = false;
        }

        public void PauseMovement(float duration)
        {
            isPaused = true;
            Invoke(nameof(ResumeMovement), duration);
        }
    }
}
