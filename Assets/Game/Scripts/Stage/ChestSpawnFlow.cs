using UnityEngine;
using UnityEngine.EventSystems;
using EternalClash.Chest;
using EternalClash.Stage;

/// <summary>
    /// Coordinates the reward popup after a stage clear.
///
    /// World chest spawning is intentionally disabled. BattleResultFlowController
    /// owns the Canvas popup and its tap-driven reward sequence.
///
/// The UI panels live in the Battle scene (disabled by default) and are managed
/// via SetActive by StageCompleteController - nothing is created at runtime here.
/// </summary>
public class ChestSpawnFlow : MonoBehaviour
{
    public static ChestSpawnFlow Instance { get; private set; }

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

    public ChestController SpawnChest() => null;

    public void OnStageClear()
    {
        CleanupPreviousChest();
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
