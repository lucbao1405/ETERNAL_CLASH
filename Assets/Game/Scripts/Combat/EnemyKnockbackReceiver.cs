using UnityEngine;
using EternalClash.Enemy;

namespace EternalClash.Combat
{
    public class EnemyKnockbackReceiver : MonoBehaviour
    {
        // Day lui nho cho MOI don trung (Postknight: quai bat lui nhe moi don).
        // ponytail: gia tri cu 1.0 khong dung thi lam quai khong bao gio vuot
        // lai tam danh vi quai chi tien lai bang world scroll. Muon day manh
        // hon (phan don/charge) thi set knockbackDistance tai cho goi.
        public float knockbackDistance = 0.35f;
        public float knockbackSpeed = 8f;

        private Vector2 target;
        private bool knocked;
        private float lockedY;
        private EnemyMover mover;
        private Rigidbody2D rb;

        private void Awake()
        {
            mover = GetComponent<EnemyMover>();
            rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (!knocked) return;

            // Quai la kinematic Rigidbody2D (EnemyMover dieu khien bang
            // MovePosition) nen phai di qua rb: ghi truc tiep transform.position
            // bi pose cua rb ghi de lai ngay buoc vat ly ke tiep.
            float currentX = rb != null ? rb.position.x : transform.position.x;
            Vector2 next = Vector2.MoveTowards(new Vector2(currentX, lockedY), target, knockbackSpeed * Time.deltaTime);

            if (rb != null)
                rb.MovePosition(next);
            else
                transform.position = new Vector3(next.x, lockedY, transform.position.z);

            if (Mathf.Abs(currentX - target.x) < 0.05f)
            {
                if (rb != null)
                {
                    rb.position = target;
                    rb.MovePosition(target);
                }
                transform.position = new Vector3(target.x, lockedY, transform.position.z);
                knocked = false;
            }
        }

        public void ApplyEnemyKnockback(Vector2 direction)
        {
            lockedY = transform.position.y;

            float xDirection = Mathf.Sign(direction.x);
            if (Mathf.Abs(xDirection) < 0.01f)
                xDirection = 1f;

            target = new Vector2(transform.position.x + xDirection * knockbackDistance, lockedY);
            knocked = true;

            if (mover != null)
                mover.PauseMovement(0.5f);
        }
    }
}
