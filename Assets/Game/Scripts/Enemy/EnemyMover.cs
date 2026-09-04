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
        private Camera mainCamera;
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
            mainCamera = Camera.main;
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.constraints |= RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
                rb.position = new Vector2(rb.position.x, lockedY);
            }
        }

        private void FixedUpdate()
        {
            if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null || isPaused) return;
            if (EnemyFormationManager.Instance != null && EnemyFormationManager.Instance.IsLocked()) return;
            if (controller != null && !controller.canMove) return;

            float currentX = rb != null ? rb.position.x : transform.position.x;
            float side = currentX >= player.position.x ? 1f : -1f;

            float worldVelocityX = worldScroller != null ? worldScroller.GetWorldVelocity().x : 0f;
            float finalVelocity = worldVelocityX;
            float distance = Mathf.Abs(player.position.x - currentX);
            float effectiveStop = isArcher ? GetArcherStopDistance(side) : stopDistance;

            if (isChargePulling)
            {
                if (!isArcher) finalVelocity -= chargePullSpeed;
            }
            else if (distance > effectiveStop && !isArcher)
            {
                float directionToPlayer = Mathf.Sign(player.position.x - currentX);
                finalVelocity += directionToPlayer * moveSpeed;
            }

            float nextX = currentX + finalVelocity * Time.fixedDeltaTime;

            // Luon kep theo PHIA HIEN TAI cua quai so voi Player, bat ke |khoang cach|
            // dang lon hay nho hon effectiveStop. Truoc day chi kep khi khoang cach da
            // VUOT QUA effectiveStop, nen trong "vung chet" [-effectiveStop, effectiveStop]
            // hoan toan khong kep gi - neu bi keo voi van toc lon (vd Charge keo lui ca
            // dan quai), quai co the xuyen thang qua Player sang phia ben kia trong vai
            // physics step, tao cam giac "nhay qua sau lung Player" du chua chet.
            float stopX = player.position.x + side * effectiveStop;

            if (side > 0f)
                nextX = Mathf.Max(nextX, stopX);
            else
                nextX = Mathf.Min(nextX, stopX);

            // Enemy is permanently constrained to its original Y.
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

        // Quai cung dung yen bang khoang cach co dinh (archerStopDistance), nhung gia tri
        // do duoc thiet ke cho 1 ty le man hinh cu the. Neu man hinh thuc te hep hon (vd
        // portrait tren mobile) thi diem dung se roi ra ngoai vung camera nhin thay. Ham
        // nay rut ngan khoang dung lai cho vua trong khung hinh thuc te, giu nguyen
        // archerStopDistance lam gioi han toi da khi man hinh du rong.
        private float GetArcherStopDistance(float side)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null || player == null)
                return archerStopDistance;

            float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
            float cameraEdgeX = mainCamera.transform.position.x + side * halfWidth;
            float maxVisibleDistance = Mathf.Abs(cameraEdgeX - player.position.x) * 0.85f;

            return Mathf.Min(archerStopDistance, Mathf.Max(maxVisibleDistance, 0.5f));
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
