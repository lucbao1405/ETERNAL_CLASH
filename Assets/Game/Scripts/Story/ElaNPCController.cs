using System.Collections.Generic;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EternalClash.Core.Save;
using EternalClash.Data;
using EternalClash.Dialogue;
using EternalClash.UI;

namespace EternalClash.Story
{
    /// <summary>
    /// Ela — romance NPC living at the first house (Page_1) in Town, unlocked
    /// after Stage 5. Click her house to talk: greeting dialogue, then a menu
    /// with Talk (topics), Gift (bag in gift mode) and Receive (her gifts).
    /// Everything is built at runtime; the "convo" spine is optional and is
    /// used as her visual when present under a Resources folder.
    /// </summary>
    public class ElaNPCController : MonoBehaviour
    {
        public static ElaNPCController Instance { get; private set; }

        // stageLevel is the stage you are ABOUT to play: clearing Stage 5 (where
        // Ela is met on the field) sets stageLevel to 6.
        private const int UnlockStage = 6;
        private const string ConvoSkeletonPath = "convo"; // Resources path of the convo SkeletonDataAsset

        [System.Serializable]
        private class Topic
        {
            public string id, title;
            public int affinityReq;
            public bool oneTime;
            public string[] lines;
            public Topic(string id, string title, int affinityReq, bool oneTime, string[] lines)
            {
                this.id = id; this.title = title; this.affinityReq = affinityReq;
                this.oneTime = oneTime; this.lines = lines;
            }
        }

        [System.Serializable]
        private class GiftReaction
        {
            public string itemId;
            public string[] lines;
            public int affinity;
            public GiftReaction(string itemId, int affinity, params string[] lines)
            {
                this.itemId = itemId; this.affinity = affinity; this.lines = lines;
            }
        }

        [System.Serializable]
        private class HerGift
        {
            public string itemId, label;
            public int affinityReq;
            public string[] lines;
            public HerGift(string itemId, string label, int affinityReq, string[] lines)
            {
                this.itemId = itemId; this.label = label; this.affinityReq = affinityReq; this.lines = lines;
            }
        }

        // ------------------------------------------------------------------ content

        private static readonly string[] Greetings =
        {
            "You came back. I was hoping you would.",
            "Every time I see a silhouette on the road, I wonder... is it you this time?",
            "Still alive, I see. Good. Keep that up.",
            "I was just thinking about you. Don't let it go to your head.",
            "Welcome home, Courier. Well... almost home."
        };

        private static readonly string[] NothingToReceive =
        {
            "I don't have anything for you right now...",
            "But keep the letters coming. And maybe... bring me something from the road?"
        };

        private static readonly Topic[] Topics =
        {
            new Topic("topic_past", "About Us", 0, false, new[]
            {
                "Do you remember the hill behind our old house?",
                "We used to watch the sunset from there. You said you'd take me to the capital someday.",
                "Look at us now. You're actually doing it. One bloody step at a time.",
                "...I'm proud of you. Even if you're an idiot for choosing this path."
            }),
            new Topic("topic_capital", "The Capital", 50, false, new[]
            {
                "The capital... it's not what people imagine anymore.",
                "The walls still stand. But inside... things are complicated.",
                "Food is rationed. The gates barely open. And the monsters get closer every month.",
                "When you reach me there — and you WILL reach me — I'll explain everything."
            }),
            new Topic("topic_letter", "Your Letters", 100, true, new[]
            {
                "I kept every letter you sent.",
                "The first one was stained with Slime goo. Disgusting. I loved it.",
                "The wolf pelt one smelled terrible for weeks.",
                "And that pressed flower... it's in my favorite book now.",
                "Don't stop sending them. They're the only proof you're still alive out there."
            }),
            new Topic("topic_future", "After The War", 300, false, new[]
            {
                "Sometimes I imagine it. The war ending. The roads opening again.",
                "What would we do? Find a quiet place somewhere? Open a shop?",
                "You'd probably insist on being a Courier forever. 'It's in my blood,' you'd say.",
                "...Idiot. As long as it's with you, I don't care what we do."
            }),
            new Topic("topic_deep", "Something Personal", 500, true, new[]
            {
                "...",
                "I never told you this. Before you left, I almost asked you to stay.",
                "But I knew you wouldn't. Not when you heard about the roads being cut off.",
                "'Someone has to do something,' you said. With that stupid determined look.",
                "That's when I knew. I was going to wait for you. However long it takes.",
                "...You're worth waiting for. Don't make me regret saying that."
            })
        };

