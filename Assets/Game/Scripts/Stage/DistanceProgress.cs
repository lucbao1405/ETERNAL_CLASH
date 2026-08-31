using UnityEngine;

public class DistanceProgress : MonoBehaviour
{
    public static DistanceProgress Instance { get; private set; }

    [SerializeField] private float stageDistance = 100f;
    private float currentDistance;

    public float Progress => Mathf.Clamp01(currentDistance / Mathf.Max(stageDistance, 1f));
    public float CurrentDistance => currentDistance;
    public float StageDistance => stageDistance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Instance = this;
    }

    public void ReduceDistance(float amount)
    {
        currentDistance = Mathf.Max(currentDistance - amount, 0f);
        Debug.Log($"[PROGRESS] Knockback penalty applied: -{amount:F1} units. Current: {currentDistance:F1}/{stageDistance:F1}");
    }

    public void AddDistance(float amount)
    {
        currentDistance += amount;
        if (currentDistance >= stageDistance)
        {
            currentDistance = stageDistance;
            StageManager.Instance?.CompleteStage();
        }
    }

    private void Update()
    {
        if (StageManager.Instance == null) return;
        if (StageManager.Instance.CurrentState != StageManager.StageState.Running) return;

        AddDistance(Time.deltaTime * 2.5f);
    }
}
