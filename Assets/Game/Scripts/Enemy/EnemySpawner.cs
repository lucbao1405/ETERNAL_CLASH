using UnityEngine;

namespace EternalClash.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        public GameObject enemyPrefab;
        public Transform spawnPoint;
        [SerializeField] private Transform enemyContainer;
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

            if (enemyContainer == null)
                enemyContainer = GameObject.Find("EnemyContainer")?.transform;

            Vector3 spawnPosition = CombatLaneY.AlignToPlayerY(spawnPoint.position);
            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, enemyContainer);
        }
    }
}
