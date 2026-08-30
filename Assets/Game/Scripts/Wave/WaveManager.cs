using UnityEngine;
using System.Collections;

namespace EternalClash.Wave
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance;

        public int currentWave = 0;
        public int enemiesPerWave = 5;
        public float delayBetweenWaves = 5f;

        private int aliveEnemies;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            StartCoroutine(StartNextWave());
        }

        public IEnumerator StartNextWave()
        {
            yield return new WaitForSeconds(delayBetweenWaves);

            currentWave++;
            aliveEnemies = enemiesPerWave + currentWave * 2;

            Debug.Log($"[WAVE] Start Wave {currentWave} Enemy={aliveEnemies}");

            // Connect EnemySpawner here
            // Spawn count will be sent to spawner
        }

        public void RegisterEnemy()
        {
            aliveEnemies++;
        }

        public void EnemyKilled()
        {
            aliveEnemies--;

            if (aliveEnemies <= 0)
            {
                Debug.Log($"[WAVE] Wave {currentWave} Complete");
                StartCoroutine(StartNextWave());
            }
        }
    }
}
