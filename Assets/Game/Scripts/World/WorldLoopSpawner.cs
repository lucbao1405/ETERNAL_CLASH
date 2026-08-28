using System.Collections.Generic;
using UnityEngine;

namespace EternalClash.World
{
    public class WorldLoopSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject worldTemplate;
        [SerializeField] private int chunkCount = 3;
        [SerializeField] private Camera targetCamera;

        private readonly List<Transform> chunks = new List<Transform>();

        private void Start()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (worldTemplate == null)
                return;

            chunks.Add(worldTemplate.transform);
            worldTemplate.name = "World_01";

            for (int i = 1; i < chunkCount; i++)
            {
                GameObject clone = Instantiate(worldTemplate, worldTemplate.transform.parent);
                clone.name = "World_0" + (i + 1);

                Transform previous = chunks[chunks.Count - 1];
                PlaceRightAfter(clone.transform, previous);

                chunks.Add(clone.transform);
            }
        }

        private void Update()
        {
            if (targetCamera == null || chunks.Count == 0)
                return;

            float cameraLeftEdge = targetCamera.transform.position.x - targetCamera.orthographicSize * targetCamera.aspect;

            Transform front = chunks[0];
            Bounds frontBounds = GetCombinedBounds(front.gameObject);

            if (frontBounds.max.x < cameraLeftEdge)
            {
                Transform lastChunk = chunks[chunks.Count - 1];
                PlaceRightAfter(front, lastChunk);

                chunks.RemoveAt(0);
                chunks.Add(front);
            }
        }

        // Dat mep trai cua "mover" dung bang mep phai cua "anchor", dua tren Bounds thuc te
        private void PlaceRightAfter(Transform mover, Transform anchor)
        {
            Bounds anchorBounds = GetCombinedBounds(anchor.gameObject);
            Bounds moverBounds = GetCombinedBounds(mover.gameObject);

            float leftOffsetFromPivot = moverBounds.min.x - mover.position.x;

            Vector3 pos = mover.position;
            pos.x = anchorBounds.max.x - leftOffsetFromPivot;
            mover.position = pos;
        }

        // Gop Bounds cua toan bo Renderer va Collider2D con (active) trong World, khong chi 1 object con
        private Bounds GetCombinedBounds(GameObject go)
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
