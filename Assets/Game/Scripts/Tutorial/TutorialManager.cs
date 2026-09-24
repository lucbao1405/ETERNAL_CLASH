using System;
using System.Collections;
using EternalClash.Core.Save;
using EternalClash.Stage;
using EternalClash.UI;
using EternalClash.Village;
using UnityEngine;
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
        private PlayerNamePromptUI namePrompt;
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
                    // Resuming mid-step must not force the shop open on every
                    // Town load; the player opens it via the blacksmith NPC.
                    // The one-time open after BlacksmithIntroduction still calls
                    // OpenBlacksmith directly.
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

            // Chi tim DialogueManager dang hoat dong: ban sao DialogueManager tren
            // prefab "thoai" trong scene Town nam tren object bi tat (khong hoi tu
            // Awake, khong co tham chieu) neu lay se lam cau hoi thoai huong dan
            // im lang that bai (ValidateReferences fail).
            DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
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

            PlayerNamePromptUI prompt = FindObjectOfType<PlayerNamePromptUI>(true);
            if (prompt == null)
            {
                Debug.LogWarning("[TUTORIAL] PlayerNamePromptUI was not found in Town.", this);
                return;
            }

            namePromptOpen = true;
            namePrompt = prompt;
            prompt.NameSubmitted += SubmitPlayerName;
            prompt.Show(PlayerName);
        }

        private void SubmitPlayerName(string nameValue)
        {
            if (string.IsNullOrWhiteSpace(nameValue))
                return;

            PlayerNamePromptUI prompt = namePrompt;
            if (prompt != null)
                prompt.NameSubmitted -= SubmitPlayerName;
            namePrompt = null;

            SaveData data = SaveManager.Instance?.Data;
            if (data == null)
            {
                namePromptOpen = false;
                return;
            }

            data.playerName = nameValue.Trim();
            namePromptOpen = false;
            if (prompt != null)
                prompt.Hide();

            SetStep(TutorialStep.FirstBattlePrompt);
        }

        private void OpenBlacksmith()
        {
            ShopThoRenController oldBlacksmithUi = FindObjectOfType<ShopThoRenController>(true);
            if (oldBlacksmithUi != null)
            {
                ShopPanelAnimator animator = oldBlacksmithUi.GetComponent<ShopPanelAnimator>();
                if (animator != null)
                {
                    animator.Open();
                    return;
                }

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
            if (namePrompt != null)
            {
                namePrompt.NameSubmitted -= SubmitPlayerName;
                namePrompt = null;
            }
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
