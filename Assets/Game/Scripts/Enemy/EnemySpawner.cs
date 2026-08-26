using UnityEngine;

namespace EternalClash.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        public GameObject enemyPrefab;
        public Transform spawnPoint;
        public float spawnInterval = 5f;

        private float timer;

        private void Update()
        {
            timer += Time.deltaTime;

            if (timer >= spawnInterval)
            {
                SpawnEnemy();
                timer = 0f;
            }
        }

        private void SpawnEnemy()
        {
            if (enemyPrefab == null || spawnPoint == null)
                return;

            Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        }
    }
}
