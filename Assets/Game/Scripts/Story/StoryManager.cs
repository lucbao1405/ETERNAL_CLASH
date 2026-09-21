using UnityEngine;
using EternalClash.Core.Save;

namespace EternalClash.Story
{
    /// <summary>
    /// Chapter 1 story content + lookup helpers. Static on purpose: it holds no
    /// runtime state, so every scene (Town, Battle) can query it directly.
    /// </summary>
    public static class StoryManager
    {
        public const int ElaUnlockStage = 6; // stageLevel after clearing Stage 5

        public static bool HasCompletedChapter1 => SaveManager.Instance?.Data?.stageLevel > 9;

        #region Stage Lore

        public static string GetStageName(int stageIndex)
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
                8 => Chapter1Content.Stage8.NAME,
                9 => Chapter1Content.Stage9.NAME,
                _ => $"Stage {stageIndex}"
            };
        }

        public static string GetStageLocation(int stageIndex)
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
                8 => Chapter1Content.Stage8.LOCATION,
                9 => Chapter1Content.Stage9.LOCATION,
                _ => "Unknown"
            };
        }

        public static string GetStageDescription(int stageIndex)
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
                8 => Chapter1Content.Stage8.DESCRIPTION,
                9 => Chapter1Content.Stage9.DESCRIPTION,
                _ => ""
            };
        }

        public static string GetPreBattleNarration(int stageIndex)
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
                8 => Chapter1Content.Stage8.PRE_BATTLE,
                9 => Chapter1Content.Stage9.PRE_BATTLE,
                _ => null
            };
        }

        public static string GetPostVictoryNarration(int stageIndex)
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
                8 => Chapter1Content.Stage8.POST_VICTORY,
                9 => Chapter1Content.Stage9.POST_VICTORY,
                _ => null
            };
        }

        public static string[] GetStageEnemies(int stageIndex)
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
                8 => Chapter1Content.Stage8.ENEMIES,
                9 => Chapter1Content.Stage9.ENEMIES,
                _ => new string[0]
            };
        }

        public static string[] GetStageDrops(int stageIndex)
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
                8 => Chapter1Content.Stage8.DROPS,
                9 => Chapter1Content.Stage9.DROPS,
                _ => new string[0]
            };
        }

        public static string GetTutorialFocus(int stageIndex)
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
                8 => Chapter1Content.Stage8.TUTORIAL_FOCUS,
                9 => Chapter1Content.Stage9.TUTORIAL_FOCUS,
                _ => ""
            };
        }

        #endregion

        #region Field NPC Encounters (end-of-map meetings)

        public static bool HasNPCEncounter(int stageIndex)
        {
            return stageIndex switch
            {
                1 => true, // Garen the Blacksmith
                2 => true, // Elara the Witch
                5 => true, // Ela
                _ => false
            };
        }

        public static string GetNPCName(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.NPC_NAME,
                2 => Chapter1Content.Stage2.NPC_NAME,
                5 => Chapter1Content.Stage5.NPC_NAME,
                _ => null
            };
        }

        public static string GetNPCRole(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.NPC_ROLE,
                2 => Chapter1Content.Stage2.NPC_ROLE,
                5 => Chapter1Content.Stage5.NPC_ROLE,
                _ => null
            };
        }

        public static string[] GetNPCDialogue(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.NPC_DIALOGUE,
                2 => Chapter1Content.Stage2.NPC_DIALOGUE,
                5 => Chapter1Content.Stage5.NPC_DIALOGUE,
                _ => null
            };
        }

        public static string GetUnlockReward(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.UNLOCK_REWARD,
                2 => Chapter1Content.Stage2.UNLOCK_REWARD,
                5 => Chapter1Content.Stage5.UNLOCK_REWARD,
                _ => null
            };
        }

        public static bool UnlocksFeature(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.UNLOCKS_FEATURE,
                2 => Chapter1Content.Stage2.UNLOCKS_FEATURE,
                5 => Chapter1Content.Stage5.UNLOCKS_FEATURE,
                _ => false
            };
        }

        public static string GetNPCSpineName(int stageIndex)
        {
            return stageIndex == 5 ? "con vo" : null;
        }

        #endregion

        #region Story Stages

        public static bool IsStoryStage(int stageIndex) => stageIndex == 6;

        public static string GetStoryEventDescription(int stageIndex)
        {
            return stageIndex == 6 ? Chapter1Content.Stage6.STORY_EVENT : null;
        }

        public static string[] GetStoryDialogue(int stageIndex)
        {
            return stageIndex == 6 ? Chapter1Content.Stage6.STORY_DIALOGUE : null;
        }

        #endregion

        #region Chapter Ending

        public static void ShowChapter1OpenEnding(DialogueManager dialogueManager)
        {
            if (dialogueManager == null) return;

            dialogueManager.OpenDialogue(
                Chapter1Content.OPEN_ENDING_SENDER,
                null,
                Chapter1Content.OPEN_ENDING_DIALOGUE
            );
        }

        public static void ShowLetterFromHer(DialogueManager dialogueManager)
        {
            if (dialogueManager == null) return;

            dialogueManager.OpenDialogue("- E", null, new[]
            {
                $"[Received: {Chapter1Content.LETTER_TITLE}]",
                "---",
                Chapter1Content.LETTER_BODY,
                "---",
                $"[Gift found: {Chapter1Content.GIFT_DESCRIPTION}]"
            });
        }

        public static string GetLetterBody() => Chapter1Content.LETTER_BODY;
        public static string GetLetterTitle() => Chapter1Content.LETTER_TITLE;
        public static string GetGiftDescription() => Chapter1Content.GIFT_DESCRIPTION;

        public static string GetChapterTitle() => Chapter1Content.CHAPTER_TITLE;
        public static string GetChapterIntro() => Chapter1Content.CHAPTER_INTRO;
        public static string GetChapterId() => Chapter1Content.CHAPTER_ID;

        #endregion
    }
}
