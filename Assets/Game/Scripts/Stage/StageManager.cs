using UnityEngine;
using EternalClash.Wave;
using EternalClash.Core.Save;

public class StageManager : MonoBehaviour
{
    public enum StageState
    {
        Preparing,
        Running,
        Victory,
        Defeat
    }

    public static StageManager Instance { get; private set; }

    public StageState CurrentState { get; private set; }
    public float BattleDuration { get; private set; }

    [Header("Stage Data (Data-Driven)")]
    [Tooltip("Danh sach StageData theo dung thu tu: index 0 = Stage 1, index 1 = Stage 2, ... " +
             "Khi vao Battle, he thong tu chon StageData tuong ung voi SaveManager.Data.stageLevel.")]
    [SerializeField] private EternalClash.Wave.StageData[] stageCatalog;

    private WaveManager waveManager;
    private float battleStartTime = -1f;

    /// <summary>So man dang chay (doc tu tien trinh da luu truoc khi bat dau).</summary>
    public int CurrentStageLevel { get; private set; } = 1;

    /// <summary>StageData duoc nap cho man dang chay.</summary>
    public EternalClash.Wave.StageData CurrentStageData { get; private set; }

    /// <summary>Tong so man hien co (= do dai stageCatalog). Neu chua cau hinh thi mac dinh 1.</summary>
    public int MaxStageLevel
    {
        get
        {
            if (stageCatalog == null || stageCatalog.Length == 0)
                return 1;
            return stageCatalog.Length;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        EnsureWaveManager();
        StartStage();
    }

    private void EnsureWaveManager()
    {
        waveManager = GetComponent<WaveManager>();
        if (waveManager == null)
            waveManager = FindObjectOfType<WaveManager>();

        if (waveManager == null)
        {
            GameObject waveObject = new GameObject("WaveManager");
            waveManager = waveObject.AddComponent<WaveManager>();
            Debug.Log("[STAGE] Auto-created WaveManager because none existed in scene.");
        }
    }

    public void StartStage()
    {
        EnsureWaveManager();
        LoadStageForCurrentProgress();

        CurrentState = StageState.Running;
        BattleDuration = 0f;
        battleStartTime = Time.time;

        // Dat lai bo dem dau tran de popup ket qua bao dung so EXP/vang kiem duoc
        // trong chinh man nay. Vang va EXP van duoc cong ngay luc nhat, day chi la
        // bo dem de hien thi.
        EternalClash.Village.PlayerStatSystem.Instance?.ResetSessionCounters();
        EternalClash.Village.GoldSystem.Instance?.ResetSessionCounters();
        Debug.Log($"[STAGE] Stage {CurrentStageLevel} Started - {StageDataName(CurrentStageData)}");

        if (waveManager != null)
        {
            // WaveManager tu quan ly coroutine cua chinh no (BeginStage tu StopCoroutine
            // ban cu neu co) - khong StartCoroutine ho tu ben ngoai de tranh chay trung.
            waveManager.BeginStage();
        }
        else
        {
            Debug.LogWarning("[STAGE] WaveManager still missing after EnsureWaveManager().");
        }
    }

    /// <summary>
    /// Doc stage da luu (SaveManager.Data.stageLevel) va nap StageData tuong ung
    /// vao WaveManager truoc khi chay man. Khong hard-code stage nao trong code:
    /// danh sach StageData duoc cau hinh o scene qua stageCatalog.
    /// </summary>
    private void LoadStageForCurrentProgress()
    {
        int savedLevel = 1;
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
            savedLevel = SaveManager.Instance.Data.stageLevel;

        CurrentStageLevel = Mathf.Clamp(savedLevel, 1, MaxStageLevel);
        CurrentStageData = GetStageData(CurrentStageLevel);

        if (waveManager != null)
            waveManager.SetStageData(CurrentStageData);
    }

    /// <summary>Lay StageData theo so man (1 = man dau tien), gioi han trong stageCatalog.</summary>
    public EternalClash.Wave.StageData GetStageData(int stageLevel)
    {
        if (stageCatalog == null || stageCatalog.Length == 0)
        {
            Debug.LogError("[STAGE] stageCatalog chua duoc cau hinh trong scene Battle. " +
                           "Gan danh sach StageData theo thu tu Stage 1..n.");
            return null;
        }

        int index = Mathf.Clamp(stageLevel, 1, stageCatalog.Length) - 1;
        return stageCatalog[index];
    }

    private string StageDataName(EternalClash.Wave.StageData data)
    {
        if (data == null)
            return "(no StageData)";

        int waveCount = data.waves != null ? data.waves.Length : 0;
        return $"{data.name} ({waveCount} waves)";
    }

    public float GetBattleTime()
    {
        if (battleStartTime >= 0f)
            return Time.time - battleStartTime;
        return BattleDuration;
    }

    private void StopBattleTimer()
    {
        if (battleStartTime < 0f)
            return;

        BattleDuration = Time.time - battleStartTime;
        battleStartTime = -1f;
    }

    public void CompleteStage()
    {
        if (CurrentState != StageState.Running) return;

        StopBattleTimer();
        CurrentState = StageState.Victory;
        Debug.Log("Stage Victory");

        if (EternalClash.Stage.StageCompleteController.Instance != null)
        {
            EternalClash.Stage.StageCompleteController.Instance.BeginPostStageFlow();
        }
        else
        {
            // Khong co controller thi khong ai goi BattlePopupController -> khong
            // co popup thang. Bao ro thay vi im lang bo qua bang toan tu "?.".
            Debug.LogError("[STAGE] Thang man nhung StageCompleteController.Instance dang null " +
                           "-> khong hien duoc popup Win.");
        }

        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            // Clear thanh cong -> tang len man ke tiep, gioi han o man cuoi cung
            // (so man lay tu stageCatalog de khong hard-code).
            int nextStage = SaveManager.Instance.Data.stageLevel + 1;
            SaveManager.Instance.Data.stageLevel = Mathf.Min(MaxStageLevel, nextStage);
            SaveManager.Instance.Save();
        }
    }

    public void FailStage()
    {
        if (CurrentState != StageState.Running) return;

        StopBattleTimer();
        CurrentState = StageState.Defeat;
        Debug.Log("Stage Defeat");

        if (EternalClash.Stage.StageCompleteController.Instance != null)
        {
            EternalClash.Stage.StageCompleteController.Instance.BeginDefeatFlow();
        }
        else
        {
            Debug.LogError("[STAGE] Thua man nhung StageCompleteController.Instance dang null " +
                           "-> khong hien duoc popup Lose.");
        }
    }
}
