using System.Collections;
using EternalClash.Item;
using EternalClash.Loot;
using EternalClash.Village;
using UnityEngine;

public class EnemyLootDropController : MonoBehaviour
{
    [SerializeField] private LootData[] lootTable;
    [SerializeField] private int expReward;

    [Header("Loot Burst")]
    [Tooltip("Do cao vong bay dau tien khi item bung ra tu than quai.")]
    [SerializeField] private float burstHeight = 1.2f;
    [Tooltip("Quang duong ngan nhat item nhay ra tu xac quai, theo huong nguoc chieu cuon map.")]
    [SerializeField] private float hopMinDistance = 0.8f;
    [Tooltip("Quang duong xa nhat item nhay ra tu xac quai (co ngau nhien de cac item khong chong len nhau).")]
    [SerializeField] private float hopMaxDistance = 1.6f;
    [Tooltip("Khoang cach toi thieu tu diem roi toi mep man hinh.")]
    [SerializeField] private float screenMargin = 0.4f;
    [Tooltip("So cu nay nho sau khi cham dat.")]
    [SerializeField] private int bounceCount = 2;
    [Tooltip("Dich mat dat len/xuong neu item nam lech so voi chan quai.")]
    [SerializeField] private float groundOffsetY = 0f;

    [Tooltip("So giay item nay (tu luc bung ra toi luc nam yen tren dat).")]
    [SerializeField] private float hopDuration = 0.75f;

    [Header("Pickup")]
    [Tooltip("So giay item dung yen sau khi nay xong, truoc khi bi hut ve Player.")]
    [SerializeField] private float magnetDelay = 1f;
    [Tooltip("So giay bay tu mat dat vao nguoi Player.")]
    [SerializeField] private float flyDuration = 0.35f;
    [Tooltip("Do cong cua duong bay ve Player.")]
    [SerializeField] private float flyArcHeight = 0.8f;
    [SerializeField] private string itemSortingLayer = "Item";

    private bool hasDropped;

    public void DropLoot()
    {
        if (hasDropped)
            return;
        hasDropped = true;

        if (lootTable != null && lootTable.Length > 0)
        {
            // Moi quai chi rot dung 1 dong xu: neu bang loot co nhieu dong Gold thi
            // cong don gia tri vao mot dong xu duy nhat, khong sinh them dong xu.
            LootData coinEntry = null;
            int coinAmount = 0;

            foreach (LootData entry in lootTable)
            {
                if (entry == null || entry.prefab == null || entry.dropRate <= 0f)
                    continue;

                if (Random.value >= entry.dropRate)
                    continue;

                int amount = RollAmount(entry);

                if (entry.itemType == ItemType.Gold)
                {
                    if (coinEntry == null)
                        coinEntry = entry;
                    coinAmount += amount;
                    continue;
                }

                SpawnDrop(entry, amount);
            }

            if (coinEntry != null)
                SpawnDrop(coinEntry, coinAmount);
        }

        GrantExpReward();
    }

    private static int RollAmount(LootData entry)
    {
        return Random.Range(entry.minAmount, Mathf.Max(entry.minAmount + 1, entry.maxAmount + 1));
    }

    private void GrantExpReward()
    {
        if (expReward <= 0)
            return;

        EternalClash.Village.PlayerStatSystem.Instance?.AddExp(expReward);
    }

    private void SpawnDrop(LootData entry, int amount)
    {
        Bounds enemyBounds = GetEnemyBounds();

        GameObject drop = Instantiate(entry.prefab, enemyBounds.center, Quaternion.identity);
        drop.transform.SetParent(null, true);

        ItemPickup pickup = drop.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.itemType = entry.itemType;
            pickup.amount = amount;
        }

        SpriteRenderer spriteRenderer = drop.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (entry.itemSprite != null)
                spriteRenderer.sprite = entry.itemSprite;

            spriteRenderer.sortingLayerName = itemSortingLayer;
        }

        // Chuyen dong do LootDropMotion dieu khien, tat vat ly de item khong roi xuyen dat
        // (collider cua item la trigger nen khong dung tren mat dat duoc).
        Rigidbody2D rigidBody = drop.GetComponent<Rigidbody2D>();
        if (rigidBody != null)
        {
            rigidBody.bodyType = RigidbodyType2D.Kinematic;
            rigidBody.velocity = Vector2.zero;
            rigidBody.gravityScale = 0f;
        }

        float itemHalfHeight = spriteRenderer != null ? spriteRenderer.bounds.extents.y : 0f;
        float groundY = enemyBounds.min.y + itemHalfHeight + groundOffsetY;

        // Diem roi: nhay tu xac quai theo huong NGUOC chieu cuon map, va khong
        // vuot ra ngoai man hinh (quai dung xa nhu Goblin Archer de bi van ra mep).
        float direction = GetAgainstMapDirection();
        float landX = enemyBounds.center.x
                      + direction * Random.Range(hopMinDistance, Mathf.Max(hopMinDistance, hopMaxDistance));
        landX = ClampToScreenX(landX, enemyBounds.center.z);
        float distance = landX - enemyBounds.center.x;

        LootDropMotion motion = drop.AddComponent<LootDropMotion>();
        motion.Play(
            groundY,
            distance,
            burstHeight * Random.Range(0.8f, 1.2f),
            bounceCount,
            // Lech nhau mot chut de cac item cham dat va bay ve lan luot, khong dinh cuc.
            hopDuration * Random.Range(0.9f, 1.1f),
            magnetDelay,
            flyDuration,
            flyArcHeight);
    }

    /// <summary>
    /// Huong nguoc chieu cuon map (+1 la ben phai). Map cuon sang trai thi item
    /// nhay sang phai. Khong co WorldScroller thi nhay ve phia quai dang dung.
    /// </summary>
    private float GetAgainstMapDirection()
    {
        var scroller = FindObjectOfType<EternalClash.World.WorldScroller>();
        if (scroller != null && Mathf.Abs(scroller.WorldVelocityX) > 0.0001f)
            return -Mathf.Sign(scroller.WorldVelocityX);

        Transform player = FindPlayer();
        if (player == null)
            return 1f;

        float dx = transform.position.x - player.position.x;
        return Mathf.Abs(dx) < 0.01f ? 1f : Mathf.Sign(dx);
    }

    private float ClampToScreenX(float x, float worldZ)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return x;

        float depth = worldZ - cam.transform.position.z;
        float left = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, depth)).x + screenMargin;
        float right = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, depth)).x - screenMargin;
        return left < right ? Mathf.Clamp(x, left, right) : x;
    }

    /// <summary>
    /// Khung hinh anh cua quai: tam dung lam diem bung ra, mep duoi lam mat dat.
    /// Bo qua particle vi bounds cua particle khong phan anh vi tri than quai.
    /// </summary>
    private Bounds GetEnemyBounds()
    {
        bool found = false;
        Bounds bounds = new Bounds(transform.position, Vector3.zero);

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer || !r.enabled)
                continue;

            if (found)
                bounds.Encapsulate(r.bounds);
            else
                bounds = r.bounds;
            found = true;
        }

        // Giu z cua quai de item khong bi lech lop ve.
        bounds.center = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);
        return bounds;
    }

    private Transform FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        return playerObject != null ? playerObject.transform : null;
    }
}
