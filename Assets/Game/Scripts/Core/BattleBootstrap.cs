using UnityEngine;
using EternalClash.Stage;

/// <summary>
/// Runtime wiring helper for Battle scene.
/// Automatically finds required systems, no Inspector setup required.
/// </summary>
public class BattleBootstrap : MonoBehaviour
{
    private StageProgressController stageProgress;
    private EnemyManager enemyManager;
    private StageCompleteController stageCompleteController;

    private bool initialized;

    private void Awake()
    {
        ResolveReferences();
        SubscribeEvents();
    }

    private void Start()
    {
        if (stageProgress != null)
        {
            stageProgress.StartStage();
        }
        else
        {
            Debug.LogWarning("[BattleBootstrap] StageProgressController not found");
        }
    }

    private void ResolveReferences()
    {
        enemyManager = FindObjectOfType<EnemyManager>();
        stageProgress = FindObjectOfType<StageProgressController>();
        stageCompleteController = FindObjectOfType<StageCompleteController>();

        if (enemyManager == null)
        {
            GameObject managerObject = new GameObject("EnemyManager");
            enemyManager = managerObject.AddComponent<EnemyManager>();
        }

        if (stageProgress == null)
        {
            Debug.LogWarning("[BattleBootstrap] Missing StageProgressController. Please add StageProgressController to Battle scene.");
        }

        if (stageCompleteController == null)
        {
            Debug.LogWarning("[BattleBootstrap] Missing StageCompleteController. Please add StageCompleteController to Battle scene.");
        }
    }

    private void SubscribeEvents()
    {
        if (initialized)
            return;

        if (enemyManager != null && stageProgress != null)
        {
            enemyManager.OnAllEnemiesCleared += stageProgress.OnEncounterCleared;
            initialized = true;
        }

        if (stageProgress != null && stageCompleteController != null)
        {
            stageProgress.OnStageCompleted += stageCompleteController.BeginPostStageFlow;
        }
    }

    private void OnDestroy()
    {
        if (enemyManager != null && stageProgress != null)
        {
            enemyManager.OnAllEnemiesCleared -= stageProgress.OnEncounterCleared;
        }

        if (stageProgress != null && stageCompleteController != null)
        {
            stageProgress.OnStageCompleted -= stageCompleteController.BeginPostStageFlow;
        }
    }
}
