using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EternalClash.Monetization
{
    /// <summary>
    /// Nut "QCx2Item" tren Win popup: bam -> xem quang cao -> nhan doi toan bo
    /// vat pham dang hien trong o Vat_Pham. Moi tran chi dung duoc mot lan; xem
    /// xong thi nut an di, bo giua chung thi bam lai duoc.
    ///
    /// Tu gan moi khi load scene nen khong mat khi team merge lai scene Battle.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class DoubleRewardAdButton : MonoBehaviour
    {
        private const string ButtonName = "QCx2Item";
        private const string Placement = "x2_item";

        private Button button;
        private bool waitingForAd;

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
            if (!scene.IsValid())
                return;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Button candidate in root.GetComponentsInChildren<Button>(true))
                {
                    if (candidate.name != ButtonName)
                        continue;

                    if (candidate.GetComponent<DoubleRewardAdButton>() == null)
                        candidate.gameObject.AddComponent<DoubleRewardAdButton>();
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
            // Popup mo lai cho tran moi -> nut song lai.
            waitingForAd = false;
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
            button.interactable = !AlreadyDoubled();
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnClick);
        }

        private static bool AlreadyDoubled()
        {
            BattleResult.BattleResultFlowController flow = BattleResult.BattleResultFlowController.Instance;
            return flow != null && flow.RewardsDoubled;
        }

        private void OnClick()
        {
            if (waitingForAd || AlreadyDoubled())
            {
                button.interactable = false;
                return;
            }

            waitingForAd = true;
            button.interactable = false;

            AdsService.ShowRewarded(Placement, success =>
            {
                waitingForAd = false;

                BattleResult.BattleResultFlowController flow =
                    BattleResult.BattleResultFlowController.Instance;

                if (success && flow != null && flow.DoubleDisplayedRewards())
                {
                    // Da nhan doi: an nut di cho khoi bam lai.
                    gameObject.SetActive(false);
                    return;
                }

                // Khong xem xong (hoac khong con gi de nhan doi): cho bam lai.
                if (this != null)
                    button.interactable = !AlreadyDoubled();
            });
        }
    }
}
