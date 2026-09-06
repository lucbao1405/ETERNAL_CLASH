using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using EternalClash.Stage;
using EternalClash.Data;
using EternalClash.Village;

namespace EternalClash.UI
{
    /// <summary>
    /// Runtime wiring for the Battle settings/pause and win/lose panels.
    /// The controller is created automatically and resolves the scene objects by
    /// name (including inactive objects), so the buttons do not need Inspector
    /// OnClick entries.
    ///
    /// Responsibilities:
    ///  - Settings (pause) panel: open/resume via the pause button and the X
    ///    button, exit to town via the Return button.
    ///  - Win / Lose result popups: slide down from the top of the screen and show
    ///    data read from StageCompleteController.StageResult / PlayerStatSystem
    ///    (Victory/Defeat state, EXP, Gold, reward item, battle time, defeat message).
    /// </summary>
    public sealed class BattlePopupController : MonoBehaviour
    {
        private const string BattleSceneName = "Battle";
        private const string TownSceneName = "Town";
        private const string DefeatMessage = "Your character has fallen in battle.";

        [Header("Slide")]
        [SerializeField] private float settingOnScreenY = -400f;
        [SerializeField] private float resultOnScreenY;
        [SerializeField] private float slideDuration = 0.35f;
        [SerializeField] private float offScreenPadding = 40f;

        [Header("Summary Label Style")]
        [SerializeField] private float summaryLabelHeight = 64f;
        [SerializeField] private float summaryLabelFont = 40f;
        [SerializeField] private Color rewardLabelColor = new Color(1f, 0.75f, 0.2f, 1f);
        [SerializeField] private Color timeLabelColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color messageLabelColor = new Color(1f, 0.55f, 0.55f, 1f);

        public static BattlePopupController Instance { get; private set; }

        private RectTransform settingPanel;
        private RectTransform winPopup;
        private RectTransform losePopup;
        private Button settingsButton;
        private Button closeSettingsButton;
        private Button settingReturnButton;
        private Button winReturnButton;
        private Button loseReturnButton;

        private PanelMotion settingMotion;
        private PanelMotion winMotion;
        private PanelMotion loseMotion;
        private bool initialized;
        private bool pausedBySettings;
        private float timeScaleBeforeSettings = 1f;
        private bool audioPauseBeforeSettings;

        private sealed class PanelMotion
        {
            public RectTransform Rect;
            public CanvasGroup CanvasGroup;
            public Vector2 OpenPosition;
            public Vector2 ClosedPosition;
            public Coroutine Animation;
            public bool IsOpen;

            // Result / settings widgets resolved by name inside the panel.
            public Button ReturnButton;
            public TextMeshProUGUI XpLabel;
            public Slider XpSlider;
            public GameObject XpRoot;
            public GameObject ItemGrid;
            public TextMeshProUGUI RewardLabel;
            public TextMeshProUGUI GoldLabel;
            public TextMeshProUGUI TimeLabel;
            public TextMeshProUGUI MessageLabel;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapForBattle()
        {
            EnsureController();
        }

        private static BattlePopupController EnsureController()
        {
            if (Instance != null)
                return Instance;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.name, BattleSceneName, StringComparison.OrdinalIgnoreCase))
                return null;

            BattlePopupController existing = FindObjectOfType<BattlePopupController>();
            if (existing != null)
                return existing;

            GameObject host = new GameObject("BattlePopupController (Runtime)");
            return host.AddComponent<BattlePopupController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeIfNeeded();
        }

        private void OnDestroy()
        {
            UnwireButtons();
            RestoreGameTime();

            if (Instance == this)
                Instance = null;
        }

        // ------------------------------------------------------------------
        // Public API used by StageCompleteController (and tests).
        // ------------------------------------------------------------------

        public void OpenSettings()
        {
            InitializeIfNeeded();
            if (settingMotion == null)
            {
                Debug.LogWarning("[BattlePopup] Cannot open Setting: panel not found.");
                return;
            }

            if (!pausedBySettings)
            {
                timeScaleBeforeSettings = Time.timeScale;
                audioPauseBeforeSettings = AudioListener.pause;
                pausedBySettings = true;
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }

            CloseResultPanels();
            OpenPanel(settingMotion);
        }

