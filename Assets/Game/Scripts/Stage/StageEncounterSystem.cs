using UnityEngine;
using System.Collections.Generic;

public class StageEncounterSystem : MonoBehaviour
{
    [SerializeField] private Transform enemyContainer;

    [System.Serializable]
    public class Encounter
    {
        public string encounterName;
        public List<GameObject> enemyPrefabs;
        public int amount;
        public bool completed;
    }

    public List<Encounter> encounters = new();
    private int currentEncounter;

    public void StartStage()
    {
        currentEncounter = 0;
        SpawnCurrentEncounter();
    }

    public void SpawnCurrentEncounter()
    {
        if (currentEncounter >= encounters.Count)
        {
            StageCompleted();
            return;
        }

        Encounter encounter = encounters[currentEncounter];
        encounter.completed = false;

        if (enemyContainer == null)
            enemyContainer = GameObject.Find("EnemyContainer")?.transform;

        for (int i = 0; i < encounter.amount; i++)
        {
            if (encounter.enemyPrefabs.Count == 0)
                continue;

            int index = Random.Range(0, encounter.enemyPrefabs.Count);
            GameObject enemy = Instantiate(encounter.enemyPrefabs[index], enemyContainer);
            enemy.transform.position = CombatLaneY.AlignToPlayerY(enemy.transform.position);
        }
    }

    public void EnemyDefeated()
    {
        if (AllEnemiesDead())
        {
            encounters[currentEncounter].completed = true;
            currentEncounter++;
            SpawnCurrentEncounter();
        }
    }

    private bool AllEnemiesDead()
    {
        return GameObject.FindGameObjectsWithTag("Enemy").Length == 0;
    }

    private void StageCompleted()
    {
        Debug.Log("Stage Clear - Spawn Chest");
    }
}
