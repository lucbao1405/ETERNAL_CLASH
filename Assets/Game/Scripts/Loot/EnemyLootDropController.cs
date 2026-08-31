using UnityEngine;
using EternalClash.Village;

public class EnemyLootDropController : MonoBehaviour
{
    [SerializeField] private GameObject[] dropPrefabs;

    [Header("Rewards granted on death (per GDD 3.1 / 3.5)")]
    [SerializeField] private int expReward = 10;
    [SerializeField] private int oreReward = 1;
    [SerializeField] private int leatherReward = 0;

    public void DropLoot()
    {
        if (dropPrefabs != null && dropPrefabs.Length > 0)
        {
            int index = Random.Range(0, dropPrefabs.Length);
            if (dropPrefabs[index] != null)
                Instantiate(dropPrefabs[index], transform.position, Quaternion.identity);
        }

        GrantRewards();
    }

    private void GrantRewards()
    {
        PlayerStatSystem.Instance?.AddExp(expReward);

        if (GoldSystem.Instance == null) return;

        int ore = oreReward;
        int leather = leatherReward;

        // LUCK raises rare material drop rate (GDD 3.5)
        if (PlayerStatSystem.Instance != null && Random.value < PlayerStatSystem.Instance.RareDropRate)
        {
            ore += oreReward;
            leather += leatherReward;
        }

        if (ore > 0 || leather > 0)
            GoldSystem.Instance.AddMaterials(ore, leather);
    }
}
