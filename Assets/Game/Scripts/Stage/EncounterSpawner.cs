using UnityEngine;

public class EncounterSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Encounter
    {
        public GameObject enemyPrefab;
        public float spawnDistance;
    }

    public Encounter[] encounters;
    private bool[] spawned;

    private void Start()
    {
        spawned = new bool[encounters.Length];
    }

    private void Update()
    {
        if (StageManager.Instance == null) return;
        if (StageManager.Instance.CurrentState != StageManager.StageState.Running) return;

        for (int i = 0; i < encounters.Length; i++)
        {
            if (spawned[i]) continue;

            if (transform.position.x >= encounters[i].spawnDistance)
            {
                Spawn(encounters[i]);
                spawned[i] = true;
            }
        }
    }

    private void Spawn(Encounter encounter)
    {
        if (encounter.enemyPrefab == null) return;

        Instantiate(
            encounter.enemyPrefab,
            transform.position,
            Quaternion.identity
        );
    }
}
