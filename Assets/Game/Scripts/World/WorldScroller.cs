using System.Collections;
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
        [SerializeField] private Transform skyLayer;

        [Header("World flow")]
        [SerializeField] private float scrollDirection = -1f;
        [SerializeField] private WorldLoopController loopController;
        [SerializeField] private WorldLoopSpawner chunkSpawner;

        [Header("Knockback recoil")]
        [SerializeField] private float recoilDuration = 0.12f;

        private bool scrolling = true;
        private float currentMultiplier = 1f;
        private Coroutine recoilRoutine;

        private void Awake()
        {
            if (loopController == null)
                loopController = FindObjectOfType<WorldLoopController>();
            if (chunkSpawner == null)
                chunkSpawner = FindObjectOfType<WorldLoopSpawner>();
            if (skyLayer == null)
                skyLayer = FindChildByName(transform, "Sky");
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
            if (loopController != null || chunkSpawner != null || !scrolling)
                return;

            MoveLayer(cloudLayer, CloudSpeed);
            MoveLayer(mountainLayer, MountainSpeed);
            MoveLayer(groundLayer, GroundSpeed);
            MoveLayer(treeLayer, TreeSpeed);
        }

        private void MoveLayer(Transform layer, float speed)
        {
            if (layer == null) return;
            layer.position = new Vector3(
                layer.position.x + scrollDirection * speed * Time.deltaTime,
                layer.position.y,
                layer.position.z);
        }

        public float GetGroundVelocity() => groundSpeed * currentMultiplier;

        public Vector3 GetWorldVelocity() => new Vector3(scrollDirection * GetGroundVelocity(), 0f, 0f);

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

        public void SetWorldSpeed(float multiplier) => SetSpeedMultiplier(multiplier);

        public void ReverseDirection()
        {
            if (loopController != null)
            {
                loopController.ReverseDirection();
                return;
            }
            scrollDirection *= -1f;
        }

        public void StopScroll() => scrolling = false;
        public void ResumeScroll() => scrolling = true;

        public void ApplyKnockbackShift(Vector3 delta)
        {
            delta.y = 0f;
            if (Mathf.Abs(delta.x) < 0.0001f) return;

            if (recoilRoutine != null)
                StopCoroutine(recoilRoutine);
            recoilRoutine = StartCoroutine(SmoothWorldRecoil(delta));
        }

        private IEnumerator SmoothWorldRecoil(Vector3 delta)
        {
            Vector3 cloudLockedPosition = cloudLayer != null ? cloudLayer.position : Vector3.zero;
            Vector3 skyLockedPosition = skyLayer != null ? skyLayer.position : Vector3.zero;
            Transform target = transform;
            Vector3 start = target.position;
            Vector3 end = start + new Vector3(delta.x, 0f, 0f);
            float duration = Mathf.Max(0.01f, recoilDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                Vector3 p = Vector3.Lerp(start, end, eased);
                target.position = new Vector3(p.x, start.y, start.z);

                if (cloudLayer != null)
                    cloudLayer.position = cloudLockedPosition;
                if (skyLayer != null)
                    skyLayer.position = skyLockedPosition;

                yield return null;
            }

            target.position = new Vector3(end.x, start.y, start.z);

            if (cloudLayer != null)
                cloudLayer.position = cloudLockedPosition;
            if (skyLayer != null)
                skyLayer.position = skyLockedPosition;

            recoilRoutine = null;
        }

        private static Transform FindChildByName(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                    return child;

                Transform match = FindChildByName(child, childName);
                if (match != null)
                    return match;
            }

            return null;
        }
    }
}