        public void CloseSettings()
        {
            InitializeIfNeeded();
            if (settingMotion != null)
                ClosePanel(settingMotion);

            // Gameplay resumes as soon as X is pressed. The closing animation uses
            // unscaled time, so it remains smooth before and after this restore.
            RestoreGameTime();
        }

        public static bool TryShowWin()
        {
            return TryShowWin(null);
        }

        public static bool TryShowWin(StageResultData result)
        {
            BattlePopupController controller = EnsureController();
            return controller != null && controller.ShowWin(result);
        }

        public static bool TryShowLose()
        {
            return TryShowLose(null);
        }

        public static bool TryShowLose(StageResultData result)
        {
            BattlePopupController controller = EnsureController();
            return controller != null && controller.ShowLose(result);
        }

        public bool ShowWin()
        {
            return ShowWin(null);
        }

        public bool ShowWin(StageResultData result)
        {
            InitializeIfNeeded();
            if (winMotion == null)
                return false;

            RestoreGameTime();
            if (settingMotion != null)
                ClosePanel(settingMotion, true);
            if (settingsButton != null)
                settingsButton.gameObject.SetActive(false);

            PopulateWin(result);

            if (loseMotion != null)
                ClosePanel(loseMotion, true);
            OpenPanel(winMotion);
            return true;
        }

        public bool ShowLose()
        {
            return ShowLose(null);
        }

        public bool ShowLose(StageResultData result)
        {
            InitializeIfNeeded();
            if (loseMotion == null)
                return false;

            RestoreGameTime();
            if (settingMotion != null)
                ClosePanel(settingMotion, true);
            if (settingsButton != null)
                settingsButton.gameObject.SetActive(false);

            PopulateLose(result);

            if (winMotion != null)
                ClosePanel(winMotion, true);
            OpenPanel(loseMotion);
            return true;
        }

        // ------------------------------------------------------------------
        // Button actions
        // ------------------------------------------------------------------

        private void OnSettingsReturnClicked()
        {
            RestoreGameTime();
            ExitToTown();
        }

        private void OnWinReturnClicked()
        {
            if (winMotion != null)
                ClosePanel(winMotion, true);
            RestoreGameTime();
            ExitToTown();
        }

        private void OnLoseReturnClicked()
        {
            if (loseMotion != null)
                ClosePanel(loseMotion, true);
            RestoreGameTime();
            ExitToTown();
        }

        /// <summary>
        /// Returns to the Town using the existing StageCompleteController flow when
        /// available; otherwise falls back to loading the Town scene directly.
        /// </summary>
        private static void ExitToTown()
        {
            StageCompleteController controller = StageCompleteController.Instance;
            if (controller != null)
            {
                controller.ReturnToVillage();
            }
            else
            {
                SceneManager.LoadScene(TownSceneName);
            }
        }

        // ------------------------------------------------------------------
        // Data population
        // ------------------------------------------------------------------

        private static StageResultData ResolveResult(StageResultData provided)
        {
            if (provided != null)
                return provided;
            return StageCompleteController.Instance != null ? StageCompleteController.Instance.StageResult : null;
        }

        private void PopulateWin(StageResultData provided)
        {
            StageResultData result = ResolveResult(provided);
            float battleTime = result != null ? result.stageTime : 0f;
            int earnedExp = result != null ? result.earnedExp : 0;
            int earnedGold = result != null ? result.earnedGold : 0;
            RewardData reward = result != null && result.rewards != null && result.rewards.Count > 0
                ? result.rewards[0]
                : null;

            if (winMotion == null)
                return;

            ShowXpContent(winMotion, earnedExp, true);
            ApplyGoldLine(winMotion, earnedGold, reward);
            ApplyRewardLine(winMotion, reward);
            ApplyTimeLine(winMotion, battleTime, true);
            ShowMessageLine(winMotion, null);
            ShowItemGrid(winMotion, true);
        }

