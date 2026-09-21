using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using EternalClash.Audio;
using EternalClash.Core;
using EternalClash.Village;

namespace EternalClash.UI
{
    /// <summary>
    /// Popup "Rest & Recover" kieu Postknight: khi bi thuong o Town, tra
    /// <see cref="gemCost"/> kim cuong de hoi day 100% HP ngay lap tuc. Chi co
    /// dung mot lua chon bang kim cuong (khong co lua chon xem quang cao).
    ///
    /// Panel "Rest_Recover" va nut mo "Rest_Recover_Open" duoc dat san trong
    /// scene Town boi menu Tools/Setup Rest &amp; Recover Popup - gia kim cuong,
    /// text, sprite deu sua duoc trong Inspector. Panel an mac dinh; trong luc
    /// bi thuong thi nut mo hien len (pop driver ben duoi tu quan ly), bam vao
    /// la mo popup.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RestRecoverPopup : MonoBehaviour
    {
        [Header("Cost")]
        [SerializeField, Min(0), Tooltip("So kim cuong de hoi day 100% HP.")]
        private int gemCost = 5;

        [Header("Widgets (dat san trong scene)")]
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button healButton;
        [SerializeField] private Button closeButton;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            AutoWireMissingReferences();

            if (healButton != null)
                healButton.onClick.AddListener(OnHealClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            // Hoi mau bang cach khac (thoi gian, coc bi mat) trong luc popup mo
            // thi dong popup lai cho dung so.
            PlayerConditionSystem cond = PlayerConditionSystem.Instance;
            if (cond != null)
                cond.OnRecoveryCompleted += OnRecoveryCompleted;
        }

        private void OnDisable()
        {
            PlayerConditionSystem cond = PlayerConditionSystem.Instance;
            if (cond != null)
                cond.OnRecoveryCompleted -= OnRecoveryCompleted;

            if (IsOpen)
            {
                IsOpen = false;
                PanelDim.Release(this);
            }
        }

        // ------------------------------------------------------------------
        // Open / close
        // ------------------------------------------------------------------

        public void Show()
        {
            IsOpen = true;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);

            if (costText != null)
                costText.text = gemCost.ToString();

            PanelDim.Acquire(this);
        }

        public void Close()
        {
            if (!IsOpen && !gameObject.activeSelf)
                return;

            IsOpen = false;
            PanelDim.Release(this);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Diem vao cho cac nut "nghi ngoi" (giuong, nha ve sinh...): du mau thi
        /// bao voi nguoi choi, con thuong thi mo popup.
        /// </summary>
        public static void TryOpenFromRest()
        {
            PlayerConditionSystem cond = PlayerConditionSystem.Instance;
            if (cond != null && cond.MaxHp > 0 && cond.CurrentHp >= cond.MaxHp)
            {
                ToastMessage.Show("Health is full.");
                return;
            }

            RestRecoverPopup popup = FindObjectOfType<RestRecoverPopup>(true);
            if (popup != null)
                popup.Show();
            else
                Debug.LogWarning("[RestRecover] Khong tim thay panel Rest_Recover trong scene.");
        }

        private void OnHealClicked()
        {
            PlayerConditionSystem cond = PlayerConditionSystem.Instance;
            if (cond == null)
            {
                Debug.LogWarning("[RestRecover] Khong co PlayerConditionSystem, khong hoi mau duoc.");
                return;
            }

            if (cond.MaxHp > 0 && cond.CurrentHp >= cond.MaxHp)
            {
                Close();
                return;
            }

            GoldSystem gold = GoldSystem.Instance;
            if (gold == null || !gold.SpendGem(gemCost))
            {
                ToastMessage.Show("Not enough diamonds!");
                return;
            }

            cond.RestoreFullHp();
            GameAudio.Play(SoundId.PlayerPotion);
            Close();
        }

        private void OnRecoveryCompleted()
        {
            if (IsOpen)
                Close();
        }

        // ------------------------------------------------------------------
        // Bootstrap + helpers
        // ------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            AttachTo(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single)
                return;

            AttachTo(scene);
        }

