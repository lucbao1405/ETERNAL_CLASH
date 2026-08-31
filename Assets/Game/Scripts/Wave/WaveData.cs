using UnityEngine;
using System.Collections.Generic;

namespace EternalClash.Wave
{
    [CreateAssetMenu(fileName = "WaveData", menuName = "Wave/WaveData", order = 0)]
    public class WaveData : ScriptableObject
    {
        public int waveNumber;
        public List<WaveEnemyEntry> enemies = new List<WaveEnemyEntry>();
        public float spawnInterval = 1f;
        public float preWaveDelay = 2f;

        public int TotalEnemies
        {
            get
            {
                int total = 0;
                foreach (var entry in enemies)
                    total += entry.count;
                return total;
            }
        }
    }

    [System.Serializable]
    public class WaveEnemyEntry
    {
        public GameObject enemyPrefab;
        public int count = 1;
        [Tooltip("Weight for random selection when picking next enemy to spawn")]
        public int spawnWeight = 1;
    }
}
