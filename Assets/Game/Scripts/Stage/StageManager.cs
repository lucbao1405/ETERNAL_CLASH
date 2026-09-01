using UnityEngine;
using EternalClash.Wave;

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
            StopAllCoroutines();
            waveManager.currentWave = 0;
            StartCoroutine(waveManager.StartNextWave());
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
        Debug.Log("Stage Victory");
    }

    public void FailStage()
    {
        if (CurrentState != StageState.Running) return;

        CurrentState = StageState.Defeat;
        Debug.Log("Stage Defeat");
    }
}
