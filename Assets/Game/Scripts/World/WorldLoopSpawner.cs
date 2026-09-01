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

        private LayerChunkManager groundManager;
        private LayerChunkManager treeManager;
        private LayerChunkManager mountainManager;
        private LayerChunkManager cloudManager;

        private void Start()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            // Initialize each layer's chunk manager
            groundManager = new LayerChunkManager(groundTemplate, chunkCount, "Ground", this.transform);
            treeManager = new LayerChunkManager(treeTemplate, chunkCount, "Tree", this.transform);
            mountainManager = new LayerChunkManager(mountainTemplate, chunkCount, "Mountain", this.transform);
            cloudManager = new LayerChunkManager(cloudTemplate, chunkCount, "Cloud", this.transform);
        }

        private void Update()
        {
            if (targetCamera == null)
                return;

            float cameraLeftEdge = targetCamera.transform.position.x - targetCamera.orthographicSize * targetCamera.aspect;

            // Update each layer independently
            groundManager?.UpdateLoop(cameraLeftEdge);
            treeManager?.UpdateLoop(cameraLeftEdge);
            mountainManager?.UpdateLoop(cameraLeftEdge);
            cloudManager?.UpdateLoop(cameraLeftEdge);
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

        public void UpdateLoop(float cameraLeftEdge)
        {
            if (chunks.Count == 0 || template == null)
                return;

            Transform front = chunks[0];
            Bounds frontBounds = GetCombinedBounds(front.gameObject);
            
            // Calculate 80% threshold: when 80% of the chunk has scrolled off-screen
            float chunkWidth = frontBounds.size.x;
            float threshold80Percent = frontBounds.max.x - (0.2f * chunkWidth);

            // If front chunk is 80% off-screen on the left, recycle it to the back
            if (threshold80Percent < cameraLeftEdge)
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

        private static void PlaceRightAfter(Transform mover, Transform anchor)
        {
            Bounds anchorBounds = GetCombinedBounds(anchor.gameObject);
            Bounds moverBounds = GetCombinedBounds(mover.gameObject);

            float leftOffsetFromPivot = moverBounds.min.x - mover.position.x;

            Vector3 pos = mover.position;
            pos.x = anchorBounds.max.x - leftOffsetFromPivot;
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
