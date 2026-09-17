using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    private readonly HashSet<GameObject> enemies = new();

    public event Action OnAllEnemiesCleared;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterEnemy(GameObject enemy)
    {
        if (enemy != null)
        {
            enemies.Add(enemy);
            Debug.Log("[ENEMY MANAGER] Registered: " + enemy.name);
        }
    }

    public void UnregisterEnemy(GameObject enemy)
    {
        if (enemy == null) return;

        enemies.Remove(enemy);

        Debug.Log("[ENEMY MANAGER] Remaining: " + enemies.Count);

        if (enemies.Count == 0)
            OnAllEnemiesCleared?.Invoke();
    }

    public int AliveCount()
    {
        return enemies.Count;
    }
}
