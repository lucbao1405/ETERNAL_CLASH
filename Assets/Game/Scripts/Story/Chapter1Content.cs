namespace EternalClash.Story
{
    public static class Chapter1Content
    {
        public const string CHAPTER_ID = "chapter_1";
        public const string CHAPTER_TITLE = "Dawn of the Plains";
        
        public const string CHAPTER_INTRO = 
            "The continent of Aethelgard is drowning in the 'Eternal Clash' — an endless, " +
            "bloody war between the human kingdom and the Monster Surge.\n\n" +
            "Every trade route has been severed. Amidst this chaos, " +
            "the Wandering Couriers have become the last hope for those left behind.";

        public static class Stage1
        {
            public const int INDEX = 1;
            public const string ID = "stage_01_first_route";
            public const string NAME = "First Route";
            public const string LOCATION = "The First Path";
            
            public const string DESCRIPTION = 
                "The most basic route out of the Plains. This place used to be peaceful, " +
                "but now it's become the territory of slimy Slimes.";

            public const string PRE_BATTLE = 
                "[Hero]: This road... I've walked it thousands of times as a child.\n\n" +
                "Now every step must be careful. Those blue Slimes look harmless, " +
                "but they can swallow a whole horse if you're not on guard.\n\n" +
                "Time to learn how to fight. Charge forward!";

            public const string POST_VICTORY = 
                "The Slimes have been pushed back. Their bodies melt into puddles of goo.\n\n" +
                "You gathered some Wood from the roadside rubble — your first material " +
                "for the long journey ahead.\n\n" +
                "A stocky man with soot-stained armor approaches from the smoke...";

            public const string TUTORIAL_FOCUS = "Attack, Movement, Charge";
            
            public static readonly string[] ENEMIES = { "Slime" };
            public static readonly string[] DROPS = { "Gold", "EXP", "Wood" };

            // NPC: Blacksmith Garen appears at END of Stage 1
            public const string NPC_NAME = "Garen the Blacksmith";
            public const string NPC_ROLE = "Blacksmith";
            
            public static readonly string[] NPC_DIALOGUE = {
                "Ho there! *cough* Still breathing, are we?",
                "Name's Garen. I run the forge back in town. Came out here looking for... well, scrap metal mostly.",
                "But looks like I found something more interesting — a live one fighting through this mess.",
                "You've got decent form for a rookie. Sloppy footwork on that last charge, but the intent was there.",
                "Listen close, kid. The road ahead? It gets worse before it gets better. Wolves, Goblins... things that won't melt when you hit 'em.",
                "Come find me at the forge when you make it back. I'll teach you how to not die on your second day.",
                "...And don't worry about payment. Consider it an investment in the next Courier who might carry MY letter someday."
            };
            
            public const string UNLOCK_REWARD = "Blacksmith Unlocked!";
            public const bool UNLOCKS_FEATURE = true;
            public const string FEATURE_NAME = "Forge";
        }

        public static class Stage2
        {
            public const int INDEX = 2;
            public const string ID = "stage_02_broken_grassland";
            public const string NAME = "Broken Grassland";
            public const string LOCATION = "Shattered Fields";
            
            public const string DESCRIPTION = 
                "This stretch has been ravaged by Wild Wolves. Grass torn apart, " +
                "animal bones scattered everywhere.";

            public const string PRE_BATTLE = 
                "[Hero]: The smell of blood... thick in the air... Wolves. They're stalking behind those rocks.\n\n" +
                "Unlike Slimes, Wolves attack fast and deal significant damage. " +
                "This is when you need to learn to protect yourself.\n\n" +
                "Use Shield at the right time to reduce damage!";

            public const string POST_VICTORY = 
                "The wolf pack has scattered. You got Wolf Hide — quality wolf pelt, " +
                "useful for crafting protective gear.\n\n" +
                "A faint glow flickers between the trees. Someone is watching...";

            public const string TUTORIAL_FOCUS = "Shield Timing";
            
            public static readonly string[] ENEMIES = { "Slime", "Wolf" };
            public static readonly string[] DROPS = { "Wood", "Wolf Hide" };

            // NPC: Witch Elara appears at END of Stage 2
            public const string NPC_NAME = "Elara";
            public const string NPC_ROLE = "Witch / Alchemist";
            
            public static readonly string[] NPC_DIALOGUE = {
                "*giggle* How fascinating. A Courier who doesn't run.",
                "I am Elara. And before you ask — no, I will not turn you into a frog. Unless you deserve it.",
                "I've been observing the monster patterns in this region. The Wolves here are agitated... something deeper in the forest is driving them outward.",
                "That something? Oh, you'll meet it soon enough. But perhaps not unprepared.",
                "If you bring me herbs during your travels — strange leaves, glowing mushrooms, anything unusual — I can brew potions that might keep you alive longer than expected.",
                "Not out of kindness, mind you. I'm simply... invested in seeing how far you'll go.",
                "...Run along now, little Courier. The road won't walk itself."
            };
            
            public const string UNLOCK_REWARD = "Potion Infusion Unlocked!";
            public const bool UNLOCKS_FEATURE = true;
            public const string FEATURE_NAME = "PotionInfusion";
        }

        public static class Stage3
        {
            public const int INDEX = 3;
            public const string ID = "stage_03_wild_path";
            public const string NAME = "Wild Path";
            public const string LOCATION = "Wilderness Trail";
            
            public const string DESCRIPTION = 
                "A narrow trail leading deeper into the plains. Fewer Slimes, but " +
                "Wolves are more numerous and far more aggressive.";

            public const string PRE_BATTLE = 
                "[Hero]: This was the traditional hunting ground of the local tribe.\n\n" +
                "Now it belongs to the wolf packs. Luckily they usually hunt alone here — " +
                "a good opportunity to farm materials.\n\n" +
                "Copper Ore sometimes appears in rock crevices. Gather as much as you can!";

            public const string POST_VICTORY = 
                "You gathered Copper Ore — crude but valuable copper ore. " +
                "With enough materials, you can take them to the forge for equipment upgrades.";

            public const string TUTORIAL_FOCUS = "Farm Materials";
            
            public static readonly string[] ENEMIES = { "Wolf" };
            public static readonly string[] DROPS = { "Copper Ore", "Wolf Hide" };
        }

        public static class Stage4
        {
            public const int INDEX = 4;
            public const string ID = "stage_04_abandoned_road";
            public const string NAME = "Abandoned Road";
            public const string LOCATION = "The Forgotten Road";
            
            public const string DESCRIPTION = 
                "An old main highway, now riddled with danger. " +
                "Goblin Archers have claimed this area as their territory.";

            public const string PRE_BATTLE = 
                "[Hero]: I hear bowstrings being drawn... Goblins!\n\n" +
                "They're different from the enemies before — they attack from range. " +
                "If you stand still, you'll become an easy target.\n\n" +
                "Combine Shield to block arrows, then Charge to close the distance!";

            public const string POST_VICTORY = 
                "The Goblin Archers have fled. You survived your first encounter " +
                "with ranged enemies.\n\n" +
                "Combo tactics are starting to take shape. The real test lies ahead.";

            public const string TUTORIAL_FOCUS = "Ranged Enemy + Combo";
            
            public static readonly string[] ENEMIES = { "Slime", "Wolf", "Goblin Archer" };
            public static readonly string[] DROPS = { "Wood", "Copper Ore" };
        }

        public static class Stage5
        {
            public const int INDEX = 5;
            public const string ID = "stage_05_goblin_territory";
            public const string NAME = "Goblin Territory";
            public const string LOCATION = "Goblin Lands";
            
            public const string DESCRIPTION = 
                "A Goblin checkpoint. More enemies, better organized, " +
                "and higher damage than previous stages.";

            public const string PRE_BATTLE = 
                "[Hero]: Ahead lies the entire Goblin force in this region.\n\n" +
                "They're organized — foot soldiers, archers, even commanders. " +
                "This isn't fighting wandering monsters anymore.\n\n" +
                "Make sure your equipment is upgraded. Use every skill you have!";

            public const string POST_VICTORY = 
                "Goblin Territory has been cleared. Smoke rises from broken encampments.\n\n" +
                "And then — amidst the chaos of battle — a familiar silhouette. Impossible. It can't be...\n\n" +
                "...It is.";

            public const string TUTORIAL_FOCUS = "Full Combat Test";
            
            public static readonly string[] ENEMIES = { "Goblin Archer", "Wolf" };
            public static readonly string[] DROPS = { "Wood", "Copper Ore", "Rare Material" };

            // NPC: HER - The childhood friend / love interest (spine: convo)
            public const string NPC_NAME = "Ela";
            public const string NPC_ROLE = "???";
            
            public static readonly string[] NPC_DIALOGUE = {
                "...",
                "You're real. You're actually here. I thought... I thought I was imagining it.",
                "How long has it been? The days blur together in the capital. Every knock on the door, I hope it's a Courier with news of you.",
                "And now here you are. Covered in Goblin blood, looking at me like I'm the ghost.",
                "I have so much to tell you. About the capital. About what's happening beyond the walls. About why the roads are really closed.",
                "But not now. Not here. This place isn't safe — even for someone who fought through all of THIS to reach me.",
                "...You fought through all of this to reach me, didn't you?",
                "...Thank you. For not giving up. For still being alive.",
                "Finish what you started here. Clear these lands. And when you truly make it to the capital...",
                "I'll be waiting. Like I promised. Like you promised.",
                "...Take this. Something to remember why you're fighting. Now go — before I forget how to say goodbye again."
            };
            
            public const string UNLOCK_REWARD = "Received Ela's Keepsake";
            public const bool UNLOCKS_FEATURE = false;
            public const string FEATURE_NAME = "";
        }

        public static class Stage6
        {
            public const int INDEX = 6;
            public const string ID = "stage_06_lost_courier_route";
            public const string NAME = "Lost Courier Route";
            public const string LOCATION = "The Lost Courier's Path";
            
            public const string DESCRIPTION = 
                "[STORY STAGE] Where a fellow Courier fell. Their undelivered letters " +
                "still rest in their bag.";

            public const string STORY_EVENT = 
                "Mid-battle, Hero discovers a corpse wearing a Courier's cloak " +
                "beneath an ancient tree. Their hand still clutches a leather satchel.\n\n" +
                "Inside are dozens of letters — from mothers to children, husbands to wives, " +
                "lovers waiting...\n\n" +
                "Words that never reached their destination.";

            public const string PRE_BATTLE = 
                "[Hero]: This satchel... it belongs to a colleague.\n\n" +
                "They tried to deliver these letters to their recipients. " +
                "But Eternal Clash waits for no one.\n\n" +
                "...I'll complete their mission. After dealing with these approaching monsters.";

            public const string POST_VICTORY = 
                "Hero picks up the leather satchel, carefully stowing it in their pack.\n\n" +
                "'Rest now. Those letters... I'll deliver them.'\n\n" +
                "The keepsake Ela gave you feels warm against your chest. Keep moving.";

            public const string TUTORIAL_FOCUS = "Story + Rare Drops";
            
            public static readonly string[] ENEMIES = { "Wolf", "Goblin Archer" };
            public static readonly string[] DROPS = { "Rare Material", "Wood" };
            
            public const bool IS_STORY_STAGE = true;

            public static readonly string[] STORY_DIALOGUE = {
                "...",
                "Another Courier. They fell here... how long ago?",
                "Their satchel of letters is still intact. Not a single letter delivered.",
                "'To my daughter in the capital... Papa is coming home...'",
                "'Dear wife... the war should end by spring...'",
                "...Words that never reached their destination.",
                "Ela is waiting. I can't afford to end up like this.",
                "I'll deliver these letters. Theirs... and mine."
            };
        }

        public static class Stage7
        {
            public const int INDEX = 7;
            public const string ID = "stage_07_monster_surge";
            public const string NAME = "Monster Surge Area";
            public const string LOCATION = "Surge Grounds";
            public const string DESCRIPTION = 
                "The Monster Surge's focal point in the Plains. " +
                "Every type of monster gathers here — the final test before leaving this region.";

            public const string PRE_BATTLE = 
                "[Hero]: The atmosphere is completely different. Danger pressing from every direction.\n\n" +
                "Slime, Wolves, Goblins — they're all here. This is the final exam " +
                "before entering the Dark Forest.\n\n" +
                "For her. Everything is for her. Fight.";

            public const string POST_VICTORY = 
                "You survived the Monster Surge! The Singing Plains are finally cleared.\n\n" +
                "The Dark Forest looms ahead — Orc territory, Poison Mushrooms, and worse.\n\n" +
                "But she's waiting. And you're one step closer.";

            public const string TUTORIAL_FOCUS = "Final Test - Chapter 1";
            
            public static readonly string[] ENEMIES = { "Slime", "Wolf", "Goblin Archer" };
            public static readonly string[] DROPS = { "Wood", "Copper Ore", "Rare Material" };
        }

        public static class Stage8
        {
            public const int INDEX = 8;
            public const string ID = "stage_08_surge_front";
            public const string NAME = "Surge Front";
            public const string LOCATION = "The Surge Front";

            public const string DESCRIPTION =
                "The deepest goblin push into the Plains. Wave after wave crashes " +
                "against the last Courier outpost before the Chieftain's arena.";

            public const string PRE_BATTLE =
                "[Hero]: The banners are torn, the outpost is burning — but the goblins keep coming.\n\n" +
                "Every Courier before me held this line. Today it's my turn.\n\n" +
                "Watch the archers — the waves come fast and don't stop.";

            public const string POST_VICTORY =
                "The assault breaks. The road to the Chieftain's arena lies open.\n\n" +
                "Whatever waits beyond that gate... you've earned the right to face it.";

            public const string TUTORIAL_FOCUS = "Endurance Test";

            public static readonly string[] ENEMIES = { "Slime", "Wolf", "Goblin Archer" };
            public static readonly string[] DROPS = { "Copper Ore", "Rare Material" };
        }

        public static class Stage9
        {
            public const int INDEX = 9;
            public const string ID = "stage_09_chieftain_arena";
            public const string NAME = "Chieftain's Arena";
            public const string LOCATION = "The Chieftain's Arena";

            public const string DESCRIPTION =
                "[BOSS STAGE] The goblin warlord's arena. His elite guard holds the " +
                "road; the Chieftain himself waits at its end.";

            public const string PRE_BATTLE =
                "[Hero]: His guard is elite — the strongest monsters I've faced yet.\n\n" +
                "And beyond them... the Chieftain. Twelve thousand strikes of steel, they say.\n\n" +
                "Ela is waiting at the capital. The Chieftain is in my way. There's nothing else to say.";

            public const string POST_VICTORY =
                "The Chieftain falls. The Plains are silent for the first time in years.\n\n" +
                "The road to the capital is open... and someone is watching from the tree line.";

            public const string TUTORIAL_FOCUS = "BOSS: Goblin Chieftain";

            public static readonly string[] ENEMIES = { "Slime", "Wolf", "Goblin Archer", "Boss" };
            public static readonly string[] DROPS = { "Rare Material", "Gold" };
        }

        // OPEN ENDING (After Stage 7)
        public const string OPEN_ENDING_SENDER = "???";
        
        public static readonly string[] OPEN_ENDING_DIALOGUE = {
            "...",
            "So. The Plains are cleared. Impressive for a Courier driven by love.",
            "I've watched your kind before. Most quit after the first Wolf bite. Some make it to the Goblins.",
            "But you? You have something they didn't. Something worth bleeding for.",
            "The Dark Forest awaits you, love-struck fool. Orc Chieftain rules there now. His tusk collection includes... well. You'll see.",
            "There IS another path. Faster. But dangerous in ways monsters can never be.",
            "She's waiting at the capital. Alive. I've seen her letters fly on crows too stubborn to die.",
            "Don't let her wait forever. Time is crueler than any Orc.",
            "[The figure steps back into shadow. A single wild daisy falls at your feet.]"
        };

        // LETTER FROM HER (Received during/after Chapter 1)
        public const string LETTER_TITLE = "Letter from Ela";
        
        public const string LETTER_BODY = 
            "Dear idiot,\n\n" +
            "If you're reading this, it means you're still alive. Good. Stay that way.\n\n" +
            "I hate that you're out there. I hate that every day I wonder if today's the day " +
            "a Courier knocks with news I don't want to hear.\n\n" +
            "But I also know you're the most stubborn person I've ever met. " +
            "If anyone can carve a path through that hellscape, it's you.\n\n" +
            "The wild daisy you sent last month. I pressed it between the pages of my book. " +
            "It's stupid, but sometimes I touch it just to remind myself you're real.\n\n" +
            "Come back to me. Whole. Preferably in fewer pieces than when you left.\n\n" +
            "I'm counting the days. Don't make me lose count.\n\n" +
            "- E\n\n" +
            "P.S. If you die out there, I'll never forgive you. So don't.";

        public const string GIFT_DESCRIPTION = "Pressed Wild Daisy — 'Touch it to remind yourself I'm real.'";
    }
}
