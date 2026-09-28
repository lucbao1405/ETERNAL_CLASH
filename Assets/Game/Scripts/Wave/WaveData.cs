using UnityEngine;
using System.Collections.Generic;

namespace EternalClash.Wave
{
    [System.Serializable]
    public class EnemySpawnGroup
    {
        [Tooltip("Loai quai se spawn trong nhom nay (Slime, Soi, Goblin...)")]
        public GameObject enemyPrefab;

        [Min(1)] public int count = 1;

        [Tooltip("Khoang cach thoi gian (giay) giua tung con trong CUNG nhom nay")]
        [Min(0f)] public float spawnInterval = 0.5f;

        [Tooltip("Do tre (giay) truoc khi nhom nay bat dau spawn, tinh tu luc Wave bat dau. " +
                 "Dung de cho 1 nhom spawn truoc nhom khac trong cung Wave (vd Goblin ra truoc, Slime ra sau 1s).")]
        [Min(0f)] public float initialDelay = 0f;
    }

    [CreateAssetMenu(fileName = "WaveData", menuName = "Wave/WaveData", order = 0)]
    public class WaveData : ScriptableObject
    {
        [Min(1)] public int waveNumber = 1;

        [Tooltip("Cac nhom quai trong Wave nay. Moi nhom spawn doc lap (song song), " +
                 "khong phai lan luot nhu 1 hang doi chung. Tat ca cung ra tu 1 Spawn Point.")]
        public List<EnemySpawnGroup> groups = new List<EnemySpawnGroup>();

        [Tooltip("So giay cho SAU KHI Wave nay het quai, truoc khi kich hoat Wave tiep theo")]
        [Min(0f)] public float delayAfterClear = 2f;

        public int TotalEnemies
        {
            get
            {
                int total = 0;
                foreach (var group in groups)
                    total += Mathf.Max(0, group.count);
                return total;
            }
        }
    }
}
