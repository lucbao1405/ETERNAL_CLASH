using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using EternalClash.Village;

namespace EternalClash.UI
{
    /// <summary>
    /// Noi 4 nut trong "Sta_Update" voi PlayerStatSystem, va cap nhat cac o so
    /// trong MOI panel "Sta" cua scene Town.
    ///
    /// Town co hai panel "Sta" hien cung bo chi so:
    ///   Canvas/Down_Panel/Stat/Sta            (thanh duoi man hinh)
    ///   Canvas/Man_Hinh_Khac/Trang_Bi/Sta     (nam ngay canh Sta_Update)
    /// Ca hai deu duoc cap nhat nen bam nut o panel nao cung thay so doi ngay.
    ///
    /// Tim doi tuong theo ten luc chay (giong BattlePopupController) nen KHONG can
    /// keo vao scene va KHONG can gan gi trong Inspector.
    /// </summary>
    public sealed class TownStatPanelController : MonoBehaviour
    {
        private const string TownSceneName = "Town";

        public static TownStatPanelController Instance { get; private set; }

        private Button strButton;
        private Button intButton;
        private Button vitButton;
        private Button luckButton;

        // Nhieu panel cung hien mot chi so -> giu danh sach thay vi mot tham chieu.
        private readonly List<TMP_Text> strTexts = new List<TMP_Text>();
        private readonly List<TMP_Text> intTexts = new List<TMP_Text>();
        private readonly List<TMP_Text> vitTexts = new List<TMP_Text>();
        private readonly List<TMP_Text> luckTexts = new List<TMP_Text>();
        private readonly List<TMP_Text> levelTexts = new List<TMP_Text>();

        private readonly List<TMP_Text> goldTexts = new List<TMP_Text>();
        private readonly List<TMP_Text> gemTexts = new List<TMP_Text>();

        // O chu hien chi phi tren mat tung nut nang cap.
        private readonly List<TMP_Text> costLabels = new List<TMP_Text>();

        private static readonly Color CostAffordableColor = new Color(1f, 0.95f, 0.6f);
        private static readonly Color CostBlockedColor = new Color(0.75f, 0.4f, 0.4f);

        private TMP_Text pointsText;
        private bool initialized;
        private bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            // RuntimeInitializeOnLoadMethod chi chay DUNG MOT LAN sau scene dau tien.
            // Controller nay lai khong DontDestroyOnLoad nen bi huy moi khi roi Town.
            // Ket qua: khoi dong o MainMenu/Battle thi no khong bao gio duoc tao, va
            // di Town -> Battle -> Town thi cung khong duoc tao lai - cac o so giu
            // nguyen chu placeholder "33" trong scene va 4 nut khong duoc noi.
            // Vi vay phai bat sceneLoaded de kiem tra lai moi lan doi scene.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            EnsureInTownScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureInTownScene();
        }

        private static void EnsureInTownScene()
        {
            if (Instance != null)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !string.Equals(scene.name, TownSceneName, StringComparison.OrdinalIgnoreCase))
                return;

            if (FindObjectOfType<TownStatPanelController>() != null)
                return;

            new GameObject("TownStatPanelController (Runtime)")
                .AddComponent<TownStatPanelController>();

