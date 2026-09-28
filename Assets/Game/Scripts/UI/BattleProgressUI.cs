using EternalClash.Wave;
using Spine.Unity;
using TMPro;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EternalClash.UI
{
    /// <summary>
    /// Thanh tien trinh tran dau o tren giua man hinh: nhan vat (Nhan_Vat_Chinh con cua Handle)
    /// chay tu dau thanh den Dichden theo tien do TOAN BO stage: (so wave da xong + phan tram
    /// quai da ha trong wave hien tai) / tong so wave. Fill chay theo player vi ca Fill va
    /// Handle deu do Slider "Progress" dieu khien - chi can gan slider.value.
    /// Object dat thu cong trong scene Battle (MainCanvas/BattleProgressUI), script chi resolve
    /// tham chieu con, khong build lai.
    /// </summary>
    public sealed class BattleProgressUI : MonoBehaviour
    {
        private const string BattleSceneName = "Battle";
        private const string RunAnim = "run";

        [Tooltip("Toc do player chay tren thanh (do lech qua muc dich / giay), de moi lan ha quai khong nhay toc.")]
        [SerializeField] private float runSpeed = 0.25f;

        [Header("Mau chu (chinh truc tiep trong Inspector)")]
        [Tooltip("Mau dong chu STAGE.")]
        [SerializeField] private Color titleColor = new Color(1f, 0.92f, 0.6f, 1f);

        [Tooltip("Mau dong chu WAVE x/y - k/n quai.")]
        [SerializeField] private Color waveColor = new Color(0.85f, 0.95f, 1f, 1f);

        private CanvasGroup group;
        private TMP_Text titleText;
        private TMP_Text waveText;
        private Slider progressSlider;
        private SkeletonGraphic playerSkeleton;
        private WaveManager waveManager;
        private bool runStarted;

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
            waveText ??= transform.Find("WaveText")?.GetComponent<TMP_Text>();
            progressSlider ??= transform.Find("Progress")?.GetComponent<Slider>();

            if (titleText != null)
                titleText.color = titleColor;
            if (waveText != null)
                waveText.color = waveColor;
            playerSkeleton ??= GetComponentInChildren<SkeletonGraphic>(true);

            // Fallback: scene cu co the de slider "Progress" o panel khac
            // (khong phai la con truc tiep cua BattleProgressUI) - tim trong toan canvas.
            if (progressSlider == null)
            {
                Transform canvas = GetComponentInParent<Canvas>()?.transform;
                progressSlider = canvas != null
                    ? canvas.GetComponentsInChildren<Slider>(true)
                        .FirstOrDefault(s => s.name == "Progress")
                    : null;
            }

            // Skeleton nhan vat tren thanh: con cua Handle/Fill cua slider.
            if (playerSkeleton == null && progressSlider != null)
                playerSkeleton = progressSlider.GetComponentInChildren<SkeletonGraphic>(true);

            if (progressSlider != null)
                progressSlider.interactable = false;

            if (titleText == null || progressSlider == null)
                Debug.LogWarning("[BattleProgress] Thieu con Title (TMP_Text) hoac Progress (Slider) trong hierarchy.");
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

        // UI dat thu cong trong scene nen chi canh bao khi thieu, tranh loi im lang.
        private static void EnsureInBattleScene()
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, BattleSceneName,
                    System.StringComparison.OrdinalIgnoreCase))
                return;

            if (FindObjectOfType<BattleProgressUI>() == null)
                Debug.LogWarning("[BattleProgress] Scene Battle thieu object BattleProgressUI (dat duoi MainCanvas).");
        }

        // ------------------------------------------------------------------
        // Cap nhat
        // ------------------------------------------------------------------

        private void LateUpdate()
        {
            if (waveManager == null)
                waveManager = WaveManager.Instance;

            // Player tren thanh chi co 1 hanh dong run: loop "run" lien tuc.
            if (!runStarted && playerSkeleton != null && playerSkeleton.AnimationState != null)
            {
                runStarted = true;
                if (playerSkeleton.Skeleton != null && playerSkeleton.Skeleton.Data.FindAnimation(RunAnim) != null)
                    playerSkeleton.AnimationState.SetAnimation(0, RunAnim, true);
                else
                    Debug.LogWarning($"[BattleProgress] Skeleton tren thanh thieu animation '{RunAnim}'.");
            }

            bool battleRunning = StageManager.Instance == null ||
                                 StageManager.Instance.CurrentState == StageManager.StageState.Running;

            // Dang tam dung (bang Pause) thi an di cho khoi de len bang.
            bool paused = Time.timeScale <= 0.001f;

            if (waveManager == null || waveManager.TotalWaves <= 0 || !battleRunning || paused)
            {
                group.alpha = 0f;
                return;
            }

            if (titleText == null || progressSlider == null)
                return;

            group.alpha = 1f;

            int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStageLevel : 1;
            int wave = Mathf.Clamp(waveManager.CurrentWaveNumber, 1, waveManager.TotalWaves);
            if (titleText != null)
                titleText.text = $"STAGE {stage}";
            if (waveText != null)
                waveText.text = $"WAVE {wave}/{waveManager.TotalWaves}   -   {waveManager.CurrentWaveKilled}/{waveManager.CurrentWaveEnemyCount} Enemy";

            float target = StageFraction(waveManager.CurrentWaveNumber, waveManager.TotalWaves,
                waveManager.CurrentWaveKilled, waveManager.CurrentWaveEnemyCount);
            progressSlider.value = Mathf.MoveTowards(progressSlider.value, target, runSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Tien do toan stage [0..1]: (so wave da xong + phan tram quai da ha cua wave hien tai)
        /// / tong so wave. Wave cuoi xong thi dung 1f (player cham Dichden). Pure de test.
        /// </summary>
        public static float StageFraction(int currentWaveNumber, int totalWaves, int waveKilled, int waveTotal)
        {
            if (totalWaves <= 0)
                return 0f;

            int wavesDone = Mathf.Clamp(currentWaveNumber - 1, 0, totalWaves);
            float waveFrac = waveTotal > 0 ? Mathf.Clamp01((float)waveKilled / waveTotal) : 0f;
            return Mathf.Clamp01((wavesDone + waveFrac) / totalWaves);
        }
    }
}
