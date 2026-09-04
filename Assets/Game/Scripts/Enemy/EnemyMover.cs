using UnityEngine;

namespace EternalClash.Enemy
{
    public class EnemyMover : MonoBehaviour
    {
        public float stopDistance = 1.2f;
        [SerializeField] private float archerStopDistance = 4.5f;
        [SerializeField] private bool isArcher = false;
        [SerializeField] private float moveSpeed = 1f;
        private EnemyController controller;
        private Rigidbody2D rb;
        private Transform player;
        private EternalClash.World.WorldScroller worldScroller;
        private bool isPaused;
        private bool isChargePulling;
        private float chargePullSpeed;
        private float lockedY;

        private void Awake()
        {
            controller = GetComponent<EnemyController>();
            rb = GetComponent<Rigidbody2D>();
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            worldScroller = FindObjectOfType<EternalClash.World.WorldScroller>();
            if (rb != null)
            {
                lockedY = rb.position.y;
                rb.gravityScale = 0f;
                rb.constraints |= RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            }
            else
            {
                lockedY = transform.position.y;
            }
        }

        private void FixedUpdate()
        {
            if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null || isPaused) return;
            if (EnemyFormationManager.Instance != null && EnemyFormationManager.Instance.IsLocked()) return;
            if (controller != null && !controller.canMove) return;

            float currentX = rb != null ? rb.position.x : transform.position.x;
            float worldVelocityX = worldScroller != null ? worldScroller.GetWorldVelocity().x : 0f;

            if (isArcher)
            {
                // Archer chi tha troi theo dung toc do cuon cua map (giong nen/background),
                // KHONG tu dung lai o mot khoang co dinh nua - de no tiep tuc troi qua vi
                // tri Player, cho phep Player tien vao danh giap la duoc thay vi bi chan
                // vinh vien o xa. Van con ban ten binh thuong trong luc troi qua neu nam
                // trong EnemyAttack.attackRange.
                float driftX = currentX + worldVelocityX * Time.fixedDeltaTime;
                MoveTo(driftX);
                return;
            }

            float side = currentX >= player.position.x ? 1f : -1f;
            float finalVelocity = worldVelocityX;
            float distance = Mathf.Abs(player.position.x - currentX);

            if (isChargePulling)
            {
                finalVelocity -= chargePullSpeed;
            }
            else if (distance > stopDistance)
            {
                float directionToPlayer = Mathf.Sign(player.position.x - currentX);
                finalVelocity += directionToPlayer * moveSpeed;
            }

            float nextX = currentX + finalVelocity * Time.fixedDeltaTime;

            // Luon kep theo PHIA HIEN TAI cua quai so voi Player, bat ke |khoang cach|
            // dang lon hay nho hon stopDistance. Truoc day chi kep khi khoang cach da
            // VUOT QUA stopDistance, nen trong "vung chet" [-stopDistance, stopDistance]
            // hoan toan khong kep gi - neu bi keo voi van toc lon (vd Charge keo lui ca
            // dan quai), quai co the xuyen thang qua Player sang phia ben kia trong vai
            // physics step, tao cam giac "nhay qua sau lung Player" du chua chet.
            float stopX = player.position.x + side * stopDistance;

            if (side > 0f)
                nextX = Mathf.Max(nextX, stopX);
            else
                nextX = Mathf.Min(nextX, stopX);

            MoveTo(nextX);
        }

        // Enemy is permanently constrained to its original Y.
        private void MoveTo(float nextX)
        {
            if (rb != null)
            {
                Vector2 current = rb.position;
                if (!Mathf.Approximately(current.y, lockedY))
                    rb.position = new Vector2(current.x, lockedY);
                rb.MovePosition(new Vector2(nextX, lockedY));
            }
            else
            {
                transform.position = new Vector3(nextX, lockedY, transform.position.z);
            }
        }

        public void StopMovement() => isPaused = true;
        public void ResumeMovement() => isPaused = false;
        public void PauseMovement(float duration){isPaused=true;Invoke(nameof(ResumeMovement),duration);}
        public void EnableChargePull(float speed){isChargePulling=true;chargePullSpeed=speed;}
        public void DisableChargePull(){isChargePulling=false;chargePullSpeed=0f;}
        public bool IsChargePulling=>isChargePulling;
        public float MoveSpeed=>moveSpeed;
        public bool IsArcher=>isArcher;
        public float ArcherStopDistance=>archerStopDistance;
    }
}
