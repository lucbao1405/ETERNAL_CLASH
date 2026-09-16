using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using EternalClash.Core.Save;
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
        private readonly List<TMP_Text> hpTexts = new List<TMP_Text>();

        // Thanh mau Stat/Hp: khung + ruot do tu HpBarSprites (dung chung voi Battle),
        // ruot co theo ti le mau. Thieu anh thi quay ve cach cu: to toi anh goc lam nen,
        // Hp_Fill dung chung anh goc.
        private const float HpEmptyTintFactor = 0.3f;
        private const float HpFillSpeed = 1.5f; // phan thanh moi giay
        private readonly List<Image> hpFills = new List<Image>();
        private float hpFillTarget = 1f;
        private bool hpFillInitialized;

        // Cum "Lv" trong panel Trang_Bi:
        //   Lv                       -> Slider hien tien do EXP
        //   Lv/lv text               -> nhan dang "Lv 5"
        //   Lv/So_Lv                 -> so dang "40/150"
        private readonly List<TMP_Text> levelLabelTexts = new List<TMP_Text>();
        private readonly List<TMP_Text> expTexts = new List<TMP_Text>();
        private readonly List<Slider> expSliders = new List<Slider>();

        // O chu hien chi phi tren mat tung nut nang cap.
        private readonly List<TMP_Text> costLabels = new List<TMP_Text>();

        private static readonly Color CostAffordableColor = new Color(1f, 0.95f, 0.6f);
        private static readonly Color CostBlockedColor = new Color(0.75f, 0.4f, 0.4f);

        private TMP_Text pointsText;

        // True khi o hien diem la object co san trong scene (vd "Point"). Luc do chi
        // ghi con so, vi nhan chu da duoc nguoi dung UI thiet ke san canh do roi.
        private bool pointsTextIsSceneObject;

        private bool initialized;
        private bool subscribed;
        private SaveManager subscribedSaveManager;

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
            AnimateHpFill();

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
            if (subscribedSaveManager != null)
                subscribedSaveManager.SaveChanged -= OnSaveDataChanged;
            subscribedSaveManager = null;

            if (PlayerStatSystem.Instance != null)
                PlayerStatSystem.Instance.OnStatsChanged -= Refresh;

            if (GoldSystem.Instance != null)
            {
                GoldSystem.Instance.OnGoldChanged -= OnCurrencyChanged;
                GoldSystem.Instance.OnGemChanged -= OnCurrencyChanged;
            }

            var condition = EternalClash.Core.PlayerConditionSystem.Instance;
            if (condition != null)
            {
                condition.OnRecoveredHpChanged -= OnRecoveredHpChanged;
                condition.OnRecoveryCompleted -= Refresh;
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
            SubscribeToSaveChanges();
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

            // Hoi mau khi bi thuong xay ra dan theo thoi gian, nen thanh HP phai
            // cap nhat theo chu khong chi ve mot lan luc mo Town.
            var condition = EternalClash.Core.PlayerConditionSystem.Instance;
            if (condition != null)
            {
                condition.OnRecoveredHpChanged -= OnRecoveredHpChanged;
                condition.OnRecoveredHpChanged += OnRecoveredHpChanged;
                condition.OnRecoveryCompleted -= Refresh;
                condition.OnRecoveryCompleted += Refresh;
            }

            subscribed = true;
        }

        private void SubscribeToSaveChanges()
        {
            SaveManager saveManager = SaveManager.Instance;
            if (saveManager == subscribedSaveManager)
                return;

            if (subscribedSaveManager != null)
                subscribedSaveManager.SaveChanged -= OnSaveDataChanged;

            subscribedSaveManager = saveManager;
            if (subscribedSaveManager != null)
                subscribedSaveManager.SaveChanged += OnSaveDataChanged;
        }

        private void OnSaveDataChanged(SaveData _) => Refresh();

        private void OnRecoveredHpChanged(int current, int max) => Refresh();

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
            CollectDirectChildTexts(scene, "Khung_Vang", goldTexts);
            CollectDirectChildTexts(scene, "Khung_Kim_Cuong", gemTexts);

            // --- Thanh mau: Stat/Hp/So_Hp ---
            CollectDirectChildTexts(scene, "Hp", hpTexts);
            foreach (Transform hp in FindAllInScene(scene, "Hp"))
            {
                Image fill = EnsureHpFill(hp);
                if (fill != null && !hpFills.Contains(fill))
                    hpFills.Add(fill);

                // Nhap nhay thanh mau khi bam GO ma chua du mau vao tran.
                if (hp.GetComponent<HpLowBlink>() == null)
                    hp.gameObject.AddComponent<HpLowBlink>();
            }

            // --- Cum "Lv": thanh EXP + nhan cap + so EXP ---
            foreach (Transform lv in FindAllInScene(scene, "Lv"))
            {
                Slider slider = lv.GetComponent<Slider>();
                if (slider != null && !expSliders.Contains(slider))
                    expSliders.Add(slider);

                // Ten co khoang trang va chu thuong dung nhu trong scene.
                Transform label = FindDescendant(lv, "lv text");
                TMP_Text labelText = label != null ? label.GetComponent<TMP_Text>() : null;
                if (labelText != null && !levelLabelTexts.Contains(labelText))
                    levelLabelTexts.Add(labelText);

                Transform expNumber = FindDescendant(lv, "So_Lv");
                TMP_Text expText = expNumber != null ? expNumber.GetComponent<TMP_Text>() : null;
                if (expText != null && !expTexts.Contains(expText))
                    expTexts.Add(expText);
            }

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

            Debug.Log($"[TownStat] Cum Lv: {expSliders.Count} thanh EXP, " +
                      $"{levelLabelTexts.Count} nhan 'lv text', {expTexts.Count} o 'So_Lv', " +
                      $"o diem={(pointsText != null ? pointsText.name : "KHONG CO")}.");

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

        /// <summary>
        /// Tao (hoac tim lai) lop Hp_Fill trong thanh mau, nam duoi chu so HP.
        /// </summary>
        private static Image EnsureHpFill(Transform hp)
        {
            Image background = hp != null ? hp.GetComponent<Image>() : null;
            if (background == null || background.sprite == null)
                return null;

            Transform existing = hp.Find(HpBarSprites.FillObjectName);
            if (existing != null)
                return existing.GetComponent<Image>();

            // Bo khung + ruot tach tu "UI blood": chi dung khi Hp dang dung dung anh do,
            // tranh thay nham neu sau nay co thanh mau thiet ke khac cung ten Hp.
            if (background.sprite.name == HpBarSprites.OriginalSpriteName)
            {
                Image split = HpBarSprites.ApplyTo(background);
                if (split != null)
                    return split;
            }

            // Cach cu: anh goc to toi lam nen, lop fill dung chung anh goc phu kin thanh.
            var go = new GameObject(HpBarSprites.FillObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = hp.gameObject.layer;

            var rect = (RectTransform)go.transform;
            rect.SetParent(hp, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            // Nam duoi chu So_Hp (con dau tien duoc ve truoc).
            rect.SetAsFirstSibling();

            Image fill = go.GetComponent<Image>();
            fill.sprite = background.sprite;
            fill.color = background.color;
            fill.material = background.material;
            fill.preserveAspect = background.preserveAspect;
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            Color c = background.color;
            background.color = new Color(c.r * HpEmptyTintFactor, c.g * HpEmptyTintFactor, c.b * HpEmptyTintFactor, c.a);

            return fill;
        }

        private void SetHpFillTarget(float ratio)
        {
            hpFillTarget = Mathf.Clamp01(ratio);

            // Lan dau mo Town: hien dung ngay, khong chay tu 100% xuong.
            if (hpFillInitialized)
                return;

            hpFillInitialized = true;
            foreach (Image fill in hpFills)
            {
                if (fill != null)
                    fill.fillAmount = hpFillTarget;
            }
        }

        /// <summary>Thanh mau chay mu dan toi gia tri moi (vd dang hoi mau theo dot).</summary>
        private void AnimateHpFill()
        {
            if (!hpFillInitialized)
                return;

            float step = HpFillSpeed * Time.unscaledDeltaTime;
            foreach (Image fill in hpFills)
            {
                if (fill != null && !Mathf.Approximately(fill.fillAmount, hpFillTarget))
                    fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, hpFillTarget, step);
            }
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
        /// <summary>
        /// Ten cac object trong scene co the dung lam cho hien so diem. Uu tien dung
        /// object co san do nguoi dung UI tao, chi khi khong co moi tu tao nhan moi.
        /// </summary>
        private static readonly string[] PointsObjectNames =
        {
            // Uu tien "Available_Point_Text" - o chu that su trong cum Lv cua panel
            // Trang_Bi. "Available_Point" chi la khung anh boc ngoai, khong co TMP.
            "Available_Point_Text",
            "Point", "Points", "So_Point", "Diem", "So_Diem", "StatPoint", "StatPoints"
        };

        private TMP_Text CreatePointsLabel(Transform staUpdate)
        {
            Scene scene = gameObject.scene;

            // 1) Uu tien object co san trong scene ten "Point" (hoac cac bien the).
            //    O chu co the nam ngay tren object do, hoac la con truc tiep cua no
            //    theo dung kieu Icon + Text ma UI nay dang dung o cho khac.
            foreach (string name in PointsObjectNames)
            {
                foreach (Transform candidate in FindAllInScene(scene, name))
                {
                    TMP_Text own = candidate.GetComponent<TMP_Text>();
                    if (own != null)
                    {
                        pointsTextIsSceneObject = true;
                        Debug.Log($"[TownStat] Hien so diem tren object co san '{name}'.");
                        return own;
                    }

                    TMP_Text child = FirstDirectChildText(candidate);
                    if (child != null)
                    {
                        pointsTextIsSceneObject = true;
                        Debug.Log($"[TownStat] Hien so diem tren con cua '{name}'.");
                        return child;
                    }
                }
            }

            // 2) Khong co thi tu tao nhan canh cum nut.
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
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.StatSelect);
            Debug.Log($"[TownStat] +1 {statName}. Diem con lai: {before} -> {stats.StatPoints}");
        }

        private void Refresh()
        {
            SaveData save = SaveManager.Instance?.Data;
            if (save?.currency != null)
            {
                Apply(goldTexts, save.currency.gold);
                Apply(gemTexts, save.currency.gem);
            }

            PlayerStatSystem stats = PlayerStatSystem.Instance;
            if (stats == null)
                return;

            Apply(strTexts, stats.Strength);
            Apply(intTexts, stats.Intelligence);
            Apply(vitTexts, stats.Vitality);
            Apply(luckTexts, stats.Luck);
            Apply(levelTexts, stats.Level);

            ApplyLevelGroup(stats);
            ApplyHealthTexts(stats);

            if (pointsText != null)
            {
                // O co san trong scene: chi ghi con so, giu nguyen phan nhan chu
                // va icon ma nguoi dung UI da dat canh do.
                pointsText.text = pointsTextIsSceneObject
                    ? stats.StatPoints.ToString()
                    : (stats.CanAllocate ? $"Diem cong: {stats.StatPoints}" : "Het diem cong");
            }

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

        /// <summary>
        /// Hien "mau hien tai / mau toi da". O Town khong co Player nao de hoi, nen:
        ///  - Mau toi da lay tu PlayerStatSystem.TotalMaxHealth (goc + bonus VIT).
        ///  - Mau hien tai lay tu PlayerConditionSystem neu no dang giu trang thai
        ///    thuong tich; khong thi coi nhu day mau.
        /// </summary>
        /// <summary>
        /// Cap nhat cum "Lv": nhan cap, so EXP va thanh tien do.
        /// RequiredExp la moc de len cap ke tiep, con CurrentExp la phan da tich
        /// duoc trong cap hien tai (AddExp tru bot moi lan len cap), nen ti le
        /// CurrentExp/RequiredExp chinh la do day cua thanh.
        /// </summary>
        private void ApplyLevelGroup(PlayerStatSystem stats)
        {
            for (int i = 0; i < levelLabelTexts.Count; i++)
            {
                if (levelLabelTexts[i] != null)
                    levelLabelTexts[i].text = $"Lv {stats.Level}";
            }

            int required = Mathf.Max(1, stats.RequiredExp);
            int current = Mathf.Clamp(stats.CurrentExp, 0, required);

            string line = $"{current}/{stats.RequiredExp}";
            for (int i = 0; i < expTexts.Count; i++)
            {
                if (expTexts[i] != null)
                    expTexts[i].text = line;
            }

            float ratio = Mathf.Clamp01((float)current / required);
            for (int i = 0; i < expSliders.Count; i++)
            {
                if (expSliders[i] == null)
                    continue;

                // Ep khoang gia tri ve 0..1 de khong phu thuoc cau hinh san trong scene.
                expSliders[i].minValue = 0f;
                expSliders[i].maxValue = 1f;
                expSliders[i].value = ratio;
            }
        }

        private void ApplyHealthTexts(PlayerStatSystem stats)
        {
            int maxHp = stats.TotalMaxHealth;
            int currentHp = maxHp;

            var condition = EternalClash.Core.PlayerConditionSystem.Instance;
            if (condition != null && condition.MaxHp > 0 && condition.IsInjured)
            {
                // Dang thuong tich: hien so mau that dang hoi, nhung van dung tran
                // la TotalMaxHealth de khong lech voi chi so VIT vua cong.
                currentHp = Mathf.Clamp(condition.CurrentHp, 0, maxHp);
            }

            SetHpFillTarget(maxHp > 0 ? (float)currentHp / maxHp : 1f);

            if (hpTexts.Count == 0)
                return;

            string line = $"{currentHp}/{maxHp}";
            for (int i = 0; i < hpTexts.Count; i++)
            {
                if (hpTexts[i] != null)
                    hpTexts[i].text = line;
            }
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
