using UnityEngine;

namespace EternalClash.Stage
{
    public class StageProgressController : MonoBehaviour
    {
        public StageData currentStage;
        private int currentEncounterIndex;
        private bool stageCompleted;

        public bool IsStageCompleted => stageCompleted;

        public event System.Action OnStageCompleted;

        private float stageStartTime;

        public void StartStage()
        {
            currentEncounterIndex = 0;
            stageCompleted = false;
            stageStartTime = Time.time;
            StartNextEncounter();
        }

        public float GetStageTime()
        {
            return Time.time - stageStartTime;
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
            if (stageCompleted) return;
            stageCompleted = true;

            if (StageManager.Instance != null)
                StageManager.Instance.CompleteStage();

            OnStageCompleted?.Invoke();
        }
    }
}
