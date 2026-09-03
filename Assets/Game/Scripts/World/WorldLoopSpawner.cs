using System.Collections.Generic;
using UnityEngine;

namespace EternalClash.World
{
    public class WorldLoopSpawner : MonoBehaviour
    {
        [Header("Layer Templates")]
        [SerializeField] private GameObject groundTemplate;
        [SerializeField] private GameObject treeTemplate;
        [SerializeField] private GameObject mountainTemplate;
        [SerializeField] private GameObject cloudTemplate;

        [Header("Settings")]
        [SerializeField] private int chunkCount = 3;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private WorldScroller worldScroller;

        private LayerChunkManager groundManager;
        private LayerChunkManager treeManager;
        private LayerChunkManager mountainManager;
        private LayerChunkManager cloudManager;

        private void Start()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (worldScroller == null)
                worldScroller = FindObjectOfType<WorldScroller>();

            // Initialize each layer's chunk manager
            groundManager = new LayerChunkManager(groundTemplate, chunkCount, "Ground", this.transform);
            treeManager = new LayerChunkManager(treeTemplate, chunkCount, "Tree", this.transform);
            mountainManager = new LayerChunkManager(mountainTemplate, chunkCount, "Mountain", this.transform);
            cloudManager = new LayerChunkManager(cloudTemplate, chunkCount, "Cloud", this.transform);
        }

        private void Update()
        {
            if (targetCamera == null || worldScroller == null)
                return;

            float cameraLeftEdge = targetCamera.transform.position.x - targetCamera.orthographicSize * targetCamera.aspect;
            float dt = worldScroller.IsScrolling ? Time.deltaTime : 0f;

            // Each layer moves ALL of its own chunks every frame, then checks recycling
            groundManager?.UpdateLoop(cameraLeftEdge, worldScroller.GroundVelocityX * dt);
            treeManager?.UpdateLoop(cameraLeftEdge, worldScroller.TreeVelocityX * dt);
            mountainManager?.UpdateLoop(cameraLeftEdge, worldScroller.MountainVelocityX * dt);
            cloudManager?.UpdateLoop(cameraLeftEdge, worldScroller.CloudVelocityX * dt);
        }

        /// <summary>
        /// Forwards a knockback shift to every chunk of the tree/mountain/ground layers.
        /// Cloud layer is intentionally excluded, matching WorldScroller's original behavior.
        /// </summary>
        public void ApplyKnockbackShift(Vector3 delta)
        {
            treeManager?.Shift(delta.x);
            mountainManager?.Shift(delta.x);
            groundManager?.Shift(delta.x);
        }

        private static Bounds GetCombinedBounds(GameObject go)
        {
            bool hasBounds = false;
            Bounds bounds = new Bounds(go.transform.position, Vector3.zero);

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            Collider2D[] colliders = go.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D c in colliders)
            {
                if (!hasBounds)
                {
                    bounds = c.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(c.bounds);
                }
            }

            return bounds;
        }
    }

    /// <summary>
    /// Helper class to manage chunk looping for a single layer.
    /// </summary>
    public class LayerChunkManager
    {
        private readonly List<Transform> chunks = new List<Transform>();
        private readonly string layerName;
        private readonly GameObject template;
        private readonly Transform parent;
        private int chunkCounter;

        public LayerChunkManager(GameObject template, int chunkCount, string name, Transform parent)
        {
            layerName = name;
            this.template = template;
            this.parent = parent;
            chunkCounter = 0;

            if (template == null)
            {
                Debug.LogWarning($"[WorldLoopSpawner] {name} template is null, skipping initialization");
                return;
            }

            // Add the original template
            chunks.Add(template.transform);
            template.name = $"{name}_Chunk_{(++chunkCounter):D2}";

            // Spawn additional chunks
            for (int i = 1; i < chunkCount; i++)
            {
                GameObject clone = Object.Instantiate(template, parent);
                clone.name = $"{name}_Chunk_{(++chunkCounter):D2}";

                Transform previous = chunks[chunks.Count - 1];
                PlaceRightAfter(clone.transform, previous);

                chunks.Add(clone.transform);
            }

            Debug.Log($"[WorldLoopSpawner] {name} layer initialized with {chunks.Count} chunks");
        }

        public void UpdateLoop(float cameraLeftEdge, float deltaX)
        {
            if (chunks.Count == 0 || template == null)
                return;

            if (deltaX != 0f)
                Shift(deltaX);

            Transform front = chunks[0];
            Bounds frontBounds = GetCombinedBounds(front.gameObject);

            // Only recycle once the chunk has FULLY exited past the camera's left edge,
            // otherwise part of it is still visible (or still under the player) when it gets moved.
            if (frontBounds.max.x < cameraLeftEdge)
            {
                // Move front chunk to the back
                Transform frontChunk = chunks[0];
                chunks.RemoveAt(0);
                
                Transform lastChunk = chunks[chunks.Count - 1];
                PlaceRightAfter(frontChunk, lastChunk);
                chunks.Add(frontChunk);
                
                Debug.Log($"[WorldLoopSpawner] {layerName} recycled chunk, order: {chunks.Count}");
            }
        }

        public void Shift(float deltaX)
        {
            foreach (Transform chunk in chunks)
            {
                Vector3 pos = chunk.position;
                pos.x += deltaX;
                chunk.position = pos;
            }
        }

        private static void PlaceRightAfter(Transform mover, Transform anchor)
        {
            Bounds anchorBounds = GetCombinedBounds(anchor.gameObject);
            float chunkWidth = anchorBounds.size.x;

            // X: nối sát ngay bên phải anchor theo đúng chiều rộng thực tế của nó.
            // Y/Z: khoá cứng theo anchor, không lấy từ mover — tránh chunk bị trôi cao độ
            // nếu nó từng được Instantiate lệch Y do khác không gian toạ độ cha.
            Vector3 pos = mover.position;
            pos.x = anchor.position.x + chunkWidth;
            pos.y = anchor.position.y;
            pos.z = anchor.position.z;
            mover.position = pos;
        }

        private static Bounds GetCombinedBounds(GameObject go)
        {
            bool hasBounds = false;
            Bounds bounds = new Bounds(go.transform.position, Vector3.zero);

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            Collider2D[] colliders = go.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D c in colliders)
            {
                if (!hasBounds)
                {
                    bounds = c.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(c.bounds);
                }
            }

            return bounds;
        }
    }
}