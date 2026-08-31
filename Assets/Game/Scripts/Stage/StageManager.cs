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
        StartStage();
    }

    public void StartStage()
    {
        CurrentState = StageState.Running;
        Debug.Log("Stage Started");

        WaveManager waveManager = GetComponent<WaveManager>();
        if (waveManager != null)
        {
            StopAllCoroutines();
            waveManager.currentWave = 0;
            StartCoroutine(waveManager.StartNextWave());
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