        private static readonly GiftReaction[] Reactions =
        {
            new GiftReaction("yellow_wildflower", 15,
                "A wildflower! Just like the ones from back home...",
                "*laughs softly* You remembered. Of course you did.",
                "I'll press this one too. Put it right next to the others.",
                "Thank you. Really."),
            new GiftReaction("blue_flower", 20,
                "Blue flowers... these only grow near water sources, don't they?",
                "You went out of your way to find this. For me.",
                "It's beautiful. Almost as beautiful as the day we first met—",
                "...Forget I said that last part."),
            new GiftReaction("leaf_green", 8,
                "A green leaf? Fresh from the plains?",
                "Simple. But coming from you, even a leaf means something.",
                "I'll add it to my collection. Keep them coming."),
            new GiftReaction("leaf_red", 8,
                "Red leaf. Autumn colors already?",
                "The seasons pass so fast when you're not here...",
                "Thank you. It reminds me to be patient."),
            new GiftReaction("leaf_yellow", 5,
                "Yellow leaf. Bright and warm, like sunlight.",
                "It's lovely. Thank you for thinking of me."),
            new GiftReaction("wolf_hide", 12,
                "Wolf hide?! It's so soft!",
                "...Did you have to fight a whole pack for this?",
                "Please tell me you didn't almost die for a piece of fur.",
                "...You did, didn't you? *sigh* Just... stay alive, okay?")
        };

        private static readonly string[] DefaultReaction =
        {
            "...This? For me?",
            "It's... not really my thing, but thank you for thinking of me.",
            "(Maybe try one of the flowers or leaves next time.)"
        };

        private static readonly HerGift[] HerGifts =
        {
            new HerGift("wood_small", "Hand-carved Whistle", 50, new[]
            {
                "Wait — before you go.",
                "I made something. It's nothing special, but...",
                "A whistle. From the same wood you brought me last time.",
                "If you're ever in trouble... blow it. I won't hear it, but maybe you'll remember I'm waiting.",
                "...Take it. And come back."
            }),
            new HerGift("blue_flower", "Pressed Flower Bookmark", 200, new[]
            {
                "I've been meaning to give you this.",
                "A bookmark. For all those letters you write me on the road.",
                "The flower... it's from the window box. The one that reminded me of you.",
                "Think of it as a promise. I'll keep your place. Always.",
                "Now go. Your story isn't finished yet."
            }),
            new HerGift("copper_ore", "Copper Ring", 500, new[]
            {
                "...Don't read too much into this, okay?",
                "It's just copper. Found it in the old market district.",
                "But I thought... every time you look at your hand, you could remember—",
                "—that someone's waiting. That you have a reason to come back.",
                "So. Take it. Or don't. I don't care.\n(I care so much.)"
            })
        };

        // ------------------------------------------------------------------ state

        private DialogueManager dialogueManager;
        private GameObject menuPanel;
        private GameObject topicsPanel;
        private TMP_Text menuTitle;
        private TMP_Text affinityText;
        private Transform topicButtonContainer;

        // Shared with the baked scene UI: Assets/Game/Resources/UI/Ela/button.png
        private static Sprite menuButtonSprite;

        // The Bag panel starts inactive; GameObject.Find/FindObjectOfType cannot
        // see it, so resolve it once through Transform.Find (inactive-safe).
        private GameObject bagPanel;
        private SmoothSlide bagSlide;
        private BagController[] bagControllers;

        private int ElaAffinity;
        private readonly HashSet<string> topicsDone = new HashSet<string>();
        private readonly HashSet<string> giftsReceived = new HashSet<string>();

        private bool IsUnlocked =>
            SaveManager.Instance?.Data != null && SaveManager.Instance.Data.stageLevel >= UnlockStage;

        // ------------------------------------------------------------------ bootstrap

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

            // Play mode can start directly in Town — the sceneLoaded event for the
            // first scene has already fired by the time this hook runs.
            EnsureInTown();
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
            UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            EnsureInTown();
        }

        private static void EnsureInTown()
        {
            UnityEngine.SceneManagement.Scene scene =
                UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            if (!scene.IsValid() ||
                !string.Equals(scene.name, "Town", System.StringComparison.OrdinalIgnoreCase))
                return;

            if (FindObjectOfType<ElaNPCController>() != null)
                return;

            new GameObject("ElaNPC (Runtime)").AddComponent<ElaNPCController>();
        }