            Debug.Log("[TownStat] Da tu tao controller cho scene Town.");
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            Initialize();
        }

        private void Start()
        {
            Subscribe();
            Refresh();
        }

        private void Update()
        {
            // GameBootstrap tao PlayerStatSystem o BeforeSceneLoad nen thuong da co
            // san luc Start(). Neu vi ly do nao do no xuat hien muon hon thi thu
            // dang ky lai cho toi khi duoc, roi thoi khong kiem tra nua.
            if (subscribed || PlayerStatSystem.Instance == null)
                return;

            Subscribe();
            Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (PlayerStatSystem.Instance != null)
                PlayerStatSystem.Instance.OnStatsChanged -= Refresh;

            if (GoldSystem.Instance != null)
            {
                GoldSystem.Instance.OnGoldChanged -= OnCurrencyChanged;
                GoldSystem.Instance.OnGemChanged -= OnCurrencyChanged;
            }

            // Danh dau da go, de OnEnable/Update dang ky lai duoc.
            subscribed = false;
        }

        // GoldSystem phat su kien kem gia tri, con Refresh() khong nhan tham so.
        private void OnCurrencyChanged(int _) => Refresh();

        private void OnDestroy()
        {
            UnwireButtons();

            if (Instance == this)
                Instance = null;
        }

        private void Subscribe()
        {
            if (PlayerStatSystem.Instance == null)
                return;

            // Go truoc roi dang ky lai de khong bi dang ky trung.
            PlayerStatSystem.Instance.OnStatsChanged -= Refresh;
            PlayerStatSystem.Instance.OnStatsChanged += Refresh;

            if (GoldSystem.Instance != null)
            {
                GoldSystem.Instance.OnGoldChanged -= OnCurrencyChanged;
                GoldSystem.Instance.OnGoldChanged += OnCurrencyChanged;
                GoldSystem.Instance.OnGemChanged -= OnCurrencyChanged;
                GoldSystem.Instance.OnGemChanged += OnCurrencyChanged;
            }

            subscribed = true;
        }

        private void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
            Scene scene = gameObject.scene;

            // --- 4 nut cong diem ---
            Transform staUpdate = FindFirstInScene(scene, "Sta_Update");
            if (staUpdate != null)
            {
                strButton = FindChildButton(staUpdate, "Str");
                intButton = FindChildButton(staUpdate, "Int");
                vitButton = FindChildButton(staUpdate, "Vit");
                luckButton = FindChildButton(staUpdate, "Luc");
            }
            else
            {
                Debug.LogWarning("[TownStat] Khong tim thay 'Sta_Update' trong scene Town.");
            }

            // --- O so trong TAT CA panel "Sta" ---
            foreach (Transform sta in FindAllInScene(scene, "Sta"))
            {
                CollectValueText(sta, "Str", strTexts);
                CollectValueText(sta, "Int", intTexts);
                CollectValueText(sta, "Vit", vitTexts);
                CollectValueText(sta, "Luc", luckTexts);
            }

            // --- So Level trong tat ca panel "Level" ---
            foreach (Transform level in FindAllInScene(scene, "Level"))
            {
                TMP_Text t = FirstDirectChildText(level);
                if (t != null)
                    levelTexts.Add(t);
            }

            // --- Vang va Kim cuong tren thanh tren cung ---
            // Cau truc giong cac nhom chi so: Icon + mot o chu la con truc tiep.
            CollectDirectChildTexts(scene, "Vang", goldTexts);
            CollectDirectChildTexts(scene, "Kim_Cuong", gemTexts);

            pointsText = CreatePointsLabel(staUpdate);

            // O chu chi phi tren mat 4 nut. Dung mot o so co san lam mau de thua
            // huong font, mau va kich co chu cua UI hien tai.
            TMP_Text sample = strTexts.Count > 0 ? strTexts[0] : null;
            costLabels.Clear();
            AddCostLabel(strButton, sample);
            AddCostLabel(intButton, sample);
            AddCostLabel(vitButton, sample);
            AddCostLabel(luckButton, sample);

            WireButtons();

            // Bao ro da noi duoc nhung gi, de khi "bam nut khong len" con biet
            // la hong o khau tim doi tuong hay o khau cong diem.
            int buttons = (strButton != null ? 1 : 0) + (intButton != null ? 1 : 0)
                        + (vitButton != null ? 1 : 0) + (luckButton != null ? 1 : 0);
            int values = strTexts.Count + intTexts.Count + vitTexts.Count + luckTexts.Count;

            Debug.Log($"[TownStat] Da noi {buttons}/4 nut, {values} o so, {levelTexts.Count} o Level, " +
                      $"{goldTexts.Count} o Vang, {gemTexts.Count} o Kim cuong.");

            if (buttons < 4)
                Debug.LogWarning("[TownStat] Thieu nut. Kiem tra ten con cua 'Sta_Update' " +
                                 "phai dung la Str / Int / Vit / Luc va co component Button.");

            if (values == 0)
                Debug.LogWarning("[TownStat] Khong tim thay o so nao trong panel 'Sta'.");
        }

        /// <summary>
        /// Lay o hien so cua mot chi so. Cau truc moi nhom la:
        ///     Str
        ///      ├── Icon          -> chu "Str" nam ben trong, chi de ghi nhan
        ///      └── So_Str        -> o hien so, la con TRUC TIEP
        /// Nen quy tac la: con truc tiep dau tien co TMP_Text chinh la o so.
        /// Cach nay khong phu thuoc vao ten (So_Str, So_Vit hay "Text (TMP)" deu duoc).
        /// </summary>
        private static void CollectValueText(Transform staRoot, string groupName, List<TMP_Text> into)
        {
            Transform group = FindDescendant(staRoot, groupName);
            if (group == null)
                return;

            TMP_Text text = FirstDirectChildText(group);
            if (text != null && !into.Contains(text))
                into.Add(text);
        }

        private static void CollectDirectChildTexts(Scene scene, string objectName, List<TMP_Text> into)
        {
            foreach (Transform t in FindAllInScene(scene, objectName))
            {
                TMP_Text text = FirstDirectChildText(t);
                if (text != null && !into.Contains(text))
                    into.Add(text);
            }
        }

        /// <summary>
        /// Tao (hoac tim lai) o chu hien chi phi ngay tren mat nut. Cac nut trong
        /// Sta_Update la nut icon khong co con nao, nen o chu duoc them lam con cua
        /// chinh nut - Layout Group cua Sta_Update chi sap xep cac nut chu khong dung
        /// toi con ben trong nut, nen khong bi xo lech.
        /// </summary>
        private void AddCostLabel(Button button, TMP_Text sample)
        {
            TMP_Text label = EnsureButtonCostLabel(button, sample);
            if (label != null)
                costLabels.Add(label);
        }

        private TMP_Text EnsureButtonCostLabel(Button button, TMP_Text sample)
        {
            if (button == null || sample == null)
                return null;

            Transform existing = FindDescendant(button.transform, "CostText");
            if (existing != null)
                return existing.GetComponent<TMP_Text>();

            TMP_Text label = Instantiate(sample, button.transform);
            label.name = "CostText";

            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();

            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.fontStyle = FontStyles.Bold;

            // Khong chan tia raycast, neu khong o chu se nuot cu bam vao nut.
            label.raycastTarget = false;
            return label;
        }

        private static TMP_Text FirstDirectChildText(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                TMP_Text text = parent.GetChild(i).GetComponent<TMP_Text>();
                if (text != null)
                    return text;
            }

            return null;
        }

        /// <summary>
        /// Tao nhan "Diem cong: N" phia tren cum nut. Sta_Update co Layout Group nen
        /// nhan duoc dat lam con cua panel cha, khong chen vao trong cum nut.
        /// </summary>
        private TMP_Text CreatePointsLabel(Transform staUpdate)
        {
            if (staUpdate == null)
                return null;

            Transform parent = staUpdate.parent != null ? staUpdate.parent : staUpdate;

            Transform existing = FindDescendant(parent, "StatPointsText");
            if (existing != null)
                return existing.GetComponent<TMP_Text>();

            TMP_Text sample = strTexts.Count > 0 ? strTexts[0] : null;
            if (sample == null)
                return null;

            TMP_Text label = Instantiate(sample, parent);
            label.name = "StatPointsText";

            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600f, 90f);
            rect.anchoredPosition = new Vector2(0f, 1120f);
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();

            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(1f, 0.85f, 0.3f);
            label.raycastTarget = false;
            return label;
        }

        private void WireButtons()
        {
            if (strButton != null) strButton.onClick.AddListener(OnAddStrength);
            if (intButton != null) intButton.onClick.AddListener(OnAddIntelligence);
            if (vitButton != null) vitButton.onClick.AddListener(OnAddVitality);
            if (luckButton != null) luckButton.onClick.AddListener(OnAddLuck);
        }

        private void UnwireButtons()
        {
            if (strButton != null) strButton.onClick.RemoveListener(OnAddStrength);
            if (intButton != null) intButton.onClick.RemoveListener(OnAddIntelligence);
            if (vitButton != null) vitButton.onClick.RemoveListener(OnAddVitality);
            if (luckButton != null) luckButton.onClick.RemoveListener(OnAddLuck);
        }

        // PlayerStatSystem tu tru diem, tinh lai chi so, luu save va phat
        // OnStatsChanged -> Refresh() chay ngay sau do.
        private void OnAddStrength() => Allocate("STR", s => s.AllocateStrength());
        private void OnAddIntelligence() => Allocate("INT", s => s.AllocateIntelligence());
        private void OnAddVitality() => Allocate("VIT", s => s.AllocateVitality());
        private void OnAddLuck() => Allocate("LUCK", s => s.AllocateLuck());

        /// <summary>
        /// Goi ham cong diem va bao ro ket qua ra Console. AllocateXxx() cua
        /// PlayerStatSystem chi "return" lang le khi het diem, nen neu khong log
        /// thi bam nut se khong co phan hoi nao va rat kho doan nguyen nhan.
        /// </summary>
        private void Allocate(string statName, Action<PlayerStatSystem> action)
        {
            PlayerStatSystem stats = PlayerStatSystem.Instance;

            if (stats == null)
            {
                Debug.LogError("[TownStat] Bam " + statName +
                               " nhung PlayerStatSystem.Instance dang null. " +
                               "GameBootstrap chua chay hoac bi loi.");
                return;
            }

            if (!stats.CanAllocate)
            {
                Debug.LogWarning("[TownStat] Bam " + statName +
                                 " nhung khong con diem cong (StatPoints = 0). " +
                                 "Can len cap de co them diem.");
                Refresh();
                return;
            }

            int before = stats.StatPoints;
            action(stats);
            Debug.Log($"[TownStat] +1 {statName}. Diem con lai: {before} -> {stats.StatPoints}");
        }

        private void Refresh()
        {
            GoldSystem gold = GoldSystem.Instance;
            if (gold != null)
            {
                Apply(goldTexts, gold.Gold);
                Apply(gemTexts, gold.Gem);
            }

            PlayerStatSystem stats = PlayerStatSystem.Instance;
            if (stats == null)
                return;

            Apply(strTexts, stats.Strength);
            Apply(intTexts, stats.Intelligence);
            Apply(vitTexts, stats.Vitality);
            Apply(luckTexts, stats.Luck);
            Apply(levelTexts, stats.Level);

            if (pointsText != null)
                pointsText.text = stats.CanAllocate
                    ? $"Diem cong: {stats.StatPoints}"
                    : "Het diem cong";

            // Chi phi hien tren mat tung nut. Doi mau khi khong du diem de nguoi choi
            // thay ngay la dang thieu chu khong phai nut hong.
            bool canAfford = stats.CanAllocate;
            string cost = stats.StatPointCost.ToString();
            for (int i = 0; i < costLabels.Count; i++)
            {
                if (costLabels[i] == null)
                    continue;

                costLabels[i].text = cost;
                costLabels[i].color = canAfford ? CostAffordableColor : CostBlockedColor;
            }

            // Het diem thi lam mo 4 nut de nguoi choi biet khong bam duoc nua.
            bool hasPoints = stats.CanAllocate;
            if (strButton != null) strButton.interactable = hasPoints;
            if (intButton != null) intButton.interactable = hasPoints;
            if (vitButton != null) vitButton.interactable = hasPoints;
            if (luckButton != null) luckButton.interactable = hasPoints;
        }

        private static void Apply(List<TMP_Text> texts, int value)
        {
            string s = value.ToString();
            for (int i = 0; i < texts.Count; i++)
            {
                if (texts[i] != null)
                    texts[i].text = s;
            }
        }

        // ------------------------------------------------------------------
        // Tim doi tuong theo ten, bao gom ca doi tuong dang tat
        // ------------------------------------------------------------------

        private static Transform FindFirstInScene(Scene scene, string name)
        {
            foreach (Transform t in FindAllInScene(scene, name))
                return t;

            return null;
        }

        private static List<Transform> FindAllInScene(Scene scene, string name)
        {
            List<Transform> found = new List<Transform>();
            if (!scene.IsValid())
                return found;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name)
                        found.Add(t);
                }
            }

            return found;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
                return null;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t != root && t.name == name)
                    return t;
            }

            return null;
        }

        private static Button FindChildButton(Transform parent, string name)
        {
            Transform child = FindDescendant(parent, name);
            return child != null ? child.GetComponent<Button>() : null;
        }
    }
}