        private void PopulateLose(StageResultData provided)
        {
            StageResultData result = ResolveResult(provided);
            float battleTime = result != null ? result.stageTime : 0f;
            int earnedExp = result != null ? result.earnedExp : 0;

            if (loseMotion == null)
                return;

            ShowXpContent(loseMotion, earnedExp, false);
            ApplyTimeLine(loseMotion, battleTime, false);
            ShowMessageLine(loseMotion, DefeatMessage);
            ShowItemGrid(loseMotion, false);
        }

        /// <summary>
        /// Shows the earned EXP caption next to the XP progress bar. On victory the
        /// bar reflects the player's current level progress; on defeat the bar is
        /// hidden because the result popup only shows defeat information.
        /// </summary>
        private void ShowXpContent(PanelMotion motion, int earnedExp, bool showProgress)
        {
            if (motion.XpLabel != null)
            {
                motion.XpLabel.text = $"XP: +{earnedExp}";
                motion.XpLabel.gameObject.SetActive(true);
            }

            if (motion.XpSlider != null)
            {
                motion.XpSlider.gameObject.SetActive(showProgress);
                if (showProgress)
                {
                    PlayerStatSystem stats = PlayerStatSystem.Instance;
                    float ratio = stats != null && stats.RequiredExp > 0
                        ? (float)stats.CurrentExp / stats.RequiredExp
                        : 0f;
                    motion.XpSlider.value = Mathf.Clamp01(ratio);
                }
                else
                {
                    motion.XpSlider.value = 0f;
                }
            }

            if (motion.XpRoot != null)
                motion.XpRoot.SetActive(true);
        }

        private void ApplyGoldLine(PanelMotion motion, int earnedGold, RewardData reward)
        {
            int totalGold = earnedGold;
            if (reward != null && reward.type == RewardType.Gold)
                totalGold += reward.amount;

            if (motion.GoldLabel == null)
                return;

            if (totalGold > 0)
            {
                motion.GoldLabel.text = $"Gold +{totalGold}";
                motion.GoldLabel.gameObject.SetActive(true);
            }
            else
            {
                motion.GoldLabel.gameObject.SetActive(false);
            }
        }

        private void ApplyRewardLine(PanelMotion motion, RewardData reward)
        {
            if (motion.RewardLabel == null)
                return;

            string line = BuildRewardLine(reward);
            if (string.IsNullOrEmpty(line))
            {
                motion.RewardLabel.gameObject.SetActive(false);
                return;
            }

            motion.RewardLabel.text = line;
            motion.RewardLabel.gameObject.SetActive(true);
        }

        private static string BuildRewardLine(RewardData reward)
        {
            if (reward == null || reward.type == RewardType.Gold)
                return null;

            switch (reward.type)
            {
                case RewardType.Equipment:
                    return reward.item != null ? reward.item.itemName : null;
                case RewardType.Material:
                    return reward.amount > 0 ? $"+{reward.amount} Materials" : null;
                case RewardType.Gem:
                    return reward.amount > 0 ? $"+{reward.amount} Gems" : null;
                default:
                    return null;
            }
        }

        private void ApplyTimeLine(PanelMotion motion, float battleTime, bool victory)
        {
            if (motion.TimeLabel == null)
                return;

            string time = FormatTime(battleTime);
            motion.TimeLabel.text = victory ? $"Time  {time}" : $"Time Survived  {time}";
            motion.TimeLabel.gameObject.SetActive(true);
        }

        private void ShowMessageLine(PanelMotion motion, string message)
        {
            if (motion.MessageLabel == null)
                return;

            if (string.IsNullOrEmpty(message))
            {
                motion.MessageLabel.gameObject.SetActive(false);
                return;
            }

            motion.MessageLabel.text = message;
            motion.MessageLabel.gameObject.SetActive(true);
        }

        private void ShowItemGrid(PanelMotion motion, bool visible)
        {
            if (motion.ItemGrid == null)
                return;

            motion.ItemGrid.SetActive(visible);
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int minutes = total / 60;
            int secs = total % 60;
            return $"{minutes:00}:{secs:00}";
        }

        // ------------------------------------------------------------------
        // Scene wiring / panel preparation
        // ------------------------------------------------------------------

