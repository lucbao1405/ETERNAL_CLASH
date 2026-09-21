using EternalClash.Wave;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EternalClash.UI
{
    /// <summary>
    /// Thanh tien trinh tran dau o tren giua man hinh: "STAGE 2 - DOT 1/3" va so quai
    /// da ha trong dot dang danh.
    ///
    /// Tu tao khi vao scene Battle (khong can dat vao scene), tu an khi tran ket thuc
    /// de khong de len popup Thang / Thua.
    /// </summary>
    public sealed class BattleProgressUI : MonoBehaviour
    {
        private const string BattleSceneName = "Battle";
        private const string RootName = "BattleProgressUI";

        private CanvasGroup group;
        private TMP_Text titleText;
        private TMP_Text countText;
        private Image barFill;

        private WaveManager waveManager;

        private void Awake()
        {
            // Scene Battle da co object nay dat thu cong trong hierarchy:
            // chi resolve tham chieu con, khong build lai.
            group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            titleText ??= transform.Find("Title")?.GetComponent<TMP_Text>();
            barFill ??= transform.Find("Bar/Fill")?.GetComponent<Image>();
            countText ??= transform.Find("Bar/Count")?.GetComponent<TMP_Text>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureInBattleScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
                EnsureInBattleScene();
        }

        private static void EnsureInBattleScene()
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, BattleSceneName,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                // Roi Battle: don not UI cu neu no dang bam vao canvas song xuyen scene.
                BattleProgressUI leftover = FindObjectOfType<BattleProgressUI>();
                if (leftover != null)
                    Destroy(leftover.gameObject);
                return;
            }

            Scene active = SceneManager.GetActiveScene();
            BattleProgressUI existing = FindObjectOfType<BattleProgressUI>();
            if (existing != null)
            {
                // Con nam trong scene hien tai thi dung lai. Neu no bi gan nham vao canvas
                // song xuyen scene (ToastMessage) thi xoa di de tao lai o dung cho.
                if (existing.gameObject.scene == active)
                    return;

                Destroy(existing.gameObject);
            }

            Canvas canvas = FindBattleCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("[BattleProgress] Khong tim thay Canvas trong scene Battle.");
                return;
            }

            Build(canvas);
        }

        /// <summary>
        /// Canvas cua CHINH scene Battle. Phai loc theo scene: cac canvas song xuyen
        /// scene (vd ToastMessage) nam trong DontDestroyOnLoad va dang an, gan UI vao
        /// do thi khong bao gio thay.
        /// </summary>
        private static Canvas FindBattleCanvas()
        {
            Scene active = SceneManager.GetActiveScene();
            Canvas fallback = null;

            foreach (Canvas canvas in FindObjectsOfType<Canvas>())
            {
                if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)
                    continue;
                if (canvas.gameObject.scene != active)
                    continue;

                if (canvas.name == "MainCanvas")
                    return canvas;

                if (fallback == null)
                    fallback = canvas;
            }

            return fallback;
        }

        // ------------------------------------------------------------------
        // Dung UI
        // ------------------------------------------------------------------

        private static void Build(Canvas canvas)
        {
            // Font lay tu mot o chu co san trong scene de dong bo kieu chu voi UI hien tai.
            TMP_Text sample = canvas.GetComponentInChildren<TMP_Text>(true);

            var root = new GameObject(RootName, typeof(RectTransform), typeof(CanvasGroup));
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(canvas.transform, false);
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -70f);
            rootRect.sizeDelta = new Vector2(620f, 130f);

            var ui = root.AddComponent<BattleProgressUI>();
            ui.group = root.GetComponent<CanvasGroup>();
            ui.group.blocksRaycasts = false;
            ui.group.interactable = false;

            ui.titleText = CreateText(rootRect, "Title", sample, 46f,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -4f), new Vector2(0f, 56f));

            // Thanh do quai da ha.
            var barBack = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            var barRect = (RectTransform)barBack.transform;
            barRect.SetParent(rootRect, false);
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.anchoredPosition = new Vector2(0f, -66f);
            barRect.sizeDelta = new Vector2(-40f, 40f);
            Image back = barBack.GetComponent<Image>();
            back.color = new Color(0f, 0f, 0f, 0.55f);
            back.raycastTarget = false;

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var fillRect = (RectTransform)fillObject.transform;
            fillRect.SetParent(barRect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
            ui.barFill = fillObject.GetComponent<Image>();
            ui.barFill.color = new Color(1f, 0.78f, 0.25f, 1f);
            ui.barFill.raycastTarget = false;
            ui.barFill.type = Image.Type.Filled;
            ui.barFill.fillMethod = Image.FillMethod.Horizontal;
            ui.barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            ui.barFill.fillAmount = 0f;

            ui.countText = CreateText(barRect, "Count", sample, 28f,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            ui.Refresh();
        }

        private static TMP_Text CreateText(RectTransform parent, string name, TMP_Text sample, float size,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, anchorMax.y >= 1f && anchorMin.y >= 1f ? 1f : 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = sizeDelta;

            var text = go.GetComponent<TextMeshProUGUI>();
            if (sample != null && sample.font != null)
                text.font = sample.font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color32(0, 0, 0, 255);
            return text;
        }

        // ------------------------------------------------------------------
        // Cap nhat
        // ------------------------------------------------------------------

        private void LateUpdate()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (waveManager == null)
                waveManager = WaveManager.Instance;

            bool battleRunning = StageManager.Instance == null ||
                                 StageManager.Instance.CurrentState == StageManager.StageState.Running;

            // Dang tam dung (bang Pause) thi an di cho khoi de len bang.
            bool paused = Time.timeScale <= 0.001f;

            if (waveManager == null || waveManager.TotalWaves <= 0 || !battleRunning || paused)
            {
                group.alpha = 0f;
                return;
            }

            if (titleText == null || countText == null || barFill == null)
                return;

            group.alpha = 1f;

            int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStageLevel : 1;
            int wave = Mathf.Clamp(waveManager.CurrentWaveNumber, 1, waveManager.TotalWaves);
            titleText.text = $"STAGE {stage}   -   WAVE {wave}/{waveManager.TotalWaves}";

            int total = Mathf.Max(0, waveManager.CurrentWaveEnemyCount);
            int killed = Mathf.Clamp(waveManager.CurrentWaveKilled, 0, total);
            countText.text = total > 0 ? $"{killed}/{total}" : string.Empty;
            barFill.fillAmount = total > 0 ? (float)killed / total : 0f;
        }
    }
}
