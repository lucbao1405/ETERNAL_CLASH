using System.Collections;
using UnityEngine;

namespace EternalClash.World
{
    public class WorldScroller : MonoBehaviour
    {
        [Header("Layer Speeds")]
        [SerializeField] private float skySpeed = 0.02f;
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

        [Header("Knockback (Postknight environment knockback)")]
        [SerializeField] private float knockbackSpeedScale = 2f;
        [SerializeField] private float knockbackRecoveryDuration = 0.15f;

        private bool scrolling = true;
        private float currentMultiplier = 1f;
        private float knockbackMultiplier;
        private bool knockbackActive;
        private Coroutine knockbackRoutine;

        private void Awake()
        {
            if (loopController == null)
                loopController = FindObjectOfType<WorldLoopController>();
            if (chunkSpawner == null)
                chunkSpawner = GetComponent<WorldLoopSpawner>() ?? FindObjectOfType<WorldLoopSpawner>();

            ResolveLayerReferences();
        }

        public bool IsScrolling => scrolling;
        public bool IsKnockbackActive => knockbackActive;
        public float KnockbackMultiplier => knockbackMultiplier;

        private float ForegroundMultiplier => currentMultiplier + knockbackMultiplier;

        public float WorldVelocityX => scrollDirection * GetGroundVelocity();
        public float SkySpeed => skySpeed * currentMultiplier;
        public float CloudSpeed => cloudSpeed * currentMultiplier;
        public float MountainSpeed => mountainSpeed * ForegroundMultiplier;
        public float GroundSpeed => groundSpeed * ForegroundMultiplier;
        public float TreeSpeed => treeSpeed * ForegroundMultiplier;
        public float SkyVelocityX => scrollDirection * SkySpeed;
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
            MoveLayer(skyLayer, SkySpeed);
        }

        private void MoveLayer(Transform layer, float speed)
        {
            if (layer == null) return;
            layer.position = new Vector3(
                layer.position.x + scrollDirection * speed * Time.deltaTime,
                layer.position.y,
                layer.position.z);
        }

        public float GetGroundVelocity() => groundSpeed * ForegroundMultiplier;

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
            CancelKnockback();
            if (loopController != null)
            {
                loopController.ResetSpeed();
                loopController.SetKnockbackMultiplier(0f);
            }
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

        public void StopScroll()
        {
            scrolling = false;
            CancelKnockback();
        }

        public void ResumeScroll() => scrolling = true;

        public void TriggerKnockback(float force)
        {
            if (!scrolling || knockbackActive)
                return;

            if (knockbackRoutine != null)
                StopCoroutine(knockbackRoutine);

            knockbackRoutine = StartCoroutine(KnockbackRecoveryRoutine(force));
        }

        private IEnumerator KnockbackRecoveryRoutine(float force)
        {
            knockbackActive = true;
            knockbackMultiplier = -force * knockbackSpeedScale;
            PropagateKnockback();

            float start = knockbackMultiplier;
            float duration = Mathf.Max(0.01f, knockbackRecoveryDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                knockbackMultiplier = Mathf.LerpUnclamped(start, 0f, eased);
                PropagateKnockback();
                yield return null;
            }

            knockbackMultiplier = 0f;
            knockbackActive = false;
            knockbackRoutine = null;
            PropagateKnockback();
            Debug.Log("[WORLD] Knockback recovery complete");
        }

        private void PropagateKnockback()
        {
            if (loopController != null)
                loopController.SetKnockbackMultiplier(knockbackMultiplier);
        }

        public void CancelKnockback()
        {
            if (knockbackRoutine != null)
            {
                StopCoroutine(knockbackRoutine);
                knockbackRoutine = null;
            }

            knockbackMultiplier = 0f;
            knockbackActive = false;
            PropagateKnockback();
        }

        public void ApplyKnockbackShift(Vector3 delta)
        {
            delta.y = 0f;
            if (Mathf.Abs(delta.x) < 0.0001f) return;

            if (chunkSpawner != null)
            {
                chunkSpawner.ApplyKnockbackShift(delta);
                return;
            }

            if (loopController != null)
            {
                loopController.ApplyKnockbackShift(delta);
                return;
            }
        }

        private void ResolveLayerReferences()
        {
            if (skyLayer == null)
                skyLayer = FindChildByName(transform, "Sky");
            if (cloudLayer == null)
                cloudLayer = FindChildByName(transform, "Cloud");
            if (mountainLayer == null)
                mountainLayer = FindChildByName(transform, "Mountain") ??
                                FindChildByName(transform, "Moutain");
            if (treeLayer == null)
                treeLayer = FindChildByName(transform, "Tree");
            if (groundLayer == null)
                groundLayer = FindChildByName(transform, "Ground");
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
