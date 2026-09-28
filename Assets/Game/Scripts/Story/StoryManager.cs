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
        public const int ElaUnlockStage = 4; // stageLevel after clearing Stage 3 (Ela)

        public static bool HasCompletedChapter1 => SaveManager.Instance?.Data?.stageLevel > 5;

        #region Stage Lore

        public static string GetStageName(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.NAME,
                2 => Chapter1Content.Stage2.NAME,
                3 => Chapter1Content.Stage3.NAME,
                4 => Chapter1Content.Stage4.NAME,
                5 => Chapter1Content.Stage9.NAME, // boss stage
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
                5 => Chapter1Content.Stage9.LOCATION, // boss stage
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
                5 => Chapter1Content.Stage9.DESCRIPTION, // boss stage
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
                5 => Chapter1Content.Stage9.PRE_BATTLE, // boss stage
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
                5 => Chapter1Content.Stage9.POST_VICTORY, // boss stage
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
                5 => Chapter1Content.Stage9.ENEMIES, // boss stage
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
                5 => Chapter1Content.Stage9.DROPS, // boss stage
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
                3 => true, // Ela
                _ => false
            };
        }

        public static string GetNPCName(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.NPC_NAME,
                2 => Chapter1Content.Stage2.NPC_NAME,
                3 => Chapter1Content.Stage5.NPC_NAME, // Ela
                _ => null
            };
        }

        public static string GetNPCRole(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.NPC_ROLE,
                2 => Chapter1Content.Stage2.NPC_ROLE,
                3 => Chapter1Content.Stage5.NPC_ROLE, // Ela
                _ => null
            };
        }

        public static string[] GetNPCDialogue(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.NPC_DIALOGUE,
                2 => Chapter1Content.Stage2.NPC_DIALOGUE,
                3 => Chapter1Content.Stage5.NPC_DIALOGUE, // Ela
                _ => null
            };
        }

        public static string GetUnlockReward(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.UNLOCK_REWARD,
                2 => Chapter1Content.Stage2.UNLOCK_REWARD,
                3 => Chapter1Content.Stage5.UNLOCK_REWARD, // Ela
                _ => null
            };
        }

        public static bool UnlocksFeature(int stageIndex)
        {
            return stageIndex switch
            {
                1 => Chapter1Content.Stage1.UNLOCKS_FEATURE,
                2 => Chapter1Content.Stage2.UNLOCKS_FEATURE,
                3 => Chapter1Content.Stage5.UNLOCKS_FEATURE, // Ela
                _ => false
            };
        }

        public static string GetNPCSpineName(int stageIndex)
        {
            return stageIndex == 3 ? "con vo" : null;
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
                Chapter1Content.LETTER_PAGE_1,
                Chapter1Content.LETTER_PAGE_2,
                Chapter1Content.LETTER_PAGE_3,
                Chapter1Content.LETTER_PAGE_4,
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