        private void InitializeIfNeeded()
        {
            if (initialized)
                return;

            initialized = true;
            Scene scene = gameObject.scene;

            settingPanel = FindSceneRect(scene, "Setting", "Settings");
            winPopup = FindSceneRect(scene, "Win_Popup", "WinPopup");
            losePopup = FindSceneRect(scene, "Lose_Popup", "LosePopup");

            Transform settingsButtonTransform = FindSceneTransform(scene, "SettingsButton", "SettingButton");
            settingsButton = EnsureButton(settingsButtonTransform);

            Transform closeTransform = settingPanel != null
                ? FindDescendant(settingPanel, "X", "Close", "CloseButton")
                : null;
            closeSettingsButton = EnsureButton(closeTransform);

            settingMotion = PreparePanel(settingPanel, settingOnScreenY);
            winMotion = PreparePanel(winPopup, resultOnScreenY);
            loseMotion = PreparePanel(losePopup, resultOnScreenY);

            BindSettingsPanel();
            BindResultPanel(winMotion, victory: true);
            BindResultPanel(loseMotion, victory: false);

            WireButtons();

            if (settingsButton == null)
                Debug.LogWarning("[BattlePopup] SettingsButton was not found in Battle scene.");
            if (settingPanel == null)
                Debug.LogWarning("[BattlePopup] Setting panel was not found in Battle scene.");
            if (closeSettingsButton == null)
                Debug.LogWarning("[BattlePopup] Setting/X button was not found in Battle scene.");
        }

        /// <summary>
        /// Resolves the pause (Setting) panel widgets: its Return button exits to
        /// town, while X resumes the game.
        /// </summary>
        private void BindSettingsPanel()
        {
            if (settingMotion == null || settingMotion.Rect == null)
                return;

            Transform returnTransform = FindDescendant(settingMotion.Rect, "Return", "ReturnButton", "Exit");
            if (returnTransform != null)
                settingReturnButton = returnTransform.GetComponent<Button>() ?? EnsureButton(returnTransform);
        }

        /// <summary>
        /// Binds the win/lose popup widgets (Return button, XP bar, item grid) and
        /// lazily adds small summary labels (Reward/Gold/Time for victory, Time/
        /// Message for defeat) that the scene popups do not provide out of the box.
        /// </summary>
        private void BindResultPanel(PanelMotion motion, bool victory)
        {
            if (motion == null || motion.Rect == null)
                return;

            Transform returnTransform = FindDescendant(motion.Rect, "Return", "ReturnButton", "Continue", "Done");
            if (returnTransform != null)
                motion.ReturnButton = returnTransform.GetComponent<Button>() ?? EnsureButton(returnTransform);

            TextMeshProUGUI xpLabel = FindDescendant(motion.Rect, "Xp_Text", "XpText")
                ?.GetComponent<TextMeshProUGUI>();
            motion.XpLabel = xpLabel;

            Transform xpSliderTransform = FindDescendant(motion.Rect, "Xp_Slide", "Xp_Silde", "XpSlide");
            motion.XpSlider = xpSliderTransform != null ? xpSliderTransform.GetComponent<Slider>() : null;

            Transform xpRoot = FindDescendant(motion.Rect, "Xp", "ExpBar", "XpBar");
            motion.XpRoot = xpRoot != null ? xpRoot.gameObject : null;

            Transform gridTransform = FindDescendant(motion.Rect, "Vat_Pham", "ItemGrid", "RewardGrid", "RewardItems");
            motion.ItemGrid = gridTransform != null ? gridTransform.gameObject : null;

            TextMeshProUGUI sample = xpLabel;
            if (sample == null)
            {
                Transform sampleTransform = FindDescendant(motion.Rect, "Text", "Text (TMP)", "Label");
                sample = sampleTransform != null ? sampleTransform.GetComponent<TextMeshProUGUI>() : null;
            }

            if (victory)
            {
                motion.RewardLabel = EnsureSummaryLabel(motion.Rect, sample, "RewardText",
                    new Vector2(0f, -360f), 800f, summaryLabelHeight, summaryLabelFont, rewardLabelColor);
                motion.GoldLabel = EnsureSummaryLabel(motion.Rect, sample, "GoldText",
                    new Vector2(0f, -445f), 640f, summaryLabelHeight, summaryLabelFont, rewardLabelColor);
                motion.TimeLabel = EnsureSummaryLabel(motion.Rect, sample, "TimeText",
                    new Vector2(0f, -530f), 640f, summaryLabelHeight, summaryLabelFont, timeLabelColor);
            }
            else
            {
                motion.MessageLabel = EnsureSummaryLabel(motion.Rect, sample, "MessageText",
                    new Vector2(0f, -320f), 900f, summaryLabelHeight + 70f, summaryLabelFont + 6f, messageLabelColor);
                motion.TimeLabel = EnsureSummaryLabel(motion.Rect, sample, "TimeText",
                    new Vector2(0f, -470f), 720f, summaryLabelHeight, summaryLabelFont, timeLabelColor);
            }
        }

