using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Item
{
    public enum ItemType
    {
        Potion,
        Gold,
        Exp,
        // Them vao CUOI enum: gia tri so cua Potion/Gold/Exp giu nguyen nen cac
        // prefab da cau hinh tu truoc khong bi lech loai.
        Ore,
        Leather,
        Wood
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
                    playerObject.GetComponent<PlayerCoin>()?.AddCoin(amount);
                    EternalClash.Village.GoldSystem.Instance?.AddGold(amount);
                    break;

                case ItemType.Exp:
                    EternalClash.Village.PlayerStatSystem.Instance?.AddExp(amount);
                    Debug.Log("[ITEM] EXP +" + amount);
                    break;

                // Nguyen lieu che tao: AddMaterial() tu ghi save nen khong can lam gi them.
                case ItemType.Ore:
                    EternalClash.Village.GoldSystem.Instance?.AddMaterial(
                        EternalClash.Village.MaterialType.Ore, amount);
                    break;

                case ItemType.Leather:
                    EternalClash.Village.GoldSystem.Instance?.AddMaterial(
                        EternalClash.Village.MaterialType.Leather, amount);
                    break;

                case ItemType.Wood:
                    EternalClash.Village.GoldSystem.Instance?.AddMaterial(
                        EternalClash.Village.MaterialType.Wood, amount);
                    break;
            }

            Debug.Log("[ITEM] Collected " + itemType);
            Destroy(gameObject);
        }
    }
}
