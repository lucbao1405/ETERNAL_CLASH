using System;
using System.Collections;
using EternalClash.Core.Save;
using EternalClash.Stage;
using EternalClash.UI;
using EternalClash.Village;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EternalClash.Tutorial
{
    public enum TutorialStep
    {
        Introduction,
        AwaitingName,
        FirstBattlePrompt,
        BattleUnlocked,
        BattleCompleted,
        BlacksmithIntroduction,
        SelectIronSword,
        UpgradeIronSword,
        AfterUpgrade,
        BagUnlocked,
        Completed
    }

    /// <summary>
    /// Coordinates the first-game sequence through existing systems. It owns only
    /// tutorial state and UI guidance; battle, equipment, rewards, and upgrades stay
    /// in their current implementations.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialManager : MonoBehaviour
    {
        private const string TownSceneName = "Town";
        private const string BattleSceneName = "Battle";

        public static TutorialManager Instance { get; private set; }
        public TutorialStep CurrentStep => (TutorialStep)(SaveManager.Instance?.Data?.tutorialStep ?? 0);
        public string PlayerName => SaveManager.Instance?.Data?.playerName ?? string.Empty;

        private bool dialogueOpen;
        private bool namePromptOpen;
        private Button swordButton;
        private Button upgradeButton;
        private Button bagButton;
        private Button battleButton;
        private TutorialHighlight activeHighlight;
        private TutorialLockManager tutorialLocks;
        private int upgradeLevelBeforeTutorial;
        private float upgradeCompletedAt = -1f;
        private bool afterUpgradeRoutineActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            tutorialLocks = gameObject.AddComponent<TutorialLockManager>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureTutorialSave();
        }

        private void Start()
        {
            StartCoroutine(ProcessCurrentSceneNextFrame());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (swordButton != null)
                swordButton.onClick.RemoveListener(OnIronSwordSelected);
            if (upgradeButton != null)
                upgradeButton.onClick.RemoveListener(OnUpgradeButtonPressed);
            if (bagButton != null)
                bagButton.onClick.RemoveListener(OnBagButtonPressed);
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (IsScene(BattleSceneName) && CurrentStep == TutorialStep.BattleUnlocked &&
                StageManager.Instance != null && StageManager.Instance.CurrentState == StageManager.StageState.Victory)
            {
                SetStep(TutorialStep.BattleCompleted);
            }

            if (IsScene(TownSceneName) && CurrentStep == TutorialStep.UpgradeIronSword &&
                GetIronSwordUpgradeLevel() > upgradeLevelBeforeTutorial)
            {
                upgradeCompletedAt = Time.unscaledTime;
                SetStep(TutorialStep.AfterUpgrade);
            }

        }

        private void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            ClearSceneReferences();
            StartCoroutine(ProcessCurrentSceneNextFrame());
        }

        private IEnumerator ProcessCurrentSceneNextFrame()
        {
            yield return null;
            EnsureTutorialSave();
            if (IsScene(TownSceneName))
                ProcessTownStep();
        }

        private void EnsureTutorialSave()
        {
            SaveData data = SaveManager.Instance?.Data;
            if (data == null || data.tutorialInitialized)
                return;

            data.tutorialInitialized = true;
            data.tutorialStep = (int)TutorialStep.Introduction;
            SaveCoordinator.RequestSave();
        }

        private void ProcessTownStep()
        {
            ConfigureBattleAccess();
            switch (CurrentStep)
            {
                case TutorialStep.Introduction:
                    ShowDialogue(new[] { "Welcome to ETERNAL CLASH.", "What is your name, warrior?" }, () =>
                    {
                        SetStep(TutorialStep.AwaitingName);
                        ShowNamePrompt();
                    });
                    break;

                case TutorialStep.AwaitingName:
                    ShowNamePrompt();
                    break;

                case TutorialStep.FirstBattlePrompt:
                    ShowDialogue(new[] { "Try your first battle." }, () => SetStep(TutorialStep.BattleUnlocked));
                    break;

                case TutorialStep.BattleCompleted:
                    ShowDialogue(new[] { "Good job.", "You collected materials." },
                        () => SetStep(TutorialStep.BlacksmithIntroduction));
                    break;

                case TutorialStep.BlacksmithIntroduction:
                    ShowDialogue(new[] { "Your weapon can be improved." }, () =>
                    {
                        SetStep(TutorialStep.SelectIronSword);
                        OpenBlacksmith();
                    });
                    break;

                case TutorialStep.SelectIronSword:
                    OpenBlacksmith();
                    WireIronSwordButton();
                    break;

                case TutorialStep.UpgradeIronSword:
                    WireUpgradeButton();
                    break;

                case TutorialStep.AfterUpgrade:
                    StartAfterUpgradeFlow();
                    break;

                case TutorialStep.BagUnlocked:
                    RefreshBag();
                    WireBagButton();
                    break;
            }
        }

        private void ShowDialogue(string[] lines, Action onComplete)
        {
            if (dialogueOpen)
                return;

            DialogueManager dialogueManager = FindObjectOfType<DialogueManager>(true);
            if (dialogueManager == null)
            {
                Debug.LogWarning("[TUTORIAL] DialogueManager was not found in Town.", this);
                return;
            }

            dialogueOpen = true;
            void Completed()
            {
                dialogueManager.DialogueCompleted -= Completed;
                dialogueOpen = false;
                onComplete?.Invoke();
            }

            dialogueManager.DialogueCompleted += Completed;
            dialogueManager.OpenDialogue("Village Guide", null, lines);
        }

        private void ShowNamePrompt()
        {
            if (namePromptOpen)
                return;

            Canvas canvas = FindObjectOfType<Canvas>(true);
            if (canvas == null)
            {
                Debug.LogWarning("[TUTORIAL] Cannot ask for a player name without a Canvas.", this);
                return;
            }

            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            namePromptOpen = true;
            GameObject panel = CreatePanel(canvas.transform, "PlayerNamePrompt", new Vector2(620f, 260f));
            CreateText(panel.transform, "Title", "What should we call you?", 26, new Vector2(0f, 82f), new Vector2(540f, 45f));

            InputField input = CreateInput(panel.transform);
            input.text = PlayerName;
            Button confirm = CreateButton(panel.transform, "Confirm", new Vector2(0f, -82f));
            CreateText(confirm.transform, "Label", "Confirm", 20, Vector2.zero, new Vector2(180f, 45f));
            confirm.onClick.AddListener(() => SubmitPlayerName(input, panel));
            input.ActivateInputField();
        }

        private void SubmitPlayerName(InputField input, GameObject panel)
        {
            string nameValue = input != null ? input.text.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(nameValue))
                return;

            SaveData data = SaveManager.Instance?.Data;
            if (data == null)
                return;

            data.playerName = nameValue;
            SetStep(TutorialStep.FirstBattlePrompt);
            namePromptOpen = false;
            Destroy(panel);
        }

        private void OpenBlacksmith()
        {
            ShopThoRenController oldBlacksmithUi = FindObjectOfType<ShopThoRenController>(true);
            if (oldBlacksmithUi != null)
            {
                oldBlacksmithUi.gameObject.SetActive(true);
                return;
            }

            BlacksmithShopUI blacksmithUi = FindObjectOfType<BlacksmithShopUI>(true);
            if (blacksmithUi != null)
                blacksmithUi.Open();
        }

        private void WireIronSwordButton()
        {
            Button target = FindIronSwordButton();
            if (target == null)
                return;

            if (swordButton != target)
            {
                if (swordButton != null)
                    swordButton.onClick.RemoveListener(OnIronSwordSelected);
                swordButton = target;
                swordButton.onClick.AddListener(OnIronSwordSelected);
            }
            Highlight(target.gameObject);
        }

        private void OnIronSwordSelected()
        {
            if (CurrentStep != TutorialStep.SelectIronSword)
                return;

            ClearHighlight();
            upgradeLevelBeforeTutorial = GetIronSwordUpgradeLevel();
            SetStep(TutorialStep.UpgradeIronSword);
            ShowDialogue(new[] { "Upgrading equipment makes you stronger." }, WireUpgradeButton);
        }

        private void WireUpgradeButton()
        {
            Button target = FindButton("UPGRADE", "upgrade");
            if (target == null)
                return;

            if (upgradeButton != target)
            {
                if (upgradeButton != null)
                    upgradeButton.onClick.RemoveListener(OnUpgradeButtonPressed);
                upgradeButton = target;
                upgradeButton.onClick.AddListener(OnUpgradeButtonPressed);
            }
            Highlight(target.gameObject);
        }

        private void OnUpgradeButtonPressed()
        {
            if (CurrentStep == TutorialStep.UpgradeIronSword)
                ClearHighlight();
        }

        private void WireBagButton()
        {
            Button target = FindBagButton();
            if (target == null)
                return;

            if (bagButton != target)
            {
                if (bagButton != null)
                    bagButton.onClick.RemoveListener(OnBagButtonPressed);
                bagButton = target;
                bagButton.onClick.AddListener(OnBagButtonPressed);
            }
            Highlight(target.gameObject);
        }

        private void OnBagButtonPressed()
        {
            if (CurrentStep != TutorialStep.BagUnlocked)
                return;

            RefreshBag();
            ClearHighlight();
            SetStep(TutorialStep.Completed);
        }

        private static Button FindIronSwordButton()
        {
            foreach (ShopItemButton itemButton in FindObjectsOfType<ShopItemButton>(true))
            {
                if (itemButton.ItemData != null && string.Equals(itemButton.ItemData.itemId,
                        NewGameEquipmentDefaults.IronSwordId, StringComparison.OrdinalIgnoreCase))
                    return itemButton.GetComponent<Button>();
            }

            return FindButton("Iron Sword", "kiem", "sword");
        }

        private static Button FindBagButton()
        {
            foreach (BagController bag in FindObjectsOfType<BagController>(true))
            {
                Button button = bag.GetComponent<Button>() ?? bag.GetComponentInParent<Button>();
                if (button != null)
                    return button;
            }

            return FindButton("Bag", "bag", "tui");
        }

        private static Button FindButton(string exactName, params string[] nameParts)
        {
            Button partialMatch = null;
            foreach (Button button in FindObjectsOfType<Button>(true))
            {
                if (string.Equals(button.name, exactName, StringComparison.OrdinalIgnoreCase))
                    return button;

                foreach (string part in nameParts)
                {
                    if (button.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        partialMatch ??= button;
                        break;
                    }
                }
            }
            return partialMatch;
        }

        private static int GetIronSwordUpgradeLevel()
        {
            return SaveManager.Instance?.Data?.equipment?.weapon?.upgradeLevel ?? 0;
        }

        private static bool IsUpgradeResultVisible()
        {
            foreach (Transform transform in FindObjectsOfType<Transform>(true))
            {
                if ((string.Equals(transform.name, "UpGradeSuccess", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(transform.name, "UpGradeFail", StringComparison.OrdinalIgnoreCase)) &&
                    transform.gameObject.activeInHierarchy)
                    return true;
            }
            return false;
        }

        private void RefreshBag()
        {
            foreach (BagController bag in FindObjectsOfType<BagController>(true))
                bag.Refresh();
        }

        private void Highlight(GameObject target)
        {
            if (target == null)
                return;
            if (activeHighlight != null && activeHighlight.gameObject != target)
                activeHighlight.SetHighlighted(false);
            activeHighlight = target.GetComponent<TutorialHighlight>() ?? target.AddComponent<TutorialHighlight>();
            activeHighlight.SetHighlighted(true);
        }

        private void ClearHighlight()
        {
            if (activeHighlight != null)
                activeHighlight.SetHighlighted(false);
            activeHighlight = null;
        }

        private void SetStep(TutorialStep step)
        {
            SaveData data = SaveManager.Instance?.Data;
            if (data == null || data.tutorialStep == (int)step)
                return;

            data.tutorialStep = (int)step;
            if (step == TutorialStep.BattleUnlocked)
            {
                data.stageLevel = 1;
                if (data.progress != null)
                    data.progress.stageLevel = 1;
            }
            SaveCoordinator.RequestSave();

            if (IsScene(TownSceneName))
                ProcessTownStep();
        }

        private void ClearSceneReferences()
        {
            ClearHighlight();
            tutorialLocks?.RestoreAll();
            swordButton = null;
            upgradeButton = null;
            bagButton = null;
            battleButton = null;
            dialogueOpen = false;
            namePromptOpen = false;
            upgradeCompletedAt = -1f;
            afterUpgradeRoutineActive = false;
        }

        private void ConfigureBattleAccess()
        {
            Button target = FindButton("StartBattle", "startbattle", "start battle", "battle");
            if (target == null)
                return;

            battleButton = target;
            bool battleUnlocked = CurrentStep >= TutorialStep.BattleUnlocked;
            tutorialLocks.SetLocked(battleButton, !battleUnlocked);
        }

        private void StartAfterUpgradeFlow()
        {
            if (afterUpgradeRoutineActive)
                return;

            afterUpgradeRoutineActive = true;
            StartCoroutine(WaitForUpgradeResultThenShowBagDialogue());
        }

        private IEnumerator WaitForUpgradeResultThenShowBagDialogue()
        {
            if (upgradeCompletedAt >= 0f)
                yield return new WaitForSecondsRealtime(0.25f);

            while (IsUpgradeResultVisible())
                yield return null;

            afterUpgradeRoutineActive = false;
            if (IsScene(TownSceneName) && CurrentStep == TutorialStep.AfterUpgrade)
            {
                ShowDialogue(new[] { "Materials you collect are stored in your bag." }, () =>
                {
                    SetStep(TutorialStep.BagUnlocked);
                    WireBagButton();
                });
            }
        }

        private static bool IsScene(string sceneName)
        {
            return string.Equals(SceneManager.GetActiveScene().name, sceneName, StringComparison.OrdinalIgnoreCase);
        }

        private static GameObject CreatePanel(Transform parent, string objectName, Vector2 size)
        {
            GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            panel.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.12f, 0.96f);
            panel.transform.SetAsLastSibling();
            return panel;
        }

        private static InputField CreateInput(Transform parent)
        {
            GameObject inputObject = new GameObject("NameInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(parent, false);
            RectTransform rect = inputObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 5f);
            rect.sizeDelta = new Vector2(460f, 52f);
            inputObject.GetComponent<Image>().color = Color.white;

            InputField input = inputObject.GetComponent<InputField>();
            Text text = CreateText(inputObject.transform, "Text", string.Empty, 20, Vector2.zero, new Vector2(430f, 46f));
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.black;
            input.textComponent = text;
            return input;
        }

        private static Button CreateButton(Transform parent, string objectName, Vector2 position)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(180f, 45f);
            buttonObject.GetComponent<Image>().color = new Color(0.25f, 0.48f, 0.72f, 1f);
            return buttonObject.GetComponent<Button>();
        }

        private static Text CreateText(Transform parent, string objectName, string value, int fontSize, Vector2 position, Vector2 size)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        public void Notify(TutorialTriggerEvent tutorialEvent)
        {
            switch (tutorialEvent)
            {
                case TutorialTriggerEvent.IronSwordSelected:
                    OnIronSwordSelected();
                    break;
                case TutorialTriggerEvent.UpgradePressed:
                    OnUpgradeButtonPressed();
                    break;
                case TutorialTriggerEvent.BagOpened:
                    OnBagButtonPressed();
                    break;
            }
        }
    }
}
