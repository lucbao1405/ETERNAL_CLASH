using UnityEngine;

public class StageProgressController : MonoBehaviour
{
    public StageData currentStage;
    private int currentEncounterIndex;
    private bool stageCompleted;

    public void StartStage()
    {
        currentEncounterIndex = 0;
        stageCompleted = false;
        StartNextEncounter();
    }

    public void StartNextEncounter()
    {
        if (currentStage == null) return;

        if (currentEncounterIndex >= currentStage.encounters.Count)
        {
            CompleteStage();
            return;
        }

        var encounter = currentStage.encounters[currentEncounterIndex];
        currentEncounterIndex++;

        EncounterSpawner.Instance.SpawnEncounter(encounter);
    }

    public void OnEncounterCleared()
    {
        StartNextEncounter();
    }

    private void CompleteStage()
    {
        stageCompleted = true;

        ChestSpawnFlow.Instance.SpawnChest();
    }
}
