using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.World
{
    public class WorldLoopSpawner : MonoBehaviour
    {
        [Header("Layer Templates")]
        [SerializeField] private GameObject skyTemplate;
        [SerializeField] private GameObject cloudTemplate;
        [SerializeField] private GameObject mountainTemplate;
        [SerializeField] private GameObject treeTemplate;
        [SerializeField] private GameObject groundTemplate;

        [Header("Settings")]
        [Min(2)]
        [SerializeField] private int chunkCount = 3;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private WorldScroller worldScroller;

        private LayerChunkManager skyManager;
        private LayerChunkManager cloudManager;
        private LayerChunkManager mountainManager;
        private LayerChunkManager treeManager;
        private LayerChunkManager groundManager;

        private void Start()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (worldScroller == null)
                worldScroller = GetComponent<WorldScroller>() ?? FindObjectOfType<WorldScroller>();

            ResolveTemplates();
            Canvas.ForceUpdateCanvases();

            // Create managers in UI draw order. Every clone stays beside its source
            // Image in the same parent, so the layer order cannot get mixed together.
            skyManager = CreateManager(skyTemplate, "Sky");
            cloudManager = CreateManager(cloudTemplate, "Cloud");
            mountainManager = CreateManager(mountainTemplate, "Mountain");
            treeManager = CreateManager(treeTemplate, "Tree");
            groundManager = CreateManager(groundTemplate, "Ground");
        }

        private void Update()
        {
            if (targetCamera == null || worldScroller == null)
                return;

            float halfWidth = targetCamera.orthographicSize * targetCamera.aspect;
            float cameraLeftEdge = targetCamera.transform.position.x - halfWidth;
            float cameraRightEdge = targetCamera.transform.position.x + halfWidth;
            float dt = worldScroller.IsScrolling ? Time.deltaTime : 0f;

            skyManager?.UpdateLoop(cameraLeftEdge, cameraRightEdge, worldScroller.SkyVelocityX * dt);
            cloudManager?.UpdateLoop(cameraLeftEdge, cameraRightEdge, worldScroller.CloudVelocityX * dt);
            mountainManager?.UpdateLoop(cameraLeftEdge, cameraRightEdge, worldScroller.MountainVelocityX * dt);
            treeManager?.UpdateLoop(cameraLeftEdge, cameraRightEdge, worldScroller.TreeVelocityX * dt);
            groundManager?.UpdateLoop(cameraLeftEdge, cameraRightEdge, worldScroller.GroundVelocityX * dt);
        }

        /// <summary>
        /// Moves every clone in the foreground layers by the same amount.
        /// Sky and cloud intentionally remain stable during knockback recoil.
        /// </summary>
        public void ApplyKnockbackShift(Vector3 delta)
        {
            treeManager?.Shift(delta.x);
            mountainManager?.Shift(delta.x);
            groundManager?.Shift(delta.x);
        }

        private LayerChunkManager CreateManager(GameObject source, string layerName)
        {
            if (source == null)
                return null;

            Transform layerParent = source.transform.parent != null
                ? source.transform.parent
                : transform;

            return new LayerChunkManager(
                source,
                chunkCount,
                layerName,
                layerParent,
                targetCamera);
        }

        private void ResolveTemplates()
        {
            skyTemplate = ResolveTemplate(skyTemplate, "Sky");
            cloudTemplate = ResolveTemplate(cloudTemplate, "Cloud");
            mountainTemplate = ResolveTemplate(mountainTemplate, "Mountain", "Moutain");
            treeTemplate = ResolveTemplate(treeTemplate, "Tree");
            groundTemplate = ResolveTemplate(groundTemplate, "Ground");
        }

        private GameObject ResolveTemplate(GameObject current, params string[] possibleNames)
        {
            if (current != null)
                return current;

            foreach (string possibleName in possibleNames)
            {
                Transform match = FindChildByName(transform, possibleName);
                if (match != null)
                    return match.gameObject;
            }

            Debug.LogWarning(
                $"[WorldLoopSpawner] Could not find layer: {string.Join(" / ", possibleNames)}",
                this);
            return null;
        }

        private static Transform FindChildByName(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                    return child;

                Transform match = FindChildByName(child, childName);
                if (match != null)
                    return match;
            }

            return null;
        }
    }

    /// <summary>
    /// Manages the seamless chunks of one parallax layer. It supports both the new
    /// UI Image/RectTransform setup and the former SpriteRenderer setup.
    /// </summary>
    internal sealed class LayerChunkManager
    {
        private const float EdgeTolerance = 0.0001f;
        private const int MaximumAutomaticChunkCount = 32;

        private readonly List<Transform> chunks = new List<Transform>();
        private readonly string layerName;
        private readonly GameObject template;
        private readonly Transform parent;
        private int chunkCounter;

        public LayerChunkManager(
            GameObject template,
            int minimumChunkCount,
            string name,
            Transform parent,
            Camera targetCamera)
        {
            layerName = name;
            this.template = template;
            this.parent = parent;

            if (template == null)
                return;

            Canvas.ForceUpdateCanvases();
            DisableBackgroundRaycasts(template);

            if (!TryGetHorizontalEdges(template.transform, out float firstMin, out float firstMax))
            {
                Debug.LogError(
                    $"[WorldLoopSpawner] {name} has no valid RectTransform, Renderer or Collider2D width. " +
                    "No clones were created, preventing them from overlapping.",
                    template);
                return;
            }

            float tileWidth = firstMax - firstMin;
            int requestedCount = Mathf.Max(2, minimumChunkCount);
            int requiredCount = CalculateRequiredChunkCount(tileWidth, targetCamera);
            int totalCount = Mathf.Clamp(
                Mathf.Max(requestedCount, requiredCount),
                2,
                MaximumAutomaticChunkCount);

            chunks.Add(template.transform);
            template.name = NextChunkName();

            for (int i = 1; i < totalCount; i++)
            {
                GameObject clone = UnityEngine.Object.Instantiate(template, parent, false);
                clone.name = NextChunkName();
                DisableBackgroundRaycasts(clone);

                Transform previous = chunks[chunks.Count - 1];
                clone.transform.SetSiblingIndex(previous.GetSiblingIndex() + 1);

                if (!PlaceRightAfter(clone.transform, previous))
                {
                    UnityEngine.Object.Destroy(clone);
                    Debug.LogError(
                        $"[WorldLoopSpawner] Stopped cloning {name}: unable to measure its UI width.",
                        template);
                    break;
                }

                chunks.Add(clone.transform);
            }

            RepairSpacing();
            ValidateSpacing();
            Debug.Log(
                $"[WorldLoopSpawner] {name}: {chunks.Count} chunks, UI width {tileWidth:0.###}.",
                template);
        }

        public void UpdateLoop(float cameraLeftEdge, float cameraRightEdge, float deltaX)
        {
            if (chunks.Count == 0 || template == null)
                return;

            if (Mathf.Abs(deltaX) > EdgeTolerance)
                Shift(deltaX);

            // A while loop also handles a large frame step or a strong knockback.
            // The safety count prevents an invalid layout from looping forever.
            int safety = chunks.Count;
            if (deltaX <= 0f)
            {
                while (safety-- > 0 &&
                       TryGetHorizontalEdges(chunks[0], out _, out float frontMax) &&
                       frontMax < cameraLeftEdge)
                {
                    Transform front = chunks[0];
                    chunks.RemoveAt(0);
                    Transform last = chunks[chunks.Count - 1];

                    if (!PlaceRightAfter(front, last))
                    {
                        chunks.Insert(0, front);
                        break;
                    }

                    front.SetSiblingIndex(last.GetSiblingIndex() + 1);
                    chunks.Add(front);
                }
            }
            else
            {
                while (safety-- > 0 &&
                       TryGetHorizontalEdges(chunks[chunks.Count - 1], out float backMin, out _) &&
                       backMin > cameraRightEdge)
                {
                    int lastIndex = chunks.Count - 1;
                    Transform back = chunks[lastIndex];
                    chunks.RemoveAt(lastIndex);
                    Transform first = chunks[0];

                    if (!PlaceLeftBefore(back, first))
                    {
                        chunks.Add(back);
                        break;
                    }

                    back.SetSiblingIndex(first.GetSiblingIndex());
                    chunks.Insert(0, back);
                }
            }
        }

        public void Shift(float deltaX)
        {
            foreach (Transform chunk in chunks)
            {
                Vector3 position = chunk.position;
                position.x += deltaX;
                chunk.position = position;
            }
        }

        private string NextChunkName()
        {
            chunkCounter++;
            return $"{layerName}_Chunk_{chunkCounter:D2}";
        }

        private static int CalculateRequiredChunkCount(float tileWidth, Camera targetCamera)
        {
            if (targetCamera == null || tileWidth <= EdgeTolerance)
                return 2;

            float cameraWidth = targetCamera.orthographicSize * targetCamera.aspect * 2f;
            if (cameraWidth <= EdgeTolerance || float.IsNaN(cameraWidth) || float.IsInfinity(cameraWidth))
                return 2;

            // Two extra chunks keep both edges covered while a chunk is recycled.
            return Mathf.CeilToInt(cameraWidth / tileWidth) + 2;
        }

        private void RepairSpacing()
        {
            for (int i = 1; i < chunks.Count; i++)
                PlaceRightAfter(chunks[i], chunks[i - 1]);
        }

        private void ValidateSpacing()
        {
            for (int i = 1; i < chunks.Count; i++)
            {
                if (!TryGetHorizontalEdges(chunks[i - 1], out _, out float previousMax) ||
                    !TryGetHorizontalEdges(chunks[i], out float currentMin, out _))
                    continue;

                float seam = currentMin - previousMax;
                if (Mathf.Abs(seam) > 0.001f)
                {
                    Debug.LogWarning(
                        $"[WorldLoopSpawner] {layerName} seam was {seam:0.#####}; repairing it.",
                        chunks[i]);
                    PlaceRightAfter(chunks[i], chunks[i - 1]);
                }
            }
        }

        private static bool PlaceRightAfter(Transform mover, Transform anchor)
        {
            if (!TryGetHorizontalEdges(anchor, out _, out float anchorMax) ||
                !TryGetHorizontalEdges(mover, out float moverMin, out _))
                return false;

            Vector3 position = mover.position;
            position.x += anchorMax - moverMin;
            position.y = anchor.position.y;
            position.z = anchor.position.z;
            mover.position = position;

            // A second correction removes floating-point/layout rounding at the seam.
            Canvas.ForceUpdateCanvases();
            if (TryGetHorizontalEdges(anchor, out _, out anchorMax) &&
                TryGetHorizontalEdges(mover, out moverMin, out _))
            {
                position = mover.position;
                position.x += anchorMax - moverMin;
                mover.position = position;
            }

            return true;
        }

        private static bool PlaceLeftBefore(Transform mover, Transform anchor)
        {
            if (!TryGetHorizontalEdges(anchor, out float anchorMin, out _) ||
                !TryGetHorizontalEdges(mover, out _, out float moverMax))
                return false;

            Vector3 position = mover.position;
            position.x += anchorMin - moverMax;
            position.y = anchor.position.y;
            position.z = anchor.position.z;
            mover.position = position;

            Canvas.ForceUpdateCanvases();
            if (TryGetHorizontalEdges(anchor, out anchorMin, out _) &&
                TryGetHorizontalEdges(mover, out _, out moverMax))
            {
                position = mover.position;
                position.x += anchorMin - moverMax;
                mover.position = position;
            }

            return true;
        }

        private static bool TryGetHorizontalEdges(Transform root, out float minX, out float maxX)
        {
            minX = float.PositiveInfinity;
            maxX = float.NegativeInfinity;
            bool hasWidth = false;

            // New setup: Panel/Image objects use RectTransform + CanvasRenderer.
            RectTransform[] rectTransforms = root.GetComponentsInChildren<RectTransform>(true);
            Vector3[] corners = new Vector3[4];
            foreach (RectTransform rectTransform in rectTransforms)
            {
                rectTransform.GetWorldCorners(corners);
                for (int i = 0; i < corners.Length; i++)
                {
                    float x = corners[i].x;
                    if (!IsFinite(x))
                        continue;

                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    hasWidth = true;
                }
            }

            // Backward compatibility for scenes that still use SpriteRenderer.
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
                Encapsulate(renderer.bounds.min.x, renderer.bounds.max.x, ref minX, ref maxX, ref hasWidth);

            Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D collider in colliders)
                Encapsulate(collider.bounds.min.x, collider.bounds.max.x, ref minX, ref maxX, ref hasWidth);

            return hasWidth && IsFinite(minX) && IsFinite(maxX) && maxX - minX > EdgeTolerance;
        }

        private static void Encapsulate(
            float candidateMin,
            float candidateMax,
            ref float minX,
            ref float maxX,
            ref bool hasWidth)
        {
            if (!IsFinite(candidateMin) || !IsFinite(candidateMax))
                return;

            minX = Mathf.Min(minX, candidateMin);
            maxX = Mathf.Max(maxX, candidateMax);
            hasWidth = true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void DisableBackgroundRaycasts(GameObject root)
        {
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
                graphic.raycastTarget = false;
        }
    }
}
