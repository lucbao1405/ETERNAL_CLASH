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

                // Khong StopMovement() enemy o day: trigger cham nhau o khoang cach
                // (mep phai circle ~0.885 + mep trai hitbox quai) luon xa hon tam danh
                // 0.85, neu dung thi quai dong bang truoc khi vao tam (Goblin Mage hitbox
                // rong) va khong bao gio duoc resume vi van con overlap.
                // EnemyMover.ClampBeforePlayer moi la noi giu khoang cach dung cua tung loai quai.
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

                // Khong ResumeMovement() enemy: exit co the xa ra khi quai dang bi
                // choang/treo boi he thong khac, resume o day se vo hieu hoa trang thai do.
            }
        }
    }
}
