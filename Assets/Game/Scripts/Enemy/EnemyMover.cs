using UnityEngine;

namespace EternalClash.Enemy
{
    public class EnemyMover : MonoBehaviour
    {
        [SerializeField] private float archerStopDistance = 4.5f;
        [SerializeField] private bool isArcher = false;
        [SerializeField] private float playerStopDistance = 0.75f;
        private EnemyController controller;
        private Rigidbody2D rb;
        private Transform player;
        private bool isPaused;
        private float lockedY;

        private void Awake()
        {
            controller = GetComponent<EnemyController>();
            rb = GetComponent<Rigidbody2D>();
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
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
            if (isPaused) return;
            if (EnemyFormationManager.Instance != null && EnemyFormationManager.Instance.IsLocked()) return;
            if (controller != null && !controller.canMove) return;

            var scroller = FindObjectOfType<EternalClash.World.WorldScroller>();
            float worldVelocityX = scroller != null ? scroller.GetWorldVelocity().x : 0f;
            float finalVelocity = worldVelocityX;

            float nextX = rb != null ? rb.position.x : transform.position.x;
            nextX += finalVelocity * Time.fixedDeltaTime;
            nextX = ClampBeforePlayer(nextX);

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

        private float ClampBeforePlayer(float nextX)
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                player = playerObject != null ? playerObject.transform : null;
            }

            if (player == null)
                return nextX;

            float currentX = rb != null ? rb.position.x : transform.position.x;
            float side = Mathf.Sign(currentX - player.position.x);
            if (Mathf.Abs(side) < 0.01f)
                side = Mathf.Sign(nextX - player.position.x);
            if (Mathf.Abs(side) < 0.01f)
                side = -1f;

            float stopX = player.position.x + side * playerStopDistance;
            return side > 0f ? Mathf.Max(nextX, stopX) : Mathf.Min(nextX, stopX);
        }

        public void StopMovement() => isPaused = true;
        public void ResumeMovement() => isPaused = false;
        public void PauseMovement(float duration){isPaused=true;Invoke(nameof(ResumeMovement),duration);}
        public bool IsArcher=>isArcher;
        public float ArcherStopDistance=>archerStopDistance;
    }
}
