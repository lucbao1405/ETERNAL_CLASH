using UnityEngine;

public class DistanceProgress : MonoBehaviour
{
    [SerializeField] private float stageDistance = 100f;
    private float currentDistance;

    public float Progress => currentDistance / stageDistance;

    private void Update()
    {
        if (StageManager.Instance == null) return;
        if (StageManager.Instance.CurrentState != StageManager.StageState.Running) return;

        currentDistance += Time.deltaTime * 2.5f;

        if (currentDistance >= stageDistance)
        {
            StageManager.Instance.CompleteStage();
        }
    }
}
