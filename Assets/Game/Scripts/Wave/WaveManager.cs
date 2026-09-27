using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EternalClash.Combat;
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

        [Header("Spacing")]
        [Tooltip("So quai toi da song dong thoi tren truong. Chi spawn quai moi khi so quai " +
                 "dang song nho hon con so nay (1-3 con la dang danh).")]
        [Min(1)] [SerializeField] private int maxAliveOnField = 3;

        [Tooltip("Khoang cach toi thieu (giay) giua 2 lan spawn bat ky, ke ca 2 nhom khac nhau, " +
                 "de quai khong ra cung luc va de len nhau.")]
        [Min(0f)] [SerializeField] private float minSpawnGap = 0.8f;

        [Header("Spawn Position")]
        [Tooltip("Khoang cach du ra ngoai ria phai man hinh khi spawn quai, de quai khong lo hinh ngay tai diem spawn.")]
        [SerializeField] private float spawnEdgePad = 1.5f;

        private float nextSpawnAllowed;

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
            nextSpawnAllowed = 0f;
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

            // Man thu thach boss chon tu panel "select boss" o Town: thay toan bo
            // man bang 1 wave duy nhat spawn dung con quai duoc chon. Doi luon
            // stageData de thanh tien trinh (TotalWaves) bao "1/1" thay vi cua man thuong.
            if (BossChallenge.Active)
            {
                WaveData bossWave = BuildBossChallengeWave();
                if (bossWave != null)
                {
                    StageData bossStage = ScriptableObject.CreateInstance<StageData>();
                    bossStage.waves = new[] { bossWave };
                    stageData = bossStage; // chi trong tran nay, khong luu vao asset
                }
                else
                {
                    BossChallenge.Cancel(); // khong tim thay prefab -> danh man thuong
                }
            }

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

        /// <summary>
        /// Wave duy nhat cho man thu thach boss: 1 con quai duoc chon. Mau duoc
        /// nhan trong ApplyStageScaling (x BossChallenge.HpMultiplier).
        /// </summary>
        private WaveData BuildBossChallengeWave()
        {
            GameObject prefab = FindBossPrefab();
            if (prefab == null)
            {
                Debug.LogWarning($"[WAVE] Khong tim thay quai '{BossChallenge.EnemyId}' trong StageData -> huy thu thach boss.");
                return null;
            }

            WaveData wave = ScriptableObject.CreateInstance<WaveData>();
            wave.waveNumber = 1;
            wave.groups = new List<EnemySpawnGroup>
            {
                new EnemySpawnGroup { enemyPrefab = prefab, count = 1, spawnInterval = 0.5f, initialDelay = 0f }
            };
            return wave;
        }

        /// <summary>Tim prefab quai duoc chon: xem man hien tai truoc, khong co thi quet toan bo catalog.</summary>
        private GameObject FindBossPrefab()
        {
            string wanted = BossChallenge.EnemyId;
            if (string.IsNullOrEmpty(wanted))
                return null;

            GameObject match = FindBossPrefabInStage(stageData, wanted);
            if (match != null || StageManager.Instance == null)
                return match;

            // Quai khong nam trong man hien tai (vd chon Goblin Mage nhung dang o man 1 chi co Slime).
            for (int level = 1; level <= StageManager.Instance.MaxStageLevel; level++)
            {
                match = FindBossPrefabInStage(StageManager.Instance.GetStageData(level), wanted);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static GameObject FindBossPrefabInStage(StageData data, string wanted)
        {
            if (data == null || data.waves == null)
                return null;

            foreach (WaveData wave in data.waves)
            {
                if (wave == null || wave.groups == null)
                    continue;

                foreach (EnemySpawnGroup group in wave.groups)
                {
                    if (group?.enemyPrefab == null)
                        continue;

                    if (BossChallenge.MatchesPrefab(group.enemyPrefab.name, wanted))
                        return group.enemyPrefab;
                }
            }

            return null;
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
                // Giu slot roi moi spawn: moi thoi diem chi 1 con duoc spawn (cac nhom
                // chay song song nhung phai xep hang qua chung 1 slot nay).
                while (!TryClaimSpawnSlot())
                    yield return null;

                SpawnEnemy(group.enemyPrefab);

                float interval = group.spawnInterval * (currentWaveIndex > 0 ? laterWaveIntervalMultiplier : 1f);
                if (i < group.count - 1 && interval > 0f)
                    yield return new WaitForSeconds(interval);
            }

            groupsStillSpawning--;
        }

        /// Chi 1 coroutine giu duoc slot trong 1 thoi diem: kiem tra va dat lich chay
        /// trong cung 1 frame, nen 2 nhom khong the spawn cung luc.
        /// Con dieu kien: khong vuot maxAliveOnField quai song + da qua minSpawnGap.
        private bool TryClaimSpawnSlot()
        {
            aliveEnemies.RemoveWhere(enemy => enemy == null);
            if (aliveEnemies.Count >= maxAliveOnField || Time.time < nextSpawnAllowed)
                return false;

            nextSpawnAllowed = Time.time + minSpawnGap;
            return true;
        }

        private void SpawnEnemy(GameObject prefab)
        {
            if (prefab == null)
                return;

            // Diem spawn duy nhat = vi tri cua chinh GameObject dang gan WaveManager nay,
            // nhung day X ra ngoai ria phai man hinh de quai trôi vao thay vi lo hinh.
            Vector3 spawnPosition = CombatLaneY.AlignToPlayerY(transform.position);
            spawnPosition.x = CombatLaneY.GetOffScreenRightX(spawnEdgePad);
            GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
            ApplyStageScaling(enemy);

            aliveEnemies.Add(enemy);
        }

        // Do kho tang dan theo man: +15% mau va sat thuong cho moi man (man 7 ~ x1.9),
        // ket hop voi so luong quai tang dan trong tung StageData.
        private void ApplyStageScaling(GameObject enemy)
        {
            int level = StageManager.Instance != null ? StageManager.Instance.CurrentStageLevel : 1;
            float damageMultiplier = 1f + 0.15f * (level - 1);

            // Man thu thach boss (panel "select boss" o Town): nhieu mau hon binh
            // thuong nhung giu nguyen sat thuong, khong thi quai yeu thanh onet-shot.
            float healthMultiplier = ComputeHealthMultiplier(level, BossChallenge.Active);

            if (healthMultiplier > 1f)
                enemy.GetComponent<EnemyHealthSystem>()?.ScaleHealth(healthMultiplier);

            if (damageMultiplier > 1f)
                enemy.GetComponent<EnemyAttack>()?.ScaleDamage(damageMultiplier);
        }

        /// <summary>He so mau sau cung cho quai theo man (pure de tu kiem tra).</summary>
        internal static float ComputeHealthMultiplier(int stageLevel, bool bossChallenge)
        {
            float multiplier = 1f + 0.15f * (stageLevel - 1);
            return bossChallenge ? multiplier * BossChallenge.HpMultiplier : multiplier;
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
