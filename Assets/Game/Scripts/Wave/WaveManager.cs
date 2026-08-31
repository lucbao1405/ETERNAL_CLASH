using UnityEngine;
using System.Collections;

namespace EternalClash.Wave
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance;

        public int currentWave = 0;
        public float delayBetweenWaves = 2f;

        [Header("Spawn Settings")]
        public float spawnRadius = 0f;

        [Header("Enemy Prefabs")]
        public GameObject slimePrefab;
        public GameObject archerPrefab;
        public GameObject wolfPrefab;

        private int aliveEnemies;
        private WaveData currentWaveData;
        private int spawnedCount;
        private int totalToSpawn;
        private bool isSpawning;

        public event System.Action<int> OnWaveComplete;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void OnEnable()
        {
            EternalClash.Enemy.EnemyDeathEvent.OnEnemyKilled += OnEnemyKilled;
        }

        private void OnDisable()
        {
            EternalClash.Enemy.EnemyDeathEvent.OnEnemyKilled -= OnEnemyKilled;
        }

        private void Start()
        {
            StartCoroutine(StartNextWave());
        }

        public IEnumerator StartNextWave()
        {
            yield return new WaitForSeconds(delayBetweenWaves);

            currentWave++;

            WaveData wave = BuildWave(currentWave);
            if (wave == null)
                yield break;

            Debug.Log($"[WAVE] Start Wave {currentWave} TotalEnemy={wave.TotalEnemies}");

            currentWaveData = wave;
            spawnedCount = 0;
            totalToSpawn = wave.TotalEnemies;
            isSpawning = true;

            yield return StartCoroutine(SpawnRoutine());

            aliveEnemies = wave.TotalEnemies;
        }

        public void RegisterEnemy()
        {
            aliveEnemies++;
        }

        public void EnemyKilled()
        {
            aliveEnemies--;

            if (aliveEnemies <= 0 && !isSpawning)
            {
                Debug.Log($"[WAVE] Wave {currentWave} Complete");
                OnWaveComplete?.Invoke(currentWave);
                StartCoroutine(StartNextWave());
            }
        }

        private IEnumerator SpawnRoutine()
        {
            if (currentWaveData == null)
                yield break;

            yield return new WaitForSeconds(currentWaveData.preWaveDelay);

            while (spawnedCount < totalToSpawn)
            {
                SpawnNextEnemy();
                spawnedCount++;

                if (spawnedCount < totalToSpawn)
                {
                    float interval = currentWaveData.spawnInterval;
                    yield return new WaitForSeconds(interval);
                }
            }

            isSpawning = false;
        }

        private void SpawnNextEnemy()
        {
            if (currentWaveData == null || currentWaveData.enemies.Count == 0)
                return;

            GameObject prefab = PickEnemyPrefab();
            if (prefab == null)
                return;

            Vector3 spawnPos = transform.position;
            if (spawnRadius > 0f)
            {
                spawnPos += new Vector3(
                    Random.Range(-spawnRadius, spawnRadius),
                    Random.Range(-spawnRadius, spawnRadius),
                    0f
                );
            }

            GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            if (EnemyManager.Instance != null)
                EnemyManager.Instance.RegisterEnemy(enemy);
        }

        private void OnEnemyKilled(GameObject enemy)
        {
            EnemyKilled();
        }

        private GameObject PickEnemyPrefab()
        {
            if (currentWaveData.enemies.Count == 0)
                return null;

            if (currentWaveData.enemies.Count == 1)
                return currentWaveData.enemies[0].enemyPrefab;

            int totalWeight = 0;
            foreach (var entry in currentWaveData.enemies)
                totalWeight += entry.spawnWeight;

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;

            foreach (var entry in currentWaveData.enemies)
            {
                cumulative += entry.spawnWeight;
                if (roll < cumulative)
                    return entry.enemyPrefab;
            }

            return currentWaveData.enemies[0].enemyPrefab;
        }

        private WaveData BuildWave(int wave)
        {
            WaveData data = ScriptableObject.CreateInstance<WaveData>();
            data.waveNumber = wave;

            data.spawnInterval = 1f;
            data.preWaveDelay = 1.5f;

            switch (wave)
            {
                case 1:
                    data.enemies.Add(new WaveEnemyEntry { enemyPrefab = slimePrefab, count = 1, spawnWeight = 1 });
                    break;

                case 2:
                    data.enemies.Add(new WaveEnemyEntry { enemyPrefab = slimePrefab, count = 1, spawnWeight = 1 });
                    data.enemies.Add(new WaveEnemyEntry { enemyPrefab = wolfPrefab, count = 1, spawnWeight = 1 });
                    break;

                case 3:
                    data.enemies.Add(new WaveEnemyEntry { enemyPrefab = slimePrefab, count = 1, spawnWeight = 1 });
                    data.enemies.Add(new WaveEnemyEntry { enemyPrefab = archerPrefab, count = 1, spawnWeight = 1 });
                    data.enemies.Add(new WaveEnemyEntry { enemyPrefab = wolfPrefab, count = 1, spawnWeight = 1 });
                    break;

                default:
                    data.enemies.Add(new WaveEnemyEntry { enemyPrefab = wolfPrefab, count = 2, spawnWeight = 2 });
                    data.enemies.Add(new WaveEnemyEntry { enemyPrefab = slimePrefab, count = 2, spawnWeight = 2 });
                    data.enemies.Add(new WaveEnemyEntry { enemyPrefab = archerPrefab, count = 1, spawnWeight = 1 });
                    break;
            }

            return data;
        }

        public bool IsSpawning => isSpawning;
    }
}
