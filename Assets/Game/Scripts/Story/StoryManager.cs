using UnityEngine;
using System.Collections.Generic;
using EternalClash.Core.Save;
using EternalClash.Village;
using EternalClash.Dialogue;

namespace EternalClash.Story
{
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private DialogueManager dialogueManager;

        private HashSet<string> viewedDialogues = new();
        private HashSet<string> receivedLetters = new();

        public event System.Action<string> OnLetterReceived;
        public event System.Action OnChapter1Complete;
        public event System.Action<int> OnNPCEncounter;

        public bool HasCompletedChapter1 => SaveManager.Instance?.Data?.stageLevel > 7;
        public int CurrentChapter => HasCompletedChapter1 ? 2 : 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoadProgress();
            dialogueManager ??= FindObjectOfType<DialogueManager>();
        }

        private void LoadProgress()
        {
            var data = SaveManager.Instance?.Data;
            if (data == null) return;

            if (data.unlockedLetters != null)
                receivedLetters = new HashSet<string>(data.unlockedLetters);
        }

        #region Stage Lore

        public string GetStageName(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.NAME,
                2 => Chapter1Content.Stage2.NAME,
                3 => Chapter1Content.Stage3.NAME,
                4 => Chapter1Content.Stage4.NAME,
                5 => Chapter1Content.Stage5.NAME,
                6 => Chapter1Content.Stage6.NAME,
                7 => Chapter1Content.Stage7.NAME,
                _ => $"Stage {stageIndex}"
            };
        }

        public string GetStageLocation(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.LOCATION,
                2 => Chapter1Content.Stage2.LOCATION,
                3 => Chapter1Content.Stage3.LOCATION,
                4 => Chapter1Content.Stage4.LOCATION,
                5 => Chapter1Content.Stage5.LOCATION,
                6 => Chapter1Content.Stage6.LOCATION,
                7 => Chapter1Content.Stage7.LOCATION,
                _ => "Unknown"
            };
        }

        public string GetStageDescription(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.DESCRIPTION,
                2 => Chapter1Content.Stage2.DESCRIPTION,
                3 => Chapter1Content.Stage3.DESCRIPTION,
                4 => Chapter1Content.Stage4.DESCRIPTION,
                5 => Chapter1Content.Stage5.DESCRIPTION,
                6 => Chapter1Content.Stage6.DESCRIPTION,
                7 => Chapter1Content.Stage7.DESCRIPTION,
                _ => ""
            };
        }

        public string GetPreBattleNarration(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.PRE_BATTLE,
                2 => Chapter1Content.Stage2.PRE_BATTLE,
                3 => Chapter1Content.Stage3.PRE_BATTLE,
                4 => Chapter1Content.Stage4.PRE_BATTLE,
                5 => Chapter1Content.Stage5.PRE_BATTLE,
                6 => Chapter1Content.Stage6.PRE_BATTLE,
                7 => Chapter1Content.Stage7.PRE_BATTLE,
                _ => null
            };
        }

        public string GetPostVictoryNarration(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.POST_VICTORY,
                2 => Chapter1Content.Stage2.POST_VICTORY,
                3 => Chapter1Content.Stage3.POST_VICTORY,
                4 => Chapter1Content.Stage4.POST_VICTORY,
                5 => Chapter1Content.Stage5.POST_VICTORY,
                6 => Chapter1Content.Stage6.POST_VICTORY,
                7 => Chapter1Content.Stage7.POST_VICTORY,
                _ => null
            };
        }

        public string[] GetStageEnemies(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.ENEMIES,
                2 => Chapter1Content.Stage2.ENEMIES,
                3 => Chapter1Content.Stage3.ENEMIES,
                4 => Chapter1Content.Stage4.ENEMIES,
                5 => Chapter1Content.Stage5.ENEMIES,
                6 => Chapter1Content.Stage6.ENEMIES,
                7 => Chapter1Content.Stage7.ENEMIES,
                _ => new string[0]
            };
        }

        public string[] GetStageDrops(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.DROPS,
                2 => Chapter1Content.Stage2.DROPS,
                3 => Chapter1Content.Stage3.DROPS,
                4 => Chapter1Content.Stage4.DROPS,
                5 => Chapter1Content.Stage5.DROPS,
                6 => Chapter1Content.Stage6.DROPS,
                7 => Chapter1Content.Stage7.DROPS,
                _ => new string[0]
            };
        }

        public string GetTutorialFocus(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.TUTORIAL_FOCUS,
                2 => Chapter1Content.Stage2.TUTORIAL_FOCUS,
                3 => Chapter1Content.Stage3.TUTORIAL_FOCUS,
                4 => Chapter1Content.Stage4.TUTORIAL_FOCUS,
                5 => Chapter1Content.Stage5.TUTORIAL_FOCUS,
                6 => Chapter1Content.Stage6.TUTORIAL_FOCUS,
                7 => Chapter1Content.Stage7.TUTORIAL_FOCUS,
                _ => ""
            };
        }

        #endregion

        #region NPC Encounters

        public bool HasNPCEncounter(int stageIndex)
        {
            return stageIndex switch
            {
                1 => true, // Blacksmith Garen (end of Stage 1)
                2 => true, // Witch Elara (end of Stage 2)
                5 => true, // Ela - childhood friend (end of Stage 5)
                _ => false
            };
        }

        public string GetNPCName(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.NPC_NAME,
                2 => Chapter1Content.Stage2.NPC_NAME,
                5 => Chapter1Content.Stage5.NPC_NAME,
                _ => null
            };
        }

        public string GetNPCRole(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.NPC_ROLE,
                2 => Chapter1Content.Stage2.NPC_ROLE,
                5 => Chapter1Content.Stage5.NPC_ROLE,
                _ => null
            };
        }

        public string[] GetNPCDialogue(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.NPC_DIALOGUE,
                2 => Chapter1Content.Stage2.NPC_DIALOGUE,
                5 => Chapter1Content.Stage5.NPC_DIALOGUE,
                _ => null
            };
        }

        public string GetUnlockReward(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.UNLOCK_REWARD,
                2 => Chapter1Content.Stage2.UNLOCK_REWARD,
                5 => Chapter1Content.Stage5.UNLOCK_REWARD,
                _ => null
            };
        }

        public bool UnlocksFeature(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.UNLOCKS_FEATURE,
                2 => Chapter1Content.Stage2.UNLOCKS_FEATURE,
                5 => Chapter1Content.Stage5.UNLOCKS_FEATURE,
                _ => false
            };
        }
        
        public string GetNPCSpineName(int stageIndex)
        {
            return stageIndex switch
            {
                5 => "convo", // Ela uses "convo" spine
                _ => null // Others use default NPC spines
            };
        }

        public void ShowNPCDialogue(int stageIndex, Sprite npcAvatar = null)
        {
            if (dialogueManager == null) return;

            var dialogue = GetNPCDialogue(stageIndex);
            var name = GetNPCName(stageIndex);

            if (dialogue == null || dialogue.Length == 0) return;

            dialogueManager.OpenDialogue(name, npcAvatar, dialogue);
            OnNPCEncounter?.Invoke(stageIndex);
        }

        #endregion

        #region Story Stages

        public bool IsStoryStage(int stageIndex)
        {
            return stageIndex == 6; // Lost Courier Route
        }

        public string GetStoryEventDescription(int stageIndex)
        {
            if (stageIndex == 6)
                return Chapter1Content.Stage6.STORY_EVENT;
            return null;
        }

        public string[] GetStoryDialogue(int stageIndex)
        {
            if (stageIndex == 6)
                return Chapter1Content.Stage6.STORY_DIALOGUE;
            return null;
        }

        public void ShowStoryDialogue(int stageIndex, Sprite avatar = null)
        {
            if (dialogueManager == null || !IsStoryStage(stageIndex)) return;

            var dialogue = GetStoryDialogue(stageIndex);
            if (dialogue == null) return;

            dialogueManager.OpenDialogue("???", avatar, dialogue);
        }

        #endregion

        #region Chapter Progression

        public void OnStageComplete(int stageIndex)
        {
            if (stageIndex == 7 && !HasCompletedChapter1)
            {
                ShowChapter1OpenEnding();
                OnChapter1Complete?.Invoke();
            }
        }

        public void ShowChapter1OpenEnding()
        {
            if (dialogueManager == null) return;

            dialogueManager.OpenDialogue(
                Chapter1Content.OPEN_ENDING_SENDER,
                null,
                Chapter1Content.OPEN_ENDING_DIALOGUE
            );
        }

        public void ShowLetterFromHer()
        {
            if (dialogueManager == null) return;

            var letterLines = new string[]
            {
                $"[Received: {Chapter1Content.LETTER_TITLE}]",
                "---",
                Chapter1Content.LETTER_BODY,
                "---",
                $"[Gift found: {Chapter1Content.GIFT_DESCRIPTION}]"
            };

            dialogueManager.OpenDialogue("- E", null, letterLines);
        }

        #endregion

        #region Letter System

        public void MarkLetterRead(string letterId)
        {
            receivedLetters.Add(letterId);
        }

        public bool HasReadLetter(string letterId)
        {
            return receivedLetters.Contains(letterId);
        }

        public string GetLetterBody() => Chapter1Content.LETTER_BODY;
        public string GetLetterTitle() => Chapter1Content.LETTER_TITLE;
        public string GetGiftDescription() => Chapter1Content.GIFT_DESCRIPTION;

        #endregion

        #region Chapter Info

        public string GetChapterTitle() => Chapter1Content.CHAPTER_TITLE;
        public string GetChapterIntro() => Chapter1Content.CHAPTER_INTRO;
        public string GetChapterId() => Chapter1Content.CHAPTER_ID;

        #endregion
    }
}