        private TextMeshProUGUI EnsureSummaryLabel(
            RectTransform panelRoot,
            TextMeshProUGUI sample,
            string labelName,
            Vector2 anchoredPosition,
            float width,
            float height,
            float fontSize,
            Color color)
        {
            TextMeshProUGUI label = null;

            Transform existing = FindDescendant(panelRoot, labelName);
            if (existing != null)
            {
                label = existing.GetComponent<TextMeshProUGUI>();
                if (label == null)
                {
                    Debug.LogWarning($"[BattlePopup] '{labelName}' exists but has no TextMeshProUGUI.");
                    return null;
                }
            }
            else if (sample != null)
            {
                TextMeshProUGUI clone = Instantiate(sample, panelRoot);
                clone.name = labelName;
                label = clone;
            }
            else
            {
                Debug.LogWarning($"[BattlePopup] Cannot create '{labelName}': no TextMeshProUGUI sample found in popup.");
                return null;
            }

            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.one * 0.5f;
            rect.anchorMax = Vector2.one * 0.5f;
            rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();

            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = true;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = fontSize;
            label.color = color;
            label.raycastTarget = false;
            label.text = string.Empty;
            return label;
        }

        private PanelMotion PreparePanel(RectTransform panel, float targetY)
        {
            if (panel == null)
                return null;

            Vector2 openPosition = new Vector2(panel.anchoredPosition.x, targetY);
            float requiredClosedY = CalculateOffScreenY(panel, targetY);
            Vector2 closedPosition = panel.anchoredPosition;
            closedPosition.x = openPosition.x;
            closedPosition.y = Mathf.Max(closedPosition.y, requiredClosedY);

            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group == null)
                group = panel.gameObject.AddComponent<CanvasGroup>();

            PanelMotion motion = new PanelMotion
            {
                Rect = panel,
                CanvasGroup = group,
                OpenPosition = openPosition,
                ClosedPosition = closedPosition,
                IsOpen = false
            };