        private static void AttachTo(Scene scene)
        {
            if (!string.Equals(scene.name, "Town", StringComparison.OrdinalIgnoreCase))
                return;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform canvas = FindDeepChild(root.transform, "Canvas");
                if (canvas == null)
                    continue;

                // Panel chua component thi them vao (chong mat khi merge).
                Transform panel = FindDeepChild(canvas, "Rest_Recover");
                if (panel != null && panel.GetComponent<RestRecoverPopup>() == null)
                    panel.gameObject.AddComponent<RestRecoverPopup>();

                // Driver dieu khien nut mo + tu dong mo popup; dat tren Canvas
                // (object luon active) vi panel an mac dinh.
                if (canvas.GetComponent<RestRecoverDriver>() == null)
                    canvas.gameObject.AddComponent<RestRecoverDriver>();
                return;
            }
        }

        /// <summary>Truong hop mat lien ket (merge): tim lai widget con theo ten.</summary>
        private void AutoWireMissingReferences()
        {
            if (healButton == null)
                healButton = FindComponentInChildren<Button>(transform, "HealButton");
            if (closeButton == null)
                closeButton = FindComponentInChildren<Button>(transform, "CloseButton");
            if (costText == null)
                costText = FindComponentInChildren<TMP_Text>(transform, "CostText");
        }

        private static T FindComponentInChildren<T>(Transform root, string name) where T : Component
        {
            Transform found = FindDeepChild(root, name);
            return found != null ? found.GetComponent<T>() : null;
        }

        internal static Transform FindDeepChild(Transform root, string name)
        {
            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }

    /// <summary>
    /// Chay tren Canvas (luon active): hien nut mo khi HP duoi 60% Max HP
    /// (xem <see cref="PlayerConditionSystem.CanOfferGemHeal"/>), tu dong mo
    /// popup 1 lan khi vao Town ma van duoi nguong. Duoc gan tu dong khi load
    /// scene Town (xem <see cref="RestRecoverPopup.AttachTo"/>).
    /// </summary>
    public sealed class RestRecoverDriver : MonoBehaviour
    {
        private const string OpenButtonName = "Rest_Recover_Open";
        private const float AutoShowDelay = 0.6f;

        [SerializeField, Tooltip("Tu dong mo popup 1 lan moi lan ve lang khi con bi thuong.")]
        private bool autoShowOnInjuredArrival = true;

        private RestRecoverPopup popup;
        private Button openBubble;
        private Coroutine autoShowRoutine;

        private void Start()
        {
            popup = FindObjectOfType<RestRecoverPopup>(true);
            if (popup == null)
            {
                Debug.LogWarning("[RestRecover] Khong tim thay panel Rest_Recover - tinh nang nghi ngoi tat.");
                enabled = false;
                return;
            }

            Transform found = RestRecoverPopup.FindDeepChild(transform, OpenButtonName);
            openBubble = found != null ? found.GetComponent<Button>() : null;
            if (openBubble != null)
                openBubble.onClick.AddListener(ShowPopup);

            if (autoShowOnInjuredArrival)
                autoShowRoutine = StartCoroutine(AutoShowWhenInjured());
        }

        private void OnDestroy()
        {
            if (openBubble != null)
                openBubble.onClick.RemoveListener(ShowPopup);
        }

        private void Update()
        {
            if (openBubble == null)
                return;

            PlayerConditionSystem cond = PlayerConditionSystem.Instance;

            // Duoi 60% Max HP thi moi hien nut mo luong hoi bang kim cuong.
            bool showBubble = cond != null && cond.CanOfferGemHeal && !popup.IsOpen;
            if (openBubble.gameObject.activeSelf != showBubble)
                openBubble.gameObject.SetActive(showBubble);
        }

        private void ShowPopup()
        {
            popup.Show();
        }

        private IEnumerator AutoShowWhenInjured()
        {
            yield return new WaitForSecondsRealtime(AutoShowDelay);

            autoShowRoutine = null;
            PlayerConditionSystem cond = PlayerConditionSystem.Instance;

            // Duoi 60% Max HP khi vao Town thi tu mo popup 1 lan (thua luon dung,
            // thang ma ve lang guc duoi nguong cung duoc offer).
            if (cond != null && cond.ShouldAutoShowRecoveryPopup && cond.CanOfferGemHeal && !popup.IsOpen && cond.ConsumeAutoShowRecoveryPopup())
                popup.Show();
        }
    }
}
