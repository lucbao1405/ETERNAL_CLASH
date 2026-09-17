using UnityEngine;

namespace EternalClash.Item
{
    public class ItemSpawnEffect : MonoBehaviour
    {
        public float jumpForce = 3f;
        public float destroyTime = 20f;

        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            if (rb != null)
            {
                rb.AddForce(new Vector2(Random.Range(-1f, 1f), 1f) * jumpForce, ForceMode2D.Impulse);
            }

            Destroy(gameObject, destroyTime);
        }
    }
}
