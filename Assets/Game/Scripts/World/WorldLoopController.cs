using System.Collections.Generic;
using UnityEngine;

namespace EternalClash.World
{
    public class WorldLoopController : MonoBehaviour
    {
        [System.Serializable]
        private class LayerSettings
        {
            public string name;
            public GameObject template;
            public float speed;
            [HideInInspector] public LayerTileLoop loop;
        }

        [Header("Layer Templates")]
        [SerializeField] private GameObject groundTemplate;
        [SerializeField] private GameObject treeTemplate;
        [SerializeField] private GameObject mountainTemplate;
        [SerializeField] private GameObject cloudTemplate;

        [Header("Loop Settings")]
        [SerializeField] private int tileCount = 3;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float cloudSpeed = 0.1f;
        [SerializeField] private float mountainSpeed = 0.5f;
        [SerializeField] private float groundSpeed = 2.5f;
        [SerializeField] private float treeSpeed = 1.5f;

        private readonly List<LayerSettings> layers = new List<LayerSettings>();
        private float speedMultiplier = 1f;
        private float direction = -1f;
        private bool scrolling = true;

        public float GroundVelocity => groundSpeed * speedMultiplier;
        public float WorldVelocityX => direction * GroundVelocity;

        protected virtual void Start()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            layers.Clear();
            AddLayer("Ground", groundTemplate, groundSpeed);
            AddLayer("Tree", treeTemplate, treeSpeed);
            AddLayer("Mountain", mountainTemplate, mountainSpeed);
            AddLayer("Cloud", cloudTemplate, cloudSpeed);
        }

        protected virtual void Update()
        {
            if (!scrolling)
                return;

            foreach (LayerSettings layer in layers)
                layer.loop?.Move(direction * layer.speed * speedMultiplier * Time.deltaTime);
        }

        public void SetWorldSpeed(float multiplier)
        {
            speedMultiplier = Mathf.Max(0f, multiplier);
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            SetWorldSpeed(multiplier);
        }

        public void ResetSpeed()
        {
            SetWorldSpeed(1f);
        }

        public void ReverseDirection()
        {
            direction *= -1f;
        }

        public void StopScroll()
        {
            scrolling = false;
        }

        public void ResumeScroll()
        {
            scrolling = true;
        }

        public void ApplyKnockbackShift(Vector3 delta)
        {
            transform.position += new Vector3(delta.x, 0f, 0f);
        }

        private void AddLayer(string name, GameObject template, float speed)
        {
            if (template == null)
                return;

            layers.Add(new LayerSettings
            {
                name = name,
                template = template,
                speed = speed,
                loop = new LayerTileLoop(template, tileCount, name, transform)
            });
        }
    }

    public sealed class LayerTileLoop
    {
        private readonly List<Transform> tiles = new List<Transform>();

        public LayerTileLoop(GameObject template, int tileCount, string name, Transform parent)
        {
            int count = Mathf.Max(2, tileCount);
            tiles.Add(template.transform);
            template.name = name + "_Tile_01";

            for (int index = 1; index < count; index++)
            {
                GameObject clone = Object.Instantiate(template, parent);
                clone.name = name + "_Tile_" + (index + 1).ToString("D2");
                PlaceAfter(clone.transform, tiles[tiles.Count - 1]);
                tiles.Add(clone.transform);
            }
        }

        public void Move(float deltaX)
        {
            foreach (Transform tile in tiles)
                tile.position = new Vector3(tile.position.x + deltaX, tile.position.y, tile.position.z);

            if (deltaX < 0f)
                RecycleLeftToRight();
            else if (deltaX > 0f)
                RecycleRightToLeft();
        }

        private void RecycleLeftToRight()
        {
            Transform first = tiles[0];
            if (GetBounds(first.gameObject).max.x > GetCameraLeftEdge())
                return;

            Transform last = tiles[tiles.Count - 1];
            PlaceAfter(first, last);
            tiles.RemoveAt(0);
            tiles.Add(first);
        }

        private void RecycleRightToLeft()
        {
            Transform last = tiles[tiles.Count - 1];
            if (GetBounds(last.gameObject).min.x < GetCameraRightEdge())
                return;

            Transform first = tiles[0];
            PlaceBefore(last, first);
            tiles.RemoveAt(tiles.Count - 1);
            tiles.Insert(0, last);
        }

        private static void PlaceAfter(Transform tile, Transform anchor)
        {
            Bounds tileBounds = GetBounds(tile.gameObject);
            Bounds anchorBounds = GetBounds(anchor.gameObject);
            tile.position = new Vector3(tile.position.x + anchorBounds.max.x - tileBounds.min.x, tile.position.y, tile.position.z);
        }

        private static void PlaceBefore(Transform tile, Transform anchor)
        {
            Bounds tileBounds = GetBounds(tile.gameObject);
            Bounds anchorBounds = GetBounds(anchor.gameObject);
            tile.position = new Vector3(tile.position.x + anchorBounds.min.x - tileBounds.max.x, tile.position.y, tile.position.z);
        }

        private static float GetCameraLeftEdge()
        {
            Camera camera = Camera.main;
            return camera == null ? float.NegativeInfinity : camera.transform.position.x - camera.orthographicSize * camera.aspect;
        }

        private static float GetCameraRightEdge()
        {
            Camera camera = Camera.main;
            return camera == null ? float.PositiveInfinity : camera.transform.position.x + camera.orthographicSize * camera.aspect;
        }

        private static Bounds GetBounds(GameObject gameObject)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(gameObject.transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }
    }
}