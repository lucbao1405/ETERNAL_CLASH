using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EternalClash.Core;
using EternalClash.Enemy;

namespace EternalClash.UI
{
    /// <summary>
    /// Bam vao coc (Page_4/coc) o Town thi keo panel "select boss" tu tren man hinh xuong,
    /// dung SmoothSlide nen co dung hieu ung va am thanh PanelScroll giong cac panel khac.
    /// Bam 1 trong cac boss (slime/wolf/goblin mage/goblin boss) thi vao tran thu thach
    /// danh dung con do voi nhieu mau hon binh thuong (BossChallenge).
    /// Tu gan moi khi load scene de khong mat khi merge scene.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class BossSelectTap : MonoBehaviour
    {
        private const string PagePath = "Page_4";
        private const string CupName = "coc";
        private const string PanelName = "select boss";

        // Ten cac nut boss ben trong panel "select boss" cua scene Town.
        private static readonly string[] BossIds = { "slime", "wolf", "goblin mage", "goblin boss" };

        private Button button;
        private SmoothSlide bossPanel;
        private bool warned;

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

                    if (candidate.GetComponent<BossSelectTap>() == null)
                        candidate.gameObject.AddComponent<BossSelectTap>();
                    return;
                }
            }
        }

        private void Awake()
        {
            button = GetComponent<Button>();
            bossPanel = FindOrCreateSlide();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(OpenBossPanel);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OpenBossPanel);
        }

        private void OpenBossPanel()
        {
            if (bossPanel == null)
                bossPanel = FindOrCreateSlide();

            if (bossPanel == null)
            {
                if (!warned)
                {
                    warned = true;
                    Debug.LogWarning($"[BossSelectTap] Khong tim thay panel '{PanelName}' trong scene.", this);
                }
                return;
            }

            bossPanel.OpenPanel();
        }

        /// <summary>
        /// Them SmoothSlide cho panel neu chua co, voi thong so giong cac panel khac o Town
        /// (Setting, Bag, Shop... deu dung off 2000 / on 250 / speed 4500 / bounce 100).
        /// Neu panel da co SmoothSlide trong Editor thi giu nguyen thong so cua no.
        /// </summary>
        private SmoothSlide FindOrCreateSlide()
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                {
                    if (candidate.name != PanelName)
                        continue;

                    SmoothSlide slide = candidate.GetComponent<SmoothSlide>();
                    if (slide == null)
                    {
                        slide = candidate.gameObject.AddComponent<SmoothSlide>();
                        slide.offScreenPos = new Vector2(0f, 2000f);
                        slide.onScreenPos = new Vector2(0f, 250f);
                        slide.moveSpeed = 4500f;
                        slide.bounceDistance = 100f;
                    }

                    Button closeButton = candidate.Find("X")?.GetComponent<Button>();
                    if (closeButton != null)
                    {
                        closeButton.onClick.RemoveAllListeners();
                        closeButton.onClick.AddListener(slide.ClosePanel);
                    }

                    WireBossButtons(candidate);

                    return slide;
                }
            }

            return null;
        }

        /// <summary>Gan hanh dong vao tran cho tung nut boss ben trong panel (chi lan dau tu gan).</summary>
        private void WireBossButtons(Transform panel)
        {
            foreach (string id in BossIds)
            {
                Button bossButton = panel.Find(id)?.GetComponent<Button>();
                if (bossButton == null)
                    continue;

                string captured = id;
                bossButton.onClick.AddListener(() => StartBossFight(captured));
            }
        }

        private void StartBossFight(string enemyId)
        {
            // Chan nhu nut GO o Town: khong du mau thi nhap nhay HP va o lai.
            PlayerConditionSystem condition = PlayerConditionSystem.Instance;
            if (condition != null && !condition.CanStartBattle())
            {
                HpLowBlink.BlinkAll();
                Debug.Log("[TOWN] Chan vao tran boss: " + condition.GetInjuredBlockReason(), this);
                return;
            }

            BossChallenge.Start(enemyId);

            if (bossPanel != null)
                bossPanel.ClosePanel();

            // Phai ghi du: SceneLoader khong ghi du se trung voi SceneLoader
            // namespace toan cuc o "switch scene" thay vi EternalClash.Core.SceneLoader.
            EternalClash.Core.SceneLoader.LoadBattle();
        }
    }
}
