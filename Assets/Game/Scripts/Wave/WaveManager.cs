using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EternalClash.Enemy;

namespace EternalClash.Wave
{
    /// <summary>
    /// Gan script nay vao GameObject dat tai vi tri Spawn Point duy nhat tren map -
    /// moi quai o moi Wave deu Instantiate tai transform.position cua chinh GameObject nay.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Stage Data (Data-Driven)")]
        [SerializeField] private StageData stageData;

        public StageData CurrentStageData => stageData;

        /// <summary>
        /// Gan StageData (du lieu cac Wave cua 1 man) cho WaveManager.
        /// Goi truoc BeginStage() - thong thuong tu StageManager de moi lan vao
        /// Battle tu dong nap dung man theo tien trinh da luu.
        /// </summary>
        public void SetStageData(StageData data)
        {
            stageData = data;
        }

        [Header("Timing")]
        [Tooltip("Do tre truoc khi Wave dau tien cua man bat dau")]
        [SerializeField] private float firstWaveDelay = 0f;

        [Tooltip("He so lam cham spawn cho cac wave TU WAVES 2 TRO DI (1.5 = cham hon 50%). " +
                 "Wave dau giu nguyen toc do trong WaveData de quai chay ra luon.")]
        [Min(1f)] [SerializeField] private float laterWaveIntervalMultiplier = 1.5f;

        private readonly HashSet<GameObject> aliveEnemies = new HashSet<GameObject>();
        private int currentWaveIndex = -1;
        private int groupsStillSpawning;
        private Coroutine stageRoutine;

        public int CurrentWaveNumber => currentWaveIndex + 1;
        public bool IsSpawning => groupsStillSpawning > 0;
        public int AliveCount => aliveEnemies.Count;

        /// <summary>Tong so dot cua man dang chay.</summary>
        public int TotalWaves => stageData != null && stageData.waves != null ? stageData.waves.Length : 0;

        /// <summary>Tong so quai cua dot dang danh.</summary>
        public int CurrentWaveEnemyCount { get; private set; }

        /// <summary>So quai da ha trong dot dang danh.</summary>
        public int CurrentWaveKilled { get; private set; }

        public event System.Action<int> OnWaveComplete;
        public event System.Action OnStageComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            EnemyDeathEvent.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            EnemyDeathEvent.OnEnemyKilled -= HandleEnemyKilled;
        }

        /// <summary>
        /// Goi ham nay tu StageManager de bat dau man. WaveManager tu quan ly coroutine
        /// cua chinh no (StopCoroutine ban cu neu co) de tranh chay trung 2 lan.
        /// </summary>
        public void BeginStage()
        {
            if (stageRoutine != null)
                StopCoroutine(stageRoutine);

            aliveEnemies.Clear();
            currentWaveIndex = -1;
            stageRoutine = StartCoroutine(RunStage());
        }

        private IEnumerator RunStage()
        {
            if (stageData == null || stageData.waves == null || stageData.waves.Length == 0)
            {
                Debug.LogWarning("[WAVE] StageData chua duoc gan hoac khong co Wave nao.");
                yield break;
            }

            yield return new WaitForSeconds(firstWaveDelay);

            for (currentWaveIndex = 0; currentWaveIndex < stageData.waves.Length; currentWaveIndex++)
            {
                WaveData wave = stageData.waves[currentWaveIndex];
                if (wave == null)
                    continue;

                yield return StartCoroutine(RunWave(wave));

                OnWaveComplete?.Invoke(CurrentWaveNumber);

                yield return new WaitForSeconds(wave.delayAfterClear);
            }

            Debug.Log("[WAVE] Stage cleared - Win Stage");
            OnStageComplete?.Invoke();
            StageManager.Instance?.CompleteStage();
        }

        private IEnumerator RunWave(WaveData wave)
        {
            Debug.Log($"[WAVE] Start Wave {wave.waveNumber} TotalEnemy={wave.TotalEnemies}");

            // UI tien trinh doc 2 con so nay de hien "Dot x/y" va so quai da ha.
            CurrentWaveEnemyCount = wave.TotalEnemies;
            CurrentWaveKilled = 0;

            groupsStillSpawning = wave.groups.Count;

            foreach (EnemySpawnGroup group in wave.groups)
                StartCoroutine(SpawnGroup(group));

            // Cho den khi: spawn xong TAT CA nhom VA khong con quai nao song.
            // Phai cho ca 2 dieu kien - tranh truong hop 1 nhom con dang cho
            // initialDelay trong khi nhom khac da chet het, tuong nham la Wave xong.
            while (groupsStillSpawning > 0 || aliveEnemies.Count > 0)
            {
                // Luoi don dep phong ve: neu 1 quai bi Destroy boi nguyen nhan khac
                // ngoai luong EnemyHealth -> EnemyDeathEvent (vd huy scene giua chung),
                // no van duoc don khoi danh sach thay vi ket Wave mai mai.
                aliveEnemies.RemoveWhere(enemy => enemy == null);
                yield return null;
            }

            Debug.Log($"[WAVE] Wave {wave.waveNumber} Complete");
        }

        private IEnumerator SpawnGroup(EnemySpawnGroup group)
        {
            if (group.initialDelay > 0f)
                yield return new WaitForSeconds(group.initialDelay);

            for (int i = 0; i < group.count; i++)
            {
                SpawnEnemy(group.enemyPrefab);

                float interval = group.spawnInterval * (currentWaveIndex > 0 ? laterWaveIntervalMultiplier : 1f);
                if (i < group.count - 1 && interval > 0f)
                    yield return new WaitForSeconds(interval);
            }

            groupsStillSpawning--;
        }

        private void SpawnEnemy(GameObject prefab)
        {
            if (prefab == null)
                return;

            // Diem spawn duy nhat = vi tri cua chinh GameObject dang gan WaveManager nay.
            Vector3 spawnPosition = CombatLaneY.AlignToPlayerY(transform.position);
            GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);

            aliveEnemies.Add(enemy);
        }

        // Nhan bao chet tu EnemyDeathEvent (xem huong dan phan 2 ben duoi).
        // Dung HashSet.Remove: enemy khong con trong tap (da bi go boi luoi don dep,
        // hoac bao trung) thi don gian khong lam gi - khong bao gio bi am so luong.
        private void HandleEnemyKilled(GameObject enemy)
        {
            if (aliveEnemies.Remove(enemy))
                CurrentWaveKilled++;
        }
    }
}