        private void Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            dialogueManager = FindObjectOfType<DialogueManager>();
            LoadProgress();
            ResolveBagPanel();
            BuildNpcInTown();
            BuildMenuUI();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void LoadProgress()
        {
            SaveData data = SaveManager.Instance?.Data;
            if (data == null) return;

            ElaAffinity = data.elaAffinity;
            topicsDone.Clear();
            if (data.elaTopicsDone != null)
                foreach (var t in data.elaTopicsDone) topicsDone.Add(t);
            giftsReceived.Clear();
            if (data.elaGiftsReceived != null)
                foreach (var g in data.elaGiftsReceived) giftsReceived.Add(g);
        }

        private void SyncSave()
        {
            SaveData data = SaveManager.Instance?.Data;
            if (data == null) return;

            data.elaAffinity = ElaAffinity;
            data.elaTopicsDone ??= new List<string>();
            data.elaTopicsDone.Clear();
            data.elaTopicsDone.AddRange(topicsDone);
            data.elaGiftsReceived ??= new List<string>();
            data.elaGiftsReceived.Clear();
            data.elaGiftsReceived.AddRange(giftsReceived);
            SaveCoordinator.RequestSave();
        }

        // ------------------------------------------------------------------ town npc

        private void BuildNpcInTown()
        {
            GameObject content = GameObject.Find("Canvas/Up_Panel/Background/Viewport/Content");
            Transform page1 = content != null ? content.transform.Find("Page_1") : null;
            Transform house = page1 != null ? page1.Find("nha") : null;
            if (page1 == null || house == null)
            {
                Debug.LogWarning("[ElaNPC] Page_1/nha not found; Ela NPC not placed.");
                return;
            }

            // Uu tien dung NPC da dat thu cong trong scene (Page_1/Ela_ClickArea).
            Transform bakedArea = page1.Find("Ela_ClickArea");
            if (bakedArea != null)
            {
                Button bakedButton = bakedArea.GetComponent<Button>();
                if (bakedButton == null)
                {
                    bakedButton = bakedArea.gameObject.AddComponent<Button>();
                    bakedButton.transition = Selectable.Transition.None;
                }
                bakedButton.onClick.RemoveListener(OnElaClicked);
                bakedButton.onClick.AddListener(OnElaClicked);

                // Truoc khi mo khoa (stageLevel < 6) Ela van an, giong nhu
                // truoc day khong tao NPC — chi la object nam san trong scene.
                bakedArea.gameObject.SetActive(IsUnlocked);

                Transform bakedSkeleton = page1.Find("Ela");
                if (bakedSkeleton != null)
                {
                    bakedSkeleton.gameObject.SetActive(IsUnlocked);
                    FaceLeft(bakedSkeleton.GetComponent<SkeletonGraphic>());
                }

                Debug.Log("[ElaNPC] Dung object Ela da dat trong scene, unlocked=" + IsUnlocked);
                return;
            }

            // Before the field meeting there is no Ela here at all — the house is
            // just scenery. She only moves in after you clear her stage.
            if (!IsUnlocked)
                return;

            TrySpawnConvoSkeleton(page1, house);

            // Transparent click area over the house — works with or without the spine.
            RectTransform houseRect = house as RectTransform;
            GameObject area = new GameObject("Ela_ClickArea",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            area.transform.SetParent(page1, false);
            RectTransform rect = area.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.anchorMin = houseRect.anchorMin;
            rect.anchorMax = houseRect.anchorMax;
            rect.pivot = houseRect.pivot;
            rect.anchoredPosition = houseRect.anchoredPosition;
            rect.sizeDelta = houseRect.sizeDelta * 0.95f;
            rect.SetAsLastSibling();

            Image image = area.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            Button button = area.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            button.onClick.AddListener(OnElaClicked);

            Debug.Log("[ElaNPC] Click area placed over the first house.");
        }

        /// <summary>Mirrors the spine so Ela looks toward the approaching hero.</summary>
        private static void FaceLeft(SkeletonGraphic skeleton)
        {
            if (skeleton == null) return;
            // Skeleton.ScaleX does not survive SkeletonGraphic's mesh rebuild;
            // mirror the RectTransform instead (same trick the field meeting
            // uses on SkeletonAnimation via localScale).
            RectTransform rect = skeleton.rectTransform != null
                ? skeleton.rectTransform
                : skeleton.transform as RectTransform;
            if (rect == null) return;
            Vector3 scale = rect.localScale;
            scale.x = -Mathf.Abs(scale.x);
            rect.localScale = scale;
        }

        private void TrySpawnConvoSkeleton(Transform page1, Transform house)
        {
            // Skeleton da duoc dat thu cong trong scene thi chi dieu khien an/hien.
            Transform existing = page1.Find("Ela");
            if (existing != null)
            {
                existing.gameObject.SetActive(IsUnlocked);
                FaceLeft(existing.GetComponent<SkeletonGraphic>());
                return;
            }

            SkeletonDataAsset skeletonData = Resources.Load<SkeletonDataAsset>(ConvoSkeletonPath);
            if (skeletonData == null)
                return; // Spine not added yet — click area alone is enough.

            GameObject elaGo = new GameObject("Ela", typeof(RectTransform), typeof(CanvasRenderer));
            elaGo.transform.SetParent(page1, false);
            elaGo.transform.SetSiblingIndex(house.GetSiblingIndex() + 1);

            SkeletonGraphic skeleton = elaGo.AddComponent<SkeletonGraphic>();
            skeleton.skeletonDataAsset = skeletonData;
            skeleton.Initialize(true);

            Spine.SkeletonData data = skeletonData.GetSkeletonData(true);
            RectTransform rect = elaGo.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            if (data != null && data.Height > 0f)
            {
                float scale = 420f / data.Height; // fit her to roughly house-door height
                rect.localScale = new Vector3(scale, scale, 1f);
            }
            rect.anchoredPosition = new Vector2(-409f, -480f); // in front of the house door

            Debug.Log("[ElaNPC] Convo skeleton spawned at the first house.");
        }

        // ------------------------------------------------------------------ interaction

        public void OnElaClicked()
        {
            if (dialogueManager == null || dialogueManager.IsOpen)
                return;

            RefreshMenuTitle();
            dialogueManager.OpenDialogue("Ela", null,
                new[] { Greetings[Random.Range(0, Greetings.Length)] }, menuPanel);
        }

        // ------------------------------------------------------------------ menu ui

        private void BuildMenuUI()
        {
            Transform screens = GameObject.Find("Canvas/Man_Hinh_Khac")?.transform;
            if (screens == null)
            {
                Debug.LogWarning("[ElaNPC] Man_Hinh_Khac not found; menu UI not built.");
                return;
            }

            // Menu da duoc dat thu cong trong scene (Man_Hinh_Khac/ElaMenu + ElaTopics):
            // chi wire nut bam, khong build lai.
            Transform bakedMenu = screens.Find("ElaMenu");
            Transform bakedTopics = screens.Find("ElaTopics");
            if (bakedMenu != null && bakedTopics != null)
            {
                menuPanel = bakedMenu.gameObject;
                topicsPanel = bakedTopics.gameObject;

                menuTitle = bakedMenu.Find("KhungName/Ela")?.GetComponent<TMP_Text>();
                menuTitle ??= bakedMenu.GetComponentInChildren<TMP_Text>(true);
                affinityText = bakedMenu.Find("Text")?.GetComponent<TMP_Text>();

                BindBakedButton(bakedMenu, "Btn_Talk", OnTalkClicked);
                BindBakedButton(bakedMenu, "Btn_Give a Gift", OnGiftClicked);
                BindBakedButton(bakedMenu, "Btn_Receive", OnReceiveClicked);
                BindBakedButton(bakedMenu, "Btn_Close", () => menuPanel.SetActive(false));
                BindBakedButton(bakedTopics, "Btn_Back",
                    () => { topicsPanel.SetActive(false); menuPanel.SetActive(true); });

                topicButtonContainer = bakedTopics.Find("TopicList");
                topicButtonContainer ??= bakedTopics;

                // Android Back closes the panels like every other popup.
                if (menuPanel.GetComponent<BackClosePanel>() == null)
                    menuPanel.AddComponent<BackClosePanel>();
                if (topicsPanel.GetComponent<BackClosePanel>() == null)
                    topicsPanel.AddComponent<BackClosePanel>();

                menuPanel.SetActive(false);
                topicsPanel.SetActive(false);
                Debug.Log("[ElaNPC] Menu UI wired from baked hierarchy.");
                return;
            }

            TMP_Text fontSource = null;
            TMP_Text[] texts = dialogueManager != null
                ? dialogueManager.GetComponentsInChildren<TMP_Text>(true)
                : null;
            if (texts != null && texts.Length > 0) fontSource = texts[texts.Length - 1];

            menuPanel = BuildPanel(screens, "ElaMenu", 660f, 640f);
            menuTitle = BuildText(menuPanel.transform, "Ela", 52f, new Vector2(0f, 245f), fontSource);
            affinityText = BuildText(menuPanel.transform, "", 28f, new Vector2(0f, 172f), fontSource);
            affinityText.color = new Color(0.42f, 0.30f, 0.18f);

            BuildButton(menuPanel.transform, "Talk", new Vector2(0f, 60f), fontSource, OnTalkClicked);
            BuildButton(menuPanel.transform, "Give a Gift", new Vector2(0f, -50f), fontSource, OnGiftClicked);
            BuildButton(menuPanel.transform, "Receive", new Vector2(0f, -160f), fontSource, OnReceiveClicked);
            BuildButton(menuPanel.transform, "Close", new Vector2(0f, -270f), fontSource, () => menuPanel.SetActive(false));
            menuPanel.AddComponent<BackClosePanel>();
            menuPanel.SetActive(false);

            topicsPanel = BuildPanel(screens, "ElaTopics", 640f, 900f);
            BuildText(topicsPanel.transform, "Talk about...", 52f, new Vector2(0f, 360f), fontSource);
            topicButtonContainer = BuildVerticalLayout(topicsPanel.transform, new Vector2(0f, 30f), new Vector2(520f, 560f));
            BuildButton(topicsPanel.transform, "Back", new Vector2(0f, -380f), fontSource,
                () => { topicsPanel.SetActive(false); menuPanel.SetActive(true); });
            topicsPanel.AddComponent<BackClosePanel>();
            topicsPanel.SetActive(false);

            Debug.Log("[ElaNPC] Menu UI built.");
        }

        private void OnTalkClicked()
        {
            menuPanel.SetActive(false);
            RebuildTopicButtons();
            topicsPanel.SetActive(true);
        }

        private void RebuildTopicButtons()
        {
            TMP_Text fontSource = menuTitle;
            for (int i = topicButtonContainer.childCount - 1; i >= 0; i--)
                Destroy(topicButtonContainer.GetChild(i).gameObject);

            float y = 0f;
            foreach (Topic topic in Topics)
            {
                if (topic.affinityReq > ElaAffinity) continue;
                if (topic.oneTime && topicsDone.Contains(topic.id)) continue;

                string label = topic.oneTime && !topicsDone.Contains(topic.id)
                    ? topic.title + "  (new)"
                    : topic.title;
                string topicId = topic.id;
                GameObject buttonObj = BuildButton(topicButtonContainer, label, new Vector2(0f, y), fontSource,
                    () => SelectTopic(topicId));
                ((RectTransform)buttonObj.transform).anchoredPosition = new Vector2(0f, y);
                y -= 120f;
            }
        }

        private void SelectTopic(string topicId)
        {
            Topic topic = System.Array.Find(Topics, t => t.id == topicId);
            if (topic == null || dialogueManager == null) return;

            topicsPanel.SetActive(false);

            bool firstTime = topic.oneTime && !topicsDone.Contains(topic.id);
            if (firstTime)
            {
                topicsDone.Add(topicId);
                AddAffinity(5);
            }

            // Menu reopens automatically when the dialogue finishes.
            dialogueManager.OpenDialogue("Ela", null, topic.lines, menuPanel);
        }

        private void ResolveBagPanel()
        {
            Transform screens = GameObject.Find("Canvas/Man_Hinh_Khac")?.transform;
            Transform bag = screens != null ? screens.Find("Bag") : null;
            if (bag == null)
            {
                Debug.LogWarning("[ElaNPC] Bag panel not found under Man_Hinh_Khac.");
                return;
            }

            bagPanel = bag.gameObject;
            bagSlide = bag.GetComponent<SmoothSlide>();
            bagControllers = bag.GetComponents<BagController>();
        }

        private void OnGiftClicked()
        {
            menuPanel.SetActive(false);

            if (bagPanel == null)
                ResolveBagPanel();

            if (bagPanel == null)
            {
                dialogueManager?.OpenDialogue("Ela", null,
                    new[] { "...You seem distracted. Your bag is missing?" });
                return;
            }

            foreach (BagController bag in bagControllers)
                bag.SetGiftMode(OnGiftPicked);

            if (bagSlide != null)
                bagSlide.OpenPanel();
        }

        private void OnGiftPicked(ItemData item)
        {
            if (item == null) return;

            // The scene historically carries two BagController components on the
            // Bag panel; use only the first or every removal would happen twice.
            bool removed = false;
            if (bagControllers != null && bagControllers.Length > 0 && bagControllers[0] != null)
                removed = bagControllers[0].RemoveItem(item.itemId, 1);

            if (bagSlide != null)
                bagSlide.ClosePanel();

            if (!removed) return;

            GiftReaction reaction = System.Array.Find(Reactions, r => r.itemId == item.itemId);
            string[] lines = reaction != null ? reaction.lines : DefaultReaction;
            AddAffinity(reaction?.affinity ?? 3);
            dialogueManager?.OpenDialogue("Ela", null, lines);
        }

        private void OnReceiveClicked()
        {
            HerGift gift = System.Array.Find(HerGifts,
                g => g.affinityReq <= ElaAffinity && !giftsReceived.Contains(g.itemId + "_" + g.label));

            if (gift == null)
            {
                dialogueManager?.OpenDialogue("Ela", null, NothingToReceive);
                return;
            }

            giftsReceived.Add(gift.itemId + "_" + gift.label);

            if (bagControllers == null || bagControllers.Length == 0 || bagControllers[0] == null)
                ResolveBagPanel();
            if (bagControllers != null && bagControllers.Length > 0 && bagControllers[0] != null)
                bagControllers[0].AddItem(gift.itemId, 1);

            AddAffinity(5);
            dialogueManager?.OpenDialogue("Ela", null, gift.lines);
        }

        private void AddAffinity(int amount)
        {
            if (amount <= 0) return;
            ElaAffinity += amount;
            SyncSave();
            RefreshMenuTitle();
        }

        private void RefreshMenuTitle()
        {
            if (menuTitle != null)
                menuTitle.text = "Ela";
            if (affinityText != null)
                affinityText.text = AffinityTitle(ElaAffinity) + "  —  Affinity " + ElaAffinity;
        }

        private static string AffinityTitle(int a)
        {
            if (a < 50) return "Stranger";
            if (a < 150) return "Acquaintance";
            if (a < 300) return "Friend";
            if (a < 500) return "Close Friend";
            if (a < 800) return "Something More...";
            return "Soulbound";
        }

        // ------------------------------------------------------------------ ui helpers

        private static void BindBakedButton(Transform root, string childName, System.Action onClick)
        {
            Button button = root.Find(childName)?.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning("[ElaNPC] Baked button not found: " + childName);
                return;
            }
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }

