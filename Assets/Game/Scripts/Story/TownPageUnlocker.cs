using UnityEngine;
using EternalClash.Core.Save;

namespace EternalClash.Story
{
    /// <summary>
    /// Town roads unlock progressively. You meet an area's NPC at the end of a
    /// battle stage; only after clearing it does that road (page) appear in Town.
    /// Rule: Page_N becomes visible once stageLevel >= N (i.e. stage N-1 cleared).
    /// Page_1 (home, Ela's house) is always there; the Content width shrinks so
    /// locked roads cannot even be scrolled to.
    /// </summary>
    public class TownPageUnlocker : MonoBehaviour
    {
        public static TownPageUnlocker Instance { get; private set; }

        private const float PageWidth = 1080f;
        private const float LastPageExtraWidth = 80f; // Page_4 is 1000 wide, not 1080

        /// <summary>How many pages (roads) are currently unlocked, starting at 1.</summary>
        public int UnlockedPageCount { get; private set; } = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
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

            if (FindObjectOfType<TownPageUnlocker>() != null)
                return;

            new GameObject("TownPageUnlocker (Runtime)").AddComponent<TownPageUnlocker>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Apply();
        }

        private void Apply()
        {
            Transform content = GameObject.Find("Canvas/Up_Panel/Background/Viewport/Content")?.transform;
            if (content == null)
            {
                Debug.LogWarning("[TownUnlock] Scroll Content not found; roads stay as authored.");
                return;
            }

            int stageLevel = SaveManager.Instance?.Data?.stageLevel ?? 1;

            int count = 1; // Page_1 always unlocked
            for (int n = 2; n <= 4; n++)
            {
                Transform page = content.Find("Page_" + n);
                if (page == null) continue;

                bool unlocked = stageLevel >= n;
                page.gameObject.SetActive(unlocked);
                if (unlocked) count = n;
            }

            UnlockedPageCount = count;

            // Shrink the scroll range so locked roads are unreachable, not just invisible.
            RectTransform contentRect = content as RectTransform;
            float width = count * PageWidth;
            if (count >= 4)
                width -= LastPageExtraWidth; // all four visible -> original 4240 layout
            contentRect.sizeDelta = new Vector2(width, contentRect.sizeDelta.y);

            Debug.Log($"[TownUnlock] stageLevel={stageLevel} -> {count} road(s) unlocked, content width={width}.");
        }
    }
}
