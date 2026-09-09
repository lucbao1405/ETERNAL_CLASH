using System.Collections;
using EternalClash.Item;
using EternalClash.Loot;
using EternalClash.Village;
using UnityEngine;

public class EnemyLootDropController : MonoBehaviour
{
    [SerializeField] private LootData[] lootTable;
    [SerializeField] private int expReward;

    [Header("Loot Physics")]
    [SerializeField] private float safeSpawnOffset = 1f;
    [SerializeField] private float launchSpeed = 3f;
    [SerializeField] private float gravityScale = 0.35f;
    [SerializeField] private PhysicsMaterial2D physicsMaterial;

    [Header("Pickup")]
    [SerializeField] private float magnetDelay = 0.4f;
    [SerializeField] private string itemSortingLayer = "Item";

    public void DropLoot()
    {
        if (lootTable != null && lootTable.Length > 0)
        {
            foreach (LootData entry in lootTable)
            {
                if (entry == null || entry.prefab == null || entry.dropRate <= 0f)
                    continue;

                if (Random.value >= entry.dropRate)
                    continue;

                SpawnDrop(entry);
            }
        }

        GrantExpReward();
    }

    private void GrantExpReward()
    {
        if (expReward <= 0)
            return;

        EternalClash.Village.PlayerStatSystem.Instance?.AddExp(expReward);
    }

    private void SpawnDrop(LootData entry)
    {
        int amount = Random.Range(entry.minAmount, Mathf.Max(entry.minAmount + 1, entry.maxAmount + 1));
        Vector3 spawnPosition = GetSafeSpawnPosition();
        Vector2 launchDirection = GetLaunchDirection(spawnPosition);

        GameObject drop = Instantiate(entry.prefab, spawnPosition, Quaternion.identity);
        drop.transform.SetParent(null, true);

        ItemPickup pickup = drop.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.itemType = entry.itemType;
            pickup.amount = amount;
            pickup.SetMagnetDelay(magnetDelay);
        }

        SpriteRenderer spriteRenderer = drop.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (entry.itemSprite != null)
                spriteRenderer.sprite = entry.itemSprite;

            spriteRenderer.sortingLayerName = itemSortingLayer;
        }

        Rigidbody2D rigidBody = drop.GetComponent<Rigidbody2D>();
        if (rigidBody != null)
        {
            rigidBody.velocity = launchDirection * launchSpeed;
            rigidBody.gravityScale = gravityScale;

            if (physicsMaterial != null)
                rigidBody.sharedMaterial = physicsMaterial;
        }
    }

    private Vector3 GetSafeSpawnPosition()
    {
        Transform player = FindPlayer();
        Vector2 awayFromPlayer = player != null
            ? transform.position - player.position
            : Random.insideUnitCircle;

        if (awayFromPlayer.sqrMagnitude < 0.0001f)
            awayFromPlayer = Vector2.up;

        awayFromPlayer.Normalize();
        return transform.position + (Vector3)awayFromPlayer * Mathf.Max(0f, safeSpawnOffset);
    }

    private Vector2 GetLaunchDirection(Vector3 spawnPosition)
    {
        Transform player = FindPlayer();
        Vector2 launchDirection = player != null && player.position != spawnPosition
            ? spawnPosition - player.position
            : Random.insideUnitCircle;

        if (launchDirection.sqrMagnitude < 0.0001f)
            launchDirection = Vector2.up;

        return launchDirection.normalized;
    }

    private Transform FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        return playerObject != null ? playerObject.transform : null;
    }
}
