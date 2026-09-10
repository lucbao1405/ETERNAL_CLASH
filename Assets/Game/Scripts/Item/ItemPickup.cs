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
        /// <summary>
        /// Duoc goi moi khi Player nhat mot vat pham (truoc khi huy prefab).
        /// BattleResultFlowController lang nghe su kien nay de ghi nhan battle loot
        /// nhat duoc trong tran. Tham so la pickup va so luong.
        /// </summary>
        public static event System.Action<ItemPickup, int> OnItemCollected;

        public ItemType itemType;
        public int amount = 10;
        public float magnetRange = 2f;
        public float moveSpeed = 8f;
        public float magnetDelay = 0f;

        /// <summary>
        /// Bat boi LootDropMotion (do roi tu quai): chuyen dong va thoi diem nhat do
        /// LootDropMotion quyet dinh, nen tat nam cham va va cham trigger o day.
        /// </summary>
        [HideInInspector] public bool externallyDriven;

        private Transform player;
        private bool collected;
        private float magnetAvailableAt = -1f;

        private void Awake()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;

            if (magnetDelay <= 0f)
                magnetAvailableAt = Time.time;
        }

        public void SetMagnetDelay(float delay)
        {
            magnetDelay = Mathf.Max(0f, delay);
            magnetAvailableAt = Time.time + magnetDelay;
        }

        private void Update()
        {
            if (externallyDriven)
                return;

            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                player = playerObject != null ? playerObject.transform : null;
            }

            if (player == null || collected)
                return;

            if (magnetAvailableAt > Time.time)
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
            if (externallyDriven || collected || !other.CompareTag("Player"))
                return;

            collected = true;

            Collect(other.gameObject);
        }

        /// <summary>Nhat ngay, dung khi item da bay toi nguoi Player.</summary>
        public void CollectBy(GameObject playerObject)
        {
            if (collected || playerObject == null)
                return;

            collected = true;
            Collect(playerObject);
        }

        private void Collect(GameObject playerObject)
        {
            switch (itemType)
            {
                case ItemType.Potion:
                    playerObject.GetComponent<HealthSystem>()?.Heal(amount);
                    break;

                case ItemType.Gold:
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
            OnItemCollected?.Invoke(this, amount);
            Destroy(gameObject);
        }
    }
}
