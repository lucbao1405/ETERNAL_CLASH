using UnityEngine;

/// <summary>
/// Automatic runtime setup for Battle scene.
/// Reduces manual Inspector wiring.
/// </summary>
public class BattleAutoSetup : MonoBehaviour
{
    private void Awake()
    {
        SetupEnemyManager();
        SetupBootstrap();
    }

    private void SetupEnemyManager()
    {
        EnemyManager manager = FindObjectOfType<EnemyManager>();

        if (manager != null)
            return;

        GameObject obj = new GameObject("EnemyManager");
        obj.AddComponent<EnemyManager>();

        Debug.Log("[AUTO SETUP] Created EnemyManager");
    }

    private void SetupBootstrap()
    {
        BattleBootstrap bootstrap = FindObjectOfType<BattleBootstrap>();

        if (bootstrap != null)
            return;

        GameObject obj = new GameObject("BattleBootstrap");
        obj.AddComponent<BattleBootstrap>();

        Debug.Log("[AUTO SETUP] Created BattleBootstrap");
    }
}
