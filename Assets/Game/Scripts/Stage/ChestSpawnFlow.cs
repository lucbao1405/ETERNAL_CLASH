using UnityEngine;
using UnityEngine.EventSystems;
using EternalClash.Chest;
using EternalClash.Stage;

/// <summary>
/// Spawns the reward chest after a stage clear and coordinates the
/// chest interaction flow.
///
/// Flow: chest spawns -> StageCompleteController enables the ChestRewardPanel
/// ("Open Chest" prompt) -> player presses OPEN -> chest opening effect
/// -> reward panel (Continue).
///
/// The UI panels live in the Battle scene (disabled by default) and are managed
/// via SetActive by StageCompleteController - nothing is created at runtime here.
/// </summary>
public class ChestSpawnFlow : MonoBehaviour
{
    public static ChestSpawnFlow Instance { get; private set; }

    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private Transform spawnPoint;

    private ChestController lastSpawnedChest;

    public ChestController LastSpawnedChest => lastSpawnedChest;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (lastSpawnedChest != null)
            lastSpawnedChest.OnClicked -= OnChestClicked;
    }

    public ChestController SpawnChest()
    {
        OnStageClear();
        return lastSpawnedChest;
    }

    public void OnStageClear()
    {
        if (chestPrefab == null || spawnPoint == null)
            return;

        EnsureRuntimeInfrastructure();
        CleanupPreviousChest();

        GameObject chestObj = Instantiate(chestPrefab, spawnPoint.position, Quaternion.identity);
        lastSpawnedChest = chestObj.GetComponent<ChestController>();

        if (lastSpawnedChest != null)
            lastSpawnedChest.OnClicked += OnChestClicked;
    }

    private void CleanupPreviousChest()
    {
        if (lastSpawnedChest == null)
            return;

        lastSpawnedChest.OnClicked -= OnChestClicked;

        if (lastSpawnedChest.gameObject != null)
            Destroy(lastSpawnedChest.gameObject);

        lastSpawnedChest = null;
    }

    private void OnChestClicked()
    {
        ShowChestOpenUI();
    }

    /// <summary>
    /// Shows the "Open Chest" prompt panel. The panel is a scene object owned by
    /// StageCompleteController; this only ensures it is enabled.
    /// </summary>
    public void ShowChestOpenUI()
    {
        if (StageCompleteController.Instance != null)
            StageCompleteController.Instance.ShowChestRewardUI();
    }

    private void EnsureRuntimeInfrastructure()
    {
        ChestOpenEffectController.EnsureInstance();

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }
}
