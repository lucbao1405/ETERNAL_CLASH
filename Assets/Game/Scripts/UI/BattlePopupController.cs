using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EternalClash.UI
{
    /// <summary>
    /// Runtime wiring for the Battle settings/win/lose panels. The controller is
    /// created automatically and resolves the scene objects by name, including
    /// inactive objects, so the buttons do not need Inspector OnClick entries.
    /// </summary>
    public sealed class BattlePopupController : MonoBehaviour
    {
        private const string BattleSceneName = "Battle";

        [Header("Slide")]
        [SerializeField] private float settingOnScreenY = -400f;
        [SerializeField] private float resultOnScreenY;
        [SerializeField] private float slideDuration = 0.35f;
        [SerializeField] private float offScreenPadding = 40f;

        public static BattlePopupController Instance { get; private set; }

        private RectTransform settingPanel;
        private RectTransform winPopup;
        private RectTransform losePopup;
        private Button settingsButton;
        private Button closeSettingsButton;

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
            BattlePopupController controller = EnsureController();
            return controller != null && controller.ShowWin();
        }

        public static bool TryShowLose()
        {
            BattlePopupController controller = EnsureController();
            return controller != null && controller.ShowLose();
        }

        public bool ShowWin()
        {
            InitializeIfNeeded();
            if (winMotion == null)
                return false;

            if (loseMotion != null)
                ClosePanel(loseMotion, true);
            OpenPanel(winMotion);
            return true;
        }

        public bool ShowLose()
        {
            InitializeIfNeeded();
            if (loseMotion == null)
                return false;

            if (winMotion != null)
                ClosePanel(winMotion, true);
            OpenPanel(loseMotion);
            return true;
        }

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

            WireButtons();

            if (settingsButton == null)
                Debug.LogWarning("[BattlePopup] SettingsButton was not found in Battle scene.");
            if (settingPanel == null)
                Debug.LogWarning("[BattlePopup] Setting panel was not found in Battle scene.");
            if (closeSettingsButton == null)
                Debug.LogWarning("[BattlePopup] Setting/X button was not found in Battle scene.");
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
        }

        private void UnwireButtons()
        {
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OpenSettings);
            if (closeSettingsButton != null)
                closeSettingsButton.onClick.RemoveListener(CloseSettings);
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
