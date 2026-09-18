using EternalClash.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EternalClash.UI
{
    /// <summary>
    /// Bi mat o Town: tap lien tiep vao coc (Page_4/coc) du <see cref="requiredTaps"/> lan
    /// thi hoi day 100% HP cho player. Hai lan tap cach nhau qua
    /// <see cref="maxTapInterval"/> giay thi dem lai tu dau.
    ///
    /// Tu gan vao coc moi khi load scene, nen van chay khi scene dang mo trong Editor
    /// chua nhan component (hoac bi mat khi merge).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class SecretHealTap : MonoBehaviour
    {
        private const string PagePath = "Page_4";
        private const string CupName = "coc";

        [SerializeField, Min(1)] private int requiredTaps = 7;
        [Tooltip("Hai lan tap cach nhau lau hon so giay nay thi dem lai tu 1.")]
        [SerializeField, Min(0.1f)] private float maxTapInterval = 1.5f;

        private Button button;
        private int tapCount;
        private float lastTapTime = -999f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            AttachTo(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachTo(scene);

        private static void AttachTo(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Button candidate in root.GetComponentsInChildren<Button>(true))
                {
                    if (candidate.name != CupName || candidate.transform.parent == null ||
                        candidate.transform.parent.name != PagePath)
                        continue;

                    if (candidate.GetComponent<SecretHealTap>() == null)
                        candidate.gameObject.AddComponent<SecretHealTap>();
                    return;
                }
            }
        }

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(OnTap);
            tapCount = 0;
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnTap);
        }

        private void OnTap()
        {
            float now = Time.unscaledTime;
            if (now - lastTapTime > maxTapInterval)
                tapCount = 0;

            lastTapTime = now;
            tapCount++;
            Debug.Log($"[SECRET] Tap coc {tapCount}/{requiredTaps}");

            if (tapCount < requiredTaps)
                return;

            tapCount = 0;

            PlayerConditionSystem condition = PlayerConditionSystem.Instance;
            if (condition == null)
            {
                Debug.LogWarning("[SECRET] Khong co PlayerConditionSystem, khong hoi mau duoc.");
                return;
            }

            if (condition.RestoreFullHp())
                EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.PlayerPotion);
            else
                Debug.Log("[SECRET] Mau da day san.");
        }
    }
}
