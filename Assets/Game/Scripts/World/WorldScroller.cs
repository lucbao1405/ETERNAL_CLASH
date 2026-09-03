using UnityEngine;

namespace EternalClash.World
{
    public class WorldScroller : MonoBehaviour
    {
        [Header("Layer Speeds")]
        [SerializeField] private float cloudSpeed = 0.1f;
        [SerializeField] private float mountainSpeed = 0.5f;
        [SerializeField] private float groundSpeed = 2.5f;
        [SerializeField] private float treeSpeed = 1.5f;

        [Header("Layer References")]
        [SerializeField] private Transform cloudLayer;
        [SerializeField] private Transform mountainLayer;
        [SerializeField] private Transform groundLayer;
        [SerializeField] private Transform treeLayer;

        [Header("World flow")]
        [SerializeField] private float scrollDirection = -1f;
        [SerializeField] private WorldLoopController loopController;
        [SerializeField] private WorldLoopSpawner chunkSpawner;

        private bool scrolling = true;
        private float currentMultiplier = 1f;

        private void Awake()
        {
            if (loopController == null)
                loopController = FindObjectOfType<WorldLoopController>();

            if (chunkSpawner == null)
                chunkSpawner = FindObjectOfType<WorldLoopSpawner>();
        }

        public bool IsScrolling => scrolling;

        public float WorldVelocityX => scrollDirection * GetGroundVelocity();
        public float CloudSpeed => cloudSpeed * currentMultiplier;
        public float MountainSpeed => mountainSpeed * currentMultiplier;
        public float GroundSpeed => groundSpeed * currentMultiplier;
        public float TreeSpeed => treeSpeed * currentMultiplier;

        public float CloudVelocityX => scrollDirection * CloudSpeed;
        public float MountainVelocityX => scrollDirection * MountainSpeed;
        public float GroundVelocityX => scrollDirection * GroundSpeed;
        public float TreeVelocityX => scrollDirection * TreeSpeed;

        private void Update()
        {
            if (loopController != null)
                return;

            // WorldLoopSpawner owns per-chunk movement for every layer when present,
            // so it reads the velocities above instead of this script moving a single Transform.
            if (chunkSpawner != null)
                return;

            if (!scrolling)
                return;

            MoveLayer(cloudLayer, CloudSpeed);
            MoveLayer(mountainLayer, MountainSpeed);
            MoveLayer(groundLayer, GroundSpeed);
            MoveLayer(treeLayer, TreeSpeed);
        }

        private void MoveLayer(Transform layer, float speed)
        {
            if (layer == null)
                return;

            Vector3 axisLocked = new Vector3(
                layer.position.x + scrollDirection * speed * Time.deltaTime,
                layer.position.y,
                layer.position.z
            );

            layer.position = axisLocked;
        }

        public float GetGroundVelocity()
        {
            return groundSpeed * currentMultiplier;
        }

        public Vector3 GetWorldVelocity()
        {
            return new Vector3(scrollDirection * GetGroundVelocity(), 0f, 0f);
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            currentMultiplier = Mathf.Max(1f, multiplier);
            if (loopController != null)
                loopController.SetWorldSpeed(currentMultiplier);
            Debug.Log("[WORLD] Speed multiplier x" + currentMultiplier);
        }

        public void ResetSpeed()
        {
            currentMultiplier = 1f;
            if (loopController != null)
                loopController.ResetSpeed();
            Debug.Log("[WORLD] Speed reset");
        }

        public void SetWorldSpeed(float multiplier)
        {
            SetSpeedMultiplier(multiplier);
        }

        public void ReverseDirection()
        {
            if (loopController != null)
            {
                loopController.ReverseDirection();
                return;
            }

            scrollDirection *= -1f;
        }

        public void StopScroll()
        {
            scrolling = false;
        }

        public void ResumeScroll()
        {
            scrolling = true;
        }

        /// <summary>
        /// Applies knockback recoil to mountain, tree, and ground layers only.
        /// Cloud layer is NOT affected by knockback.
        /// </summary>
        public void ApplyKnockbackShift(Vector3 delta)
        {
            if (loopController != null)
            {
                loopController.ApplyKnockbackShift(delta);
                return;
            }

            if (chunkSpawner != null)
            {
                chunkSpawner.ApplyKnockbackShift(delta);
                return;
            }

            if (mountainLayer != null)
                mountainLayer.position += delta;
            if (treeLayer != null)
                treeLayer.position += delta;
            if (groundLayer != null)
                groundLayer.position += delta;
        }
    }
}