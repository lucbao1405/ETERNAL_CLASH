using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Item
{
    public enum ItemType
    {
        Potion,
        Gold,
        Exp
    }

    public class ItemPickup : MonoBehaviour
    {
        public ItemType itemType;
        public int amount = 10;
        public float magnetRange = 2f;
        public float moveSpeed = 8f;

        private Transform player;
        private bool collected;

        private void Update()
        {
            if (player == null)
            {
                GameObject obj = GameObject.FindGameObjectWithTag("Player");
                if (obj != null)
                    player = obj.transform;
            }

            if (player == null || collected)
                return;

            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= magnetRange)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    player.position,
                    moveSpeed * Time.deltaTime
                );
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || !other.CompareTag("Player"))
                return;

            collected = true;

            Collect(other.gameObject);
        }

        private void Collect(GameObject playerObject)
        {
            switch (itemType)
            {
                case ItemType.Potion:
                    playerObject.GetComponent<HealthSystem>()?.Heal(amount);
                    break;

                case ItemType.Gold:
                    Debug.Log("[ITEM] Gold +" + amount);
                    break;

                case ItemType.Exp:
                    Debug.Log("[ITEM] EXP +" + amount);
                    break;
            }

            Debug.Log("[ITEM] Collected " + itemType);
            Destroy(gameObject);
        }
    }
}
