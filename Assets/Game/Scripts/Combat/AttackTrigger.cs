using UnityEngine;

namespace EternalClash.Combat
{
    [RequireComponent(typeof(Collider2D))]
    public class AttackTrigger : MonoBehaviour
    {
        public BasicAttackSystem basicAttackSystem;

        private void Awake()
        {
            EnsureBasicAttackSystem();
        }

        private void Start()
        {
            EnsureBasicAttackSystem();
        }

        private void EnsureBasicAttackSystem()
        {
            if (basicAttackSystem == null)
            {
                basicAttackSystem = GetComponent<BasicAttackSystem>();
                if (basicAttackSystem == null)
                    basicAttackSystem = GetComponentInParent<BasicAttackSystem>();
                if (basicAttackSystem == null)
                {
                    var player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                        basicAttackSystem = player.GetComponent<BasicAttackSystem>();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;

            if (other.CompareTag("Enemy"))
            {
                if (basicAttackSystem == null)
                    EnsureBasicAttackSystem();

                if (basicAttackSystem != null)
                {
                    basicAttackSystem.SetTarget(other.gameObject);
                }

                // Stop player movement
                var playerController = GetComponentInParent<EternalClash.Player.PlayerController>();
                if (playerController == null)
                {
                    var p = GameObject.FindGameObjectWithTag("Player");
                    if (p != null) playerController = p.GetComponent<EternalClash.Player.PlayerController>();
                }

                if (playerController != null)
                    playerController.StopMovement();

                // Stop enemy movement
                var enemyMover = other.GetComponent<EternalClash.Enemy.EnemyMover>();
                if (enemyMover != null)
                    enemyMover.StopMovement();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other == null) return;

            if (other.CompareTag("Enemy"))
            {
                if (basicAttackSystem != null)
                {
                    basicAttackSystem.ClearTarget();
                }

                var playerController = GetComponentInParent<EternalClash.Player.PlayerController>();
                if (playerController == null)
                {
                    var p = GameObject.FindGameObjectWithTag("Player");
                    if (p != null) playerController = p.GetComponent<EternalClash.Player.PlayerController>();
                }

                if (playerController != null)
                    playerController.ResumeMovement();

                var enemyMover = other.GetComponent<EternalClash.Enemy.EnemyMover>();
                if (enemyMover != null)
                    enemyMover.ResumeMovement();
            }
        }
    }
}