        private static GameObject BuildPanel(Transform parent, string name, float width, float height)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.transform.SetAsLastSibling();

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.14f, 0.10f, 0.07f, 0.97f);

            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.62f, 0.25f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);

            return panel;
        }

        private static TMP_Text BuildText(Transform parent, string content, float size, Vector2 pos, TMP_Text fontSource)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);

            TMP_Text text = go.AddComponent<TextMeshProUGUI>();
            if (fontSource != null) text.font = fontSource.font;
            text.text = content;
            text.fontSize = size;
            text.color = new Color(0.96f, 0.92f, 0.85f);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            RectTransform rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(560f, size * 1.6f);
            rect.anchoredPosition = pos;
            return text;
        }

        private static GameObject BuildButton(Transform parent, string label, Vector2 pos,
            TMP_Text fontSource, System.Action onClick)
        {
            GameObject go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(440f, 96f);
            rect.anchoredPosition = pos;

            Image image = go.GetComponent<Image>();
            if (menuButtonSprite == null)
                menuButtonSprite = Resources.Load<Sprite>("UI/Ela/button");
            if (menuButtonSprite != null)
            {
                image.sprite = menuButtonSprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.93f, 0.52f, 0.10f, 1f);
            }

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            TMP_Text text = BuildText(go.transform, label, 40f, Vector2.zero, fontSource);
            text.rectTransform.sizeDelta = rect.sizeDelta;

            return go;
        }

        private static Transform BuildVerticalLayout(Transform parent, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject("TopicList", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            VerticalLayoutGroup layout = go.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 24f;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = false;
            layout.childControlWidth = false;

            return go.transform;
        }
    }
}
