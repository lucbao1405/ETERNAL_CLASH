using UnityEngine;
using EternalClash.Stage;
using EternalClash.Core.Save;

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
        ApplySavedHp();

        if (stageProgress != null)
        {
            stageProgress.StartStage();
        }
        else
        {
            Debug.LogWarning("[BattleBootstrap] StageProgressController not found");
        }
    }

    private void ApplySavedHp()
    {
        var condition = EternalClash.Core.PlayerConditionSystem.Instance;
        var data = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
        if (condition == null || data == null) return;
        if (data.maxHp <= 0) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        var health = player != null ? player.GetComponent<EternalClash.Character.HealthSystem>() : null;
        if (health == null) return;

        int current = Mathf.Clamp(data.currentHp, 0, data.maxHp);
        int delta = current - health.CurrentHealth;
        if (delta > 0) health.Heal(delta);
        else if (delta < 0)
        {
            int newHp = Mathf.Max(0, current);
            while (health.CurrentHealth > newHp && health.CurrentHealth > 0)
            {
                health.TakeDamage(1);
            }
        }

        if (condition.IsInjured && condition.CanStartBattle())
        {
            // Edge case: battle entered while still injured but at 80%+
            // Recovery will complete naturally via PlayerConditionSystem.Update
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
