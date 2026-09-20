using UnityEngine;
using EternalClash.Story;

namespace EternalClash.Stage
{
    public class StageStoryController : MonoBehaviour
    {
        public static StageStoryController Instance { get; private set; }

        [Header("References")]
        [SerializeField] private StoryManager storyManager;

        private int currentStage;

        public int CurrentStage => currentStage;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            storyManager ??= StoryManager.Instance;
        }

        public void SetCurrentStage(int stageIndex)
        {
            currentStage = stageIndex;
        }

        public void ShowPreBattleNarration()
        {
            if (storyManager == null) return;

            var narration = storyManager.GetPreBattleNarration(currentStage);
            if (!string.IsNullOrEmpty(narration))
            {
                Debug.Log($"[STORY] Stage {currentStage} Pre-Battle: {narration}");
            }
        }

        public void OnStageVictory()
        {
            if (storyManager == null) return;

            storyManager.OnStageComplete(currentStage);

            if (storyManager.HasNPCEncounter(currentStage))
            {
                Invoke(nameof(TriggerNPCEncounter), 1.5f);
            }
            else if (storyManager.IsStoryStage(currentStage))
            {
                Invoke(nameof(TriggerStoryEvent), 1.5f);
            }

            if (currentStage == 7)
            {
                Invoke(nameof(TriggerChapter1Ending), 3f);
            }
        }

        private void TriggerNPCEncounter()
        {
            storyManager.ShowNPCDialogue(currentStage);
            Debug.Log($"[STORY] NPC Encounter: {storyManager.GetNPCName(currentStage)}");
        }

        private void TriggerStoryEvent()
        {
            storyManager.ShowStoryDialogue(currentStage);
            Debug.Log($"[STORY] Story Event: {storyManager.GetStoryEventDescription(currentStage)}");
        }

        private void TriggerChapter1Ending()
        {
            storyManager.ShowLetterFromHer();
            Debug.Log("[STORY] Chapter 1 Complete - Letter from Her received");
        }

        public string GetStageDisplayName()
        {
            return storyManager?.GetStageName(currentStage) ?? $"Stage {currentStage}";
        }

        public string GetStageLocationName()
        {
            return storyManager?.GetStageLocation(currentStage) ?? "Unknown";
        }

        public string GetTutorialHint()
        {
            return storyManager?.GetTutorialFocus(currentStage) ?? "";
        }
    }
}
