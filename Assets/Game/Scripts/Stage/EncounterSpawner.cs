using UnityEngine;

public class EncounterSpawner : MonoBehaviour
{
    [SerializeField] private Transform enemyContainer;
    public static EncounterSpawner Instance { get; private set; }

    [System.Serializable]
    public class Encounter
    {
        public GameObject enemyPrefab;
        public float spawnDistance;
    }

    public Encounter[] encounters;
    private bool[] spawned;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Instance = this;
    }

    private void Start()
    {
        if (encounters != null)
            spawned = new bool[encounters.Length];
    }

    private void Update()
    {
        if (StageManager.Instance == null) return;
        if (StageManager.Instance.CurrentState != StageManager.StageState.Running) return;
        if (encounters == null || spawned == null) return;

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

    public void SpawnEncounter(EncounterData encounterData)
    {
        if (encounterData == null) return;
        Debug.Log("[ENCOUNTER] Spawning encounter data");
    }

    private void Spawn(Encounter encounter)
    {
        if (encounter.enemyPrefab == null) return;

        Vector3 spawnPosition = CombatLaneY.AlignToPlayerY(transform.position);
        Instantiate(
            encounter.enemyPrefab,
            spawnPosition,
            Quaternion.identity,
            enemyContainer
        );
    }
}
