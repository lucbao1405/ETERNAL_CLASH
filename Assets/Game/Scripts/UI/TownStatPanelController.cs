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

        // Cum "Potion" trong panel Trang_Bi: chi so thuoc do Phu thuy nang cap.
        //   Potion/Healing_Potion/Text   -> luong hoi phuc (Heal + bonus %)
        //   Potion/Cooldown_Potion/Text  -> hoi chieu thuoc (giam theo cap)
        //   Potion/Defense_Potion/Text   -> luc khiên thuoc
        private TMP_Text potionHealText;
        private TMP_Text potionCooldownText;
        private TMP_Text potionShieldText;

        private TMP_Text pointsText;

        // True khi o hien diem la object co san trong scene (vd "Point"). Luc do chi
        // ghi con so, vi nhan chu da duoc nguoi dung UI thiet ke san canh do roi.
        private bool pointsTextIsSceneObject;

        private bool initialized;
        private bool subscribed;
        private SaveManager subscribedSaveManager;

        [Header("Reset Stats (de trong de tu tao runtime)")]
        [SerializeField] private Button resetStatsButton;
        [SerializeField] private TMP_Text resetCostText;
        [SerializeField] private string resetButtonText = "Reset Points";
        [SerializeField] private Color resetButtonColor = new Color(0.75f, 0.25f, 0.2f, 1f);
        [SerializeField] private Vector2 resetButtonSize = new Vector2(220f, 56f);
        [SerializeField] private float resetButtonBottomOffset = 16f;

        [Header("Reset Confirmation (English)")]
        [SerializeField] private string confirmTitleText = "Reset Stat Points";
        [SerializeField] private string confirmMessageFormat = "Reset all stat points to base values?\nThis action costs {0} gold.";
        [SerializeField] private string confirmMessageFree = "Reset all stat points to base values?\nThis action is free this time.";
        [SerializeField] private string confirmYesText = "Confirm";
        [SerializeField] private string confirmNoText = "Cancel";
        [SerializeField] private Color confirmPanelColor = new Color(0.13f, 0.13f, 0.16f, 0.97f);
        [SerializeField] private Color confirmYesColor = new Color(0.2f, 0.5f, 0.25f, 1f);
        [SerializeField] private Color confirmNoColor = new Color(0.55f, 0.2f, 0.2f, 1f);
        [SerializeField] private Vector2 confirmPanelSize = new Vector2(620f, 360f);

        private GameObject confirmDialog;

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

            if (resetStatsButton != null)
                resetStatsButton.onClick.RemoveListener(OnClick_ResetStats);

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
                // Bo qua hang "Level" cua profile panel (ThongTin/Level): hang nay co
                // con truc tiep "Level_Number" va duoc TownProfilePanelController quan ly
                // (ghi SaveData.level vao Level_Number/Number). Neu khong bo qua,
                // stats.Level bi ghi de vao o chu nhan "Level" o cot ben trai.
                if (level.Find("Level_Number") != null)
                    continue;

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

            // --- Chi so thuoc Phu thuy trong panel Trang_Bi ---
            foreach (Transform potion in FindAllInScene(scene, "Potion"))
            {
                // Cu the canh Potion/Healing_Potion nay chi co trong Trang_Bi;
                // bo qua node "Potion" khac khong dung cau truc.
                if (FindDescendant(potion, "Healing_Potion") == null)
                    continue;

                potionHealText = FindDescendant(potion, "Healing_Potion")?.GetComponentInChildren<TMP_Text>(true);
                potionCooldownText = FindDescendant(potion, "Cooldown_Potion")?.GetComponentInChildren<TMP_Text>(true);
                potionShieldText = FindDescendant(potion, "Defense_Potion")?.GetComponentInChildren<TMP_Text>(true);
            }

            pointsText = CreatePointsLabel(staUpdate);

            // Khong hien so chi phi tren mat nut nua: xoa o chu cu neu con sot lai.
            RemoveCostLabel(strButton);
            RemoveCostLabel(intButton);
            RemoveCostLabel(vitButton);
            RemoveCostLabel(luckButton);

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

            SetupResetStatsButton();
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
        /// Xoa o chu "CostText" tung duoc tao tren mat nut cong diem.
        /// </summary>
        private static void RemoveCostLabel(Button button)
        {
            if (button == null)
                return;

            Transform existing = FindDescendant(button.transform, "CostText");
            if (existing != null)
                Destroy(existing.gameObject);
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
                Apply(goldTexts, save.currency.gold, FormatCurrency);
                Apply(gemTexts, save.currency.gem, FormatCurrency);
            }

            PlayerStatSystem stats = PlayerStatSystem.Instance;
            if (stats == null)
                return;

            Apply(strTexts, stats.BaseStrength);
            Apply(intTexts, stats.BaseIntelligence);
            Apply(vitTexts, stats.BaseVitality);
            Apply(luckTexts, stats.BaseLuck);
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

            // Het diem thi lam mo 4 nut de nguoi choi biet khong bam duoc nua.
            bool hasPoints = stats.CanAllocate;
            if (strButton != null) strButton.interactable = hasPoints;
            if (intButton != null) intButton.interactable = hasPoints;
            if (vitButton != null) vitButton.interactable = hasPoints;
            if (luckButton != null) luckButton.interactable = hasPoints;

            ApplyPotionStats();
            RefreshResetCost();
        }

        // ==================== Reset Stats ====================

        private void SetupResetStatsButton()
        {
            if (resetStatsButton == null)
                resetStatsButton = CreateRuntimeResetButton();

            if (resetStatsButton == null)
                return;

            resetStatsButton.onClick.RemoveListener(OnClick_ResetStats);
            resetStatsButton.onClick.AddListener(OnClick_ResetStats);

            if (resetCostText == null)
                resetCostText = resetStatsButton.GetComponentInChildren<TMP_Text>(true);

            RefreshResetCost();
        }

        /// <summary>
        /// Tim dung panel "Trang_Bi" chinh (co con "Sta" theo cau truc
        /// Canvas/Man_Hinh_Khac/Trang_Bi/Sta), chon rect lon nhat de tranh nham
        /// cac object trung ten nho khac.
        /// </summary>
        private Transform FindTrangBiPanel()
        {
            Scene scene = gameObject.scene;
            Transform best = null;
            float bestArea = -1f;

            foreach (Transform candidate in FindAllInScene(scene, "Trang_Bi"))
            {
                if (FindDescendant(candidate, "Sta") == null)
                    continue;

                var rect = (RectTransform)candidate;
                float area = rect.rect.width * rect.rect.height;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = candidate;
                }
            }

            return best;
        }

        /// <summary>
        /// Tao nut tai runtime khi khong ai gan trong Inspector. Nut nam duoi cuoi
        /// panel "Trang_Bi" (Canvas/Man_Hinh_Khac/Trang_Bi).
        /// </summary>
        private Button CreateRuntimeResetButton()
        {
            Transform trangBi = FindTrangBiPanel();
            if (trangBi == null)
                return null;

            Transform close = trangBi.Find("X");
            if (close == null)
                close = FindDescendant(trangBi, "X");
            if (close == null)
                return null;

            // Gan nut lam con cua nut "X": cung context canvas voi X nen bao gio
            // bi le chinh giua man hinh, bi che boi thanh duoi hay sai camera.
            GameObject go = new GameObject("ResetStatsButton", typeof(RectTransform),
                typeof(Image), typeof(Button));
            go.layer = close.gameObject.layer;
            go.transform.SetParent(close, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(((RectTransform)close).rect.width + resetButtonBottomOffset, 0f);
            rect.sizeDelta = resetButtonSize;

            // Canvas rieng voi overrideSorting: Down_Panel la sibling sau
            // Man_Hinh_Khac nen ve de len panel; canvas con nay dam bao nut
            // luon duoc ve tren cung.
            var overlay = go.AddComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = 500;
            overlay.worldCamera = trangBi.GetComponentInParent<Canvas>().rootCanvas.worldCamera;
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            go.GetComponent<Image>().color = resetButtonColor;

            GameObject label = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            label.transform.SetParent(go.transform, false);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var tmp = label.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.text = resetButtonText;
            tmp.fontSize = 20;
            tmp.enableWordWrapping = true;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return go.GetComponent<Button>();
        }

        /// <summary>
        /// Panel "Trang_Bi" co rect cao gan nhu toan man hinh nhung phan thuc te
        /// hien thi bi thanh Down_Panel (HP, thanh mau) che phia duoi. Do lech tu
        /// day rect len dinh cua Down_Panel de nut khong bi che.
        /// </summary>
        private static float ComputeVisibleBottomOffset(RectTransform panelRect, float margin)
        {
            Transform downPanel = null;
            Scene scene = SceneManager.GetActiveScene();
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.gameObject.scene != scene || !string.Equals(t.name, "Down_Panel", StringComparison.Ordinal))
                    continue;
                downPanel = t;
                break;
            }

            if (downPanel == null)
                return margin;

            var dpRect = (RectTransform)downPanel;
            var corners = new Vector3[4];
            dpRect.GetWorldCorners(corners);
            Vector2 topCenterScreen = RectTransformUtility.WorldToScreenPoint(null, corners[1]);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    panelRect, topCenterScreen, null, out Vector2 local))
                return local.y + margin;

            return margin;
        }

        private void OnClick_ResetStats()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            ShowResetConfirmation();
        }

        /// <summary>
        /// Hop xac nhan (tieng Anh) truoc khi tuyen thao tac tuyen diem khong hoan tac.
        /// </summary>
        private void ShowResetConfirmation()
        {
            int cost = PlayerStatSystem.Instance != null ? PlayerStatSystem.Instance.GetResetCost() : 0;

            if (confirmDialog == null)
                confirmDialog = CreateConfirmDialog();

            if (confirmDialog == null)
                return;

            Transform message = confirmDialog.transform.Find("Panel/Message");
            var text = message != null ? message.GetComponent<TMPro.TextMeshProUGUI>() : null;
            if (text != null)
                text.text = cost > 0 ? string.Format(confirmMessageFormat, cost) : confirmMessageFree;

            var buttons = confirmDialog.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                b.onClick.RemoveAllListeners();
                if (b.name == "ConfirmButton")
                    b.onClick.AddListener(ConfirmResetStats);
                else if (b.name == "CancelButton")
                    b.onClick.AddListener(HideResetConfirmation);
            }

            confirmDialog.SetActive(true);
            confirmDialog.transform.SetAsLastSibling();
        }

        private void ConfirmResetStats()
        {
            HideResetConfirmation();
            PlayerStatSystem.Instance?.ResetStats();
            RefreshResetCost();
        }

        private void HideResetConfirmation()
        {
            if (confirmDialog != null)
                confirmDialog.SetActive(false);
        }

        private void RefreshResetCost()
        {
            if (resetCostText == null)
                return;

            int cost = PlayerStatSystem.Instance != null ? PlayerStatSystem.Instance.GetResetCost() : 0;
            string costLabel = cost == 0 ? "Free" : $"{cost} Gold";

            // Neu o chi phi la chinh label cua nut (truong hop tu tao runtime) thi
            // gop ten nut va chi phi cung mot dong; neu la o rieng thi chi ghi gia.
            bool labelIsOnButton = resetStatsButton != null &&
                                   resetCostText.transform.IsChildOf(resetStatsButton.transform);
            resetCostText.text = labelIsOnButton
                ? $"{resetButtonText}\n{costLabel}"
                : costLabel;

            if (resetStatsButton != null)
                resetStatsButton.interactable = PlayerStatSystem.Instance != null &&
                                                PlayerStatSystem.Instance.CanResetStats();
        }

        private GameObject CreateConfirmDialog()
        {
            // Gan vao panel "Trang_Bi" de dialog tu dong an khi dong panel.
            Transform trangBi = FindTrangBiPanel();
            if (trangBi == null)
                return null;

            GameObject root = new GameObject("ResetConfirmDialog", typeof(RectTransform),
                typeof(Image), typeof(Button));
            root.layer = trangBi.gameObject.layer;
            root.transform.SetParent(trangBi, false);

            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            // Nut cha la nen mo: bam ra ngoai cung dong hop.
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            root.GetComponent<Button>().onClick.AddListener(HideResetConfirmation);

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = confirmPanelSize;
            panel.GetComponent<Image>().color = confirmPanelColor;

            GameObject title = CreateConfirmText(panel.transform, "Title", confirmTitleText, 30,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -56f), new Vector2(-32f, -8f), Color.white);
            title.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            CreateConfirmText(panel.transform, "Message", confirmMessageFree, 24,
                new Vector2(0f, 0.28f), new Vector2(1f, 0.78f), new Vector2(32f, 0f), new Vector2(-32f, 0f), Color.white);

            GameObject yes = CreateConfirmButton(panel.transform, "ConfirmButton", confirmYesText, confirmYesColor);
            GameObject no = CreateConfirmButton(panel.transform, "CancelButton", confirmNoText, confirmNoColor);

            RectTransform yesRect = (RectTransform)yes.transform;
            yesRect.anchorMin = yesRect.anchorMax = new Vector2(0.5f, 0f);
            yesRect.pivot = new Vector2(1f, 0f);
            yesRect.anchoredPosition = new Vector2(-24f, 28f);
            yesRect.sizeDelta = new Vector2(220f, 60f);

            RectTransform noRect = (RectTransform)no.transform;
            noRect.anchorMin = noRect.anchorMax = new Vector2(0.5f, 0f);
            noRect.pivot = new Vector2(0f, 0f);
            noRect.anchoredPosition = new Vector2(24f, 28f);
            noRect.sizeDelta = new Vector2(220f, 60f);

            root.SetActive(false);

            // Canvas rieng de dialog luon ve tren cung, khong bi Down_Panel dap.
            var dOverlay = root.AddComponent<Canvas>();
            dOverlay.overrideSorting = true;
            dOverlay.sortingOrder = 600;
            dOverlay.worldCamera = trangBi.GetComponentInParent<Canvas>().rootCanvas.worldCamera;
            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            return root;
        }

        private static GameObject CreateConfirmText(Transform parent, string name, string content, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var tmp = go.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.enableWordWrapping = true;
            return go;
        }

        private static GameObject CreateConfirmButton(Transform parent, string name, string label, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;

            GameObject text = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            text.transform.SetParent(go.transform, false);
            RectTransform textRect = (RectTransform)text.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var tmp = text.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return go;
        }

        /// <summary>
        /// Dong bo chi so thuoc (Phu thuy nang cap) vao cum Potion cua panel
        /// Trang_Bi. Cong thuc trung khop voi badge trong shop Phu thuy.
        /// </summary>
        private void ApplyPotionStats()
        {
            if (potionHealText == null && potionCooldownText == null && potionShieldText == null)
                return;

            AlchemistUpgradeSystem witch = AlchemistUpgradeSystem.Instance;
            int healBonus = witch != null
                ? AlchemistUpgradeSystem.GetHealingBonusPercent(witch.HealingLevel)
                : 0;
            int baseHeal = PlayerStatSystem.Instance != null
                ? PlayerStatSystem.Instance.PotionHealAmount
                : 50;
            int heal = Mathf.RoundToInt(baseHeal * (1f + healBonus / 100f));
            int cooldown = witch != null ? witch.GetCooldownValue() : AlchemistUpgradeSystem.BaseCooldownSeconds;
            int shield = witch != null ? witch.GetShieldValue() : AlchemistUpgradeSystem.BaseShieldValue;

            if (potionHealText != null)
                potionHealText.text = heal.ToString();
            if (potionCooldownText != null)
                potionCooldownText.text = cooldown + "s";
            if (potionShieldText != null)
                potionShieldText.text = shield.ToString();
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

        private static void Apply(List<TMP_Text> texts, int value, Func<int, string> format = null)
        {
            string s = format != null ? format(value) : value.ToString();
            for (int i = 0; i < texts.Count; i++)
            {
                if (texts[i] != null)
                    texts[i].text = s;
            }
        }

        /// <summary>
        /// Rut gon so tien: tu 1000 tro len hien dang "1,01k" (dau phay la dau
        /// thap phan kieu Viet Nam), 1.000.000 tro len la "1,23m". Duoi 1000 giu
        /// nguyen con so. Bo so 0 thua: 1500 -> "1,5k", 15000 -> "15k".
        /// </summary>
        private static string FormatCurrency(int value)
        {
            if (value < 1000)
                return value.ToString();

            double v = value / 1000.0;
            if (v >= 1000.0)
                return Shorten(v / 1000.0) + "m";

            return Shorten(v) + "k";
        }

        private static string Shorten(double v)
        {
            return v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                    .Replace('.', ',');
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
