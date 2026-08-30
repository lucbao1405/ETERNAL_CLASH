using UnityEngine;

public class ChestSpawnFlow : MonoBehaviour
{
    public static ChestSpawnFlow Instance { get; private set; }

    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private Transform spawnPoint;

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnChest()
    {
        OnStageClear();
    }

    public void OnStageClear()
    {
        if (chestPrefab == null || spawnPoint == null)
            return;

        Instantiate(chestPrefab, spawnPoint.position, Quaternion.identity);
    }
}
