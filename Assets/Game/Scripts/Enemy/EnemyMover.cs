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
        private bool isPaused;
        private bool isChargePulling;
        private float chargePullSpeed;
        private float lockedY;

        private void Awake()
        {
            controller = GetComponent<EnemyController>();
            rb = GetComponent<Rigidbody2D>();
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            lockedY = transform.position.y;
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

            var scroller = FindObjectOfType<EternalClash.World.WorldScroller>();
            float worldVelocityX = scroller != null ? scroller.GetWorldVelocity().x : 0f;
            float finalVelocity = worldVelocityX;
            float distance = Mathf.Abs(player.position.x - transform.position.x);
            float effectiveStop = isArcher ? archerStopDistance : stopDistance;

            if (isChargePulling)
            {
                if (!isArcher) finalVelocity -= chargePullSpeed;
            }
            else if (distance > effectiveStop && !isArcher)
            {
                float directionToPlayer = Mathf.Sign(player.position.x - transform.position.x);
                finalVelocity += directionToPlayer * moveSpeed;
            }

            float currentX = rb != null ? rb.position.x : transform.position.x;
            float nextX = currentX + finalVelocity * Time.fixedDeltaTime;
            float horizontalDistance = player.position.x - currentX;

            if (horizontalDistance > effectiveStop)
            {
                float stopX = player.position.x - effectiveStop;
                nextX = Mathf.Min(nextX, stopX);
            }
            else if (horizontalDistance < -effectiveStop)
            {
                float stopX = player.position.x + effectiveStop;
                nextX = Mathf.Max(nextX, stopX);
            }

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