            panel.anchoredPosition = closedPosition;
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;
            panel.gameObject.SetActive(false);
            return motion;
        }

        private float CalculateOffScreenY(RectTransform panel, float openY)
        {
            RectTransform parentRect = panel.parent as RectTransform;
            float parentHeight = parentRect != null ? Mathf.Abs(parentRect.rect.height) : Screen.height;
            float panelHeight = Mathf.Abs(panel.rect.height);
            return openY + parentHeight * 0.5f + panelHeight * 0.5f + offScreenPadding;
        }

        private void OpenPanel(PanelMotion motion)
        {
            if (motion == null)
                return;

            if (motion.Animation != null)
                StopCoroutine(motion.Animation);

            motion.Rect.gameObject.SetActive(true);
            motion.Rect.SetAsLastSibling();
            motion.CanvasGroup.alpha = 1f;
            motion.CanvasGroup.interactable = true;
            motion.CanvasGroup.blocksRaycasts = true;
            motion.IsOpen = true;
            motion.Animation = StartCoroutine(SlideTo(motion, motion.OpenPosition, false));
        }

        private void ClosePanel(PanelMotion motion, bool immediate = false)
        {
            if (motion == null)
                return;

            if (motion.Animation != null)
                StopCoroutine(motion.Animation);

            motion.CanvasGroup.interactable = false;
            motion.CanvasGroup.blocksRaycasts = false;
            motion.IsOpen = false;

            if (immediate)
            {
                motion.Rect.anchoredPosition = motion.ClosedPosition;
                motion.Rect.gameObject.SetActive(false);
                motion.Animation = null;
                return;
            }

            motion.Animation = StartCoroutine(SlideTo(motion, motion.ClosedPosition, true));
        }

        private void CloseResultPanels()
        {
            if (winMotion != null)
                ClosePanel(winMotion, true);
            if (loseMotion != null)
                ClosePanel(loseMotion, true);
        }

        private IEnumerator SlideTo(PanelMotion motion, Vector2 destination, bool deactivateAfter)
        {
            Vector2 start = motion.Rect.anchoredPosition;
            float duration = Mathf.Max(0.01f, slideDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                motion.Rect.anchoredPosition = Vector2.LerpUnclamped(start, destination, eased);
                yield return null;
            }

            motion.Rect.anchoredPosition = destination;
            motion.Animation = null;

            if (deactivateAfter && !motion.IsOpen)
                motion.Rect.gameObject.SetActive(false);
        }

        private void RestoreGameTime()
        {
            if (!pausedBySettings)
                return;

            Time.timeScale = timeScaleBeforeSettings;
            AudioListener.pause = audioPauseBeforeSettings;
            pausedBySettings = false;
        }

        private void WireButtons()
        {
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
            if (closeSettingsButton != null)
                closeSettingsButton.onClick.AddListener(CloseSettings);
            if (settingReturnButton != null)
                settingReturnButton.onClick.AddListener(OnSettingsReturnClicked);

            if (winMotion != null && winMotion.ReturnButton != null)
            {
                winReturnButton = winMotion.ReturnButton;
                winReturnButton.onClick.AddListener(OnWinReturnClicked);
            }

            if (loseMotion != null && loseMotion.ReturnButton != null)
            {
                loseReturnButton = loseMotion.ReturnButton;
                loseReturnButton.onClick.AddListener(OnLoseReturnClicked);
            }
        }

        private void UnwireButtons()
        {
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OpenSettings);
            if (closeSettingsButton != null)
                closeSettingsButton.onClick.RemoveListener(CloseSettings);
            if (settingReturnButton != null)
                settingReturnButton.onClick.RemoveListener(OnSettingsReturnClicked);
            if (winReturnButton != null)
                winReturnButton.onClick.RemoveListener(OnWinReturnClicked);
            if (loseReturnButton != null)
                loseReturnButton.onClick.RemoveListener(OnLoseReturnClicked);
        }

        private static Button EnsureButton(Transform target)
        {
            if (target == null)
                return null;

            Button button = target.GetComponent<Button>();
            if (button == null)
            {
                button = target.gameObject.AddComponent<Button>();
                button.targetGraphic = target.GetComponent<Graphic>();
            }

            return button;
        }

        private static RectTransform FindSceneRect(Scene scene, params string[] names)
        {
            return FindSceneTransform(scene, names) as RectTransform;
        }

        private static Transform FindSceneTransform(Scene scene, params string[] names)
        {
            if (!scene.IsValid())
                return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform candidate in transforms)
                {
                    if (NameMatches(candidate.name, names))
                        return candidate;
                }
            }

            return null;
        }

        private static Transform FindDescendant(Transform root, params string[] names)
        {
            if (root == null)
                return null;

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate != root && NameMatches(candidate.name, names))
                    return candidate;
            }

            return null;
        }

        private static bool NameMatches(string candidate, string[] names)
        {
            string normalizedCandidate = NormalizeName(candidate);
            foreach (string name in names)
            {
                if (normalizedCandidate == NormalizeName(name))
                    return true;
            }

            return false;
        }

        private static string NormalizeName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            char[] buffer = new char[value.Length];
            int length = 0;
            foreach (char character in value)
            {
                if (!char.IsLetterOrDigit(character))
                    continue;

                buffer[length++] = char.ToLowerInvariant(character);
            }

            return new string(buffer, 0, length);
        }
    }
}
