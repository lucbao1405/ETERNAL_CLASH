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

    private WaveManager waveManager;

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
        CurrentState = StageState.Running;
        Debug.Log("Stage Started");

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

    public void CompleteStage()
    {
        if (CurrentState != StageState.Running) return;

        CurrentState = StageState.Victory;
            EternalClash.Stage.StageCompleteController.Instance?.BeginPostStageFlow();
            if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
            {
                SaveManager.Instance.Data.stageLevel = Mathf.Min(5, SaveManager.Instance.Data.stageLevel + 1);
                SaveManager.Instance.Save();
            }
        Debug.Log("Stage Victory");
    }

    public void FailStage()
    {
        if (CurrentState != StageState.Running) return;

        CurrentState = StageState.Defeat;
        Debug.Log("Stage Defeat");

        EternalClash.Stage.StageCompleteController.Instance?.BeginDefeatFlow();
    }
}
