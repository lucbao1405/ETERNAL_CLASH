using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using EternalClash.Data;
using EternalClash.Village;
using EternalClash.Core.Save;
using EternalClash.Skill;

namespace EternalClash.UI
{
    /// <summary>
    /// Controller cho giao diện chọn kỹ năng trong Town (Skill Panel).
    /// Hỗ trợ chuyển đổi giữa nhánh Charge và Shield, đổi mô tả đặc điểm,
    /// phát loop animation tương ứng trên Spine SkeletonGraphic,
    /// và nạp danh sách kỹ năng con từ file SkillData ScriptableObject.
    /// </summary>
    public class TownSkillPanelController : MonoBehaviour
    {
        [System.Serializable]
        public class SubSkillData
        {
            public string skillId;
            public string skillName;
            public int tier = 1;
            public bool isUnlocked = true;
            public string unlockRequirement = "Mở khóa khi đạt cấp độ cao hơn.";
            [TextArea(2, 5)]
            public string skillDescription;
            public Sprite icon;
            public SkillData skillDataAsset;
        }

        [System.Serializable]
        public class SkillBranchData
        {
            public string branchId;            // "charge" hoac "shield"
            public string branchDisplayName;   // "CHARGE" hoac "SHIELD"
            [TextArea(3, 6)]
            public string traitDescription;    // Motadacdiemchieu
            public string spineAnimationName;  // "charge", "shield"
            public Button switchButton;        // Nut bam chuyen nhanh
            [Tooltip("Kéo thả các file SkillData (ScriptableObject) vào đây")]
            public List<SkillData> skillDataAssets = new List<SkillData>();
            [Tooltip("Dữ liệu con (tự động đồng bộ từ skillDataAssets hoặc nhập thủ công)")]
            public List<SubSkillData> subSkills = new List<SubSkillData>();
        }

        [Header("--- Header & Branch Info ---")]
        [Tooltip("Text hiển thị tên nhánh kỹ năng chính (CHARGE / SHIELD)")]
        [SerializeField] private TMP_Text tenSkillText;

        [Tooltip("Text hiển thị mô tả đặc điểm chiêu thức của nhánh")]
        [SerializeField] private TMP_Text motaDacDiemChieuText;

        [Header("--- Character Preview ---")]
        [Tooltip("Spine SkeletonGraphic của nhân vật chính trong panel")]
        [SerializeField] private SkeletonGraphic nhanVatChinhGraphic;

        [Header("--- Selected Sub-Skill Info ---")]
        [Tooltip("Text hiển thị tên skill được chọn trong list_skill")]
        [SerializeField] private TMP_Text chonSkillText;

        [Tooltip("Text hiển thị mô tả của skill được chọn trong list_skill")]
        [SerializeField] private TMP_Text motaSkillText;

        [Header("--- Navigation Buttons ---")]
        [SerializeField] private Button chargeButton;
        [SerializeField] private Button shieldButton;
        [SerializeField] private Button closeButton;

        [Header("--- Sub-Skill Slots (List_Skill) ---")]
        [Tooltip("Transform chứa các slot kỹ năng con")]
        [SerializeField] private Transform listSkillContainer;
        [SerializeField] private List<Button> skillSlotButtons = new List<Button>();
        [SerializeField] private ScrollRect skillScrollRect;

        [Header("--- Highlight & Lock Settings ---")]
        [SerializeField] private Color activeBranchButtonColor = Color.white;
        [SerializeField] private Color inactiveBranchButtonColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        [SerializeField] private Color selectedSlotColor = new Color(1f, 0.92f, 0.4f, 1f);
        [SerializeField] private Color normalSlotColor = Color.white;
        [SerializeField] private Color lockedSlotColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color lockedIconColor = new Color(0.6f, 0.6f, 0.6f, 0.9f);

        [Header("--- Level Unlock Settings ---")]
        [Tooltip("Cấp độ mở khóa kỹ năng thứ 2 (mặc định 10)")]
        [SerializeField] private int skill2UnlockLevel = 10;

        [Tooltip("Khoảng cách cấp độ mở mỗi kỹ năng tiếp theo (mặc định 5: Lv 15, Lv 20,...)")]
        [SerializeField] private int levelStepPerSkill = 5;

        [Tooltip("Ghi đè level để test trực tiếp trong Inspector (nếu = 0 sẽ lấy level thật của người chơi)")]
        [SerializeField] private int testPlayerLevelOverride = 0;

        [Header("--- Skill Branches Data ---")]
        [SerializeField] private List<SkillBranchData> branches = new List<SkillBranchData>();

        private int currentBranchIndex = 0;
        private int currentSubSkillIndex = 0;

        private void Awake()
        {
            SanitizeLockedColors();
            AutoFindReferences();
            EnsureSkillScrollRect();
            InitializeDefaultDataIfNeeded();
            RegisterButtonEvents();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SanitizeLockedColors();
        }
#endif

        private void SanitizeLockedColors()
        {
            // Tự động nâng độ sáng nếu giá trị đang quá tối từ bản cũ
            if (lockedIconColor.r < 0.35f || lockedIconColor.a < 0.5f)
            {
                lockedIconColor = new Color(0.6f, 0.6f, 0.6f, 0.9f);
            }
            if (lockedSlotColor.r < 0.35f)
            {
                lockedSlotColor = new Color(0.45f, 0.45f, 0.45f, 1f);
            }
        }

        private void OnEnable()
        {
            // Restore the loadout selected before entering another scene.
            int savedBranchIndex = branches.FindIndex(b =>
                string.Equals(b.branchId, SkillLoadout.BranchId, StringComparison.OrdinalIgnoreCase));
            if (savedBranchIndex >= 0)
                currentBranchIndex = savedBranchIndex;

            SelectBranch(currentBranchIndex);
        }

        private void RegisterButtonEvents()
        {
            if (chargeButton != null)
            {
                chargeButton.onClick.RemoveListener(OnChargeButtonClicked);
                chargeButton.onClick.AddListener(OnChargeButtonClicked);
            }

            if (shieldButton != null)
            {
                shieldButton.onClick.RemoveListener(OnShieldButtonClicked);
                shieldButton.onClick.AddListener(OnShieldButtonClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseButtonClicked);
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        public void OnChargeButtonClicked()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            int index = branches.FindIndex(b => string.Equals(b.branchId, "charge", StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                SelectBranch(index);
            }
        }

        public void OnShieldButtonClicked()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            int index = branches.FindIndex(b => string.Equals(b.branchId, "shield", StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                SelectBranch(index);
            }
        }

        public void OnCloseButtonClicked()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            var animator = GetComponent<ShopPanelAnimator>();
            if (animator != null)
            {
                animator.Close();
                return;
            }

            SmoothSlide slide = GetComponent<SmoothSlide>();
            if (slide != null)
            {
                slide.ClosePanel();
                return;
            }

            if (gameObject != null)
                gameObject.SetActive(false);
        }

        /// <summary>
        /// Gán danh sách file dữ liệu SkillData cho một nhánh cụ thể và tự động đồng bộ.
        /// </summary>
        public void AssignSkillDataAssets(string branchId, List<SkillData> assets)
        {
            InitializeDefaultDataIfNeeded();
            int index = branches.FindIndex(b => string.Equals(b.branchId, branchId, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                branches[index].skillDataAssets = new List<SkillData>(assets);
                SyncSubSkillsFromAssets(branches[index]);
            }
        }

        /// <summary>
        /// Chọn một nhánh kỹ năng (Charge hoặc Shield).
        /// </summary>
        public void SelectBranch(int branchIndex)
        {
            if (branches == null || branches.Count == 0)
                return;

            if (branchIndex < 0 || branchIndex >= branches.Count)
                branchIndex = 0;

            currentBranchIndex = branchIndex;
            SkillBranchData branch = branches[branchIndex];
            SkillLoadout.SelectBranch(branch.branchId);

            // 1. Cập nhật ten_skill
            if (tenSkillText != null)
            {
                tenSkillText.text = branch.branchDisplayName;
            }

            // 2. Cập nhật Motadacdiemchieu
            if (motaDacDiemChieuText != null)
            {
                motaDacDiemChieuText.text = branch.traitDescription;
            }

            // 3. Đổi animation của nhan_vat_chinh, loop = true
            PlayCharacterAnimation(branch.spineAnimationName, true);

            // 4. Cập nhật màu sắc/trạng thái các nút chọn nhánh
            UpdateBranchButtonsVisual();

            // 5. Cập nhật danh sách các kỹ năng con trong list_skill
            RefreshSubSkillSlots(branch);

            // Restore the previously equipped sub-skill when reopening the panel.
            int savedSkillIndex = branch.subSkills.FindIndex(s =>
                string.Equals(s.skillId, SkillLoadout.SkillId, StringComparison.OrdinalIgnoreCase));
            SelectSubSkill(savedSkillIndex >= 0 ? savedSkillIndex : 0);
        }

        /// <summary>
        /// Phát animation trên Spine SkeletonGraphic.
        /// </summary>
        public void PlayCharacterAnimation(string animName, bool loop = true)
        {
            if (nhanVatChinhGraphic == null || string.IsNullOrEmpty(animName))
                return;

            try
            {
                if (!nhanVatChinhGraphic.IsValid)
                {
                    nhanVatChinhGraphic.Initialize(false);
                }

                if (nhanVatChinhGraphic.AnimationState != null)
                {
                    nhanVatChinhGraphic.AnimationState.SetAnimation(0, animName, loop);
                }
                else
                {
                    nhanVatChinhGraphic.startingAnimation = animName;
                    nhanVatChinhGraphic.startingLoop = loop;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TownSkillPanelController] Không thể phát animation '{animName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Cập nhật các slot trong list_skill.
        /// </summary>
        private void RefreshSubSkillSlots(SkillBranchData branch)
        {
            EnsureSlotButtons();
            SyncSubSkillsFromAssets(branch);

            for (int i = 0; i < skillSlotButtons.Count; i++)
            {
                Button slotBtn = skillSlotButtons[i];
                if (slotBtn == null)
                    continue;

                int slotIndex = i;
                slotBtn.onClick.RemoveAllListeners();

                if (i < branch.subSkills.Count)
                {
                    slotBtn.gameObject.SetActive(true);
                    slotBtn.interactable = true;

                    SubSkillData skill = branch.subSkills[i];

                    // Cập nhật icon nếu có Image con
                    Image iconImg = FindSlotIconImage(slotBtn);
                    if (iconImg != null)
                    {
                        if (skill.icon != null)
                        {
                            iconImg.sprite = skill.icon;
                            iconImg.enabled = true;
                        }
                        // Nếu mở khóa: sáng rõ (Color.white), nếu khóa: làm mờ/tối (lockedIconColor)
                        iconImg.color = skill.isUnlocked ? Color.white : lockedIconColor;
                    }

                    // Cập nhật màu nền của slot
                    Image bgImg = slotBtn.GetComponent<Image>();
                    if (bgImg != null)
                    {
                        bgImg.color = skill.isUnlocked ? normalSlotColor : lockedSlotColor;
                    }

                    slotBtn.onClick.AddListener(() =>
                    {
                        EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
                        SelectSubSkill(slotIndex);
                    });
                }
                else
                {
                    // Nếu nhánh có ít hơn số lượng slot
                    slotBtn.interactable = false;
                }
            }
        }

        /// <summary>
        /// Lấy cấp độ yêu cầu để mở khóa slot kỹ năng:
        /// - Skill 1 (slot 0): Lv 1 (mở mặc định)
        /// - Skill 2 (slot 1): Lv 10
        /// - Skill 3 (slot 2): Lv 15
        /// - Skill 4 (slot 3): Lv 20 (từ sau cứ 5 lv mở 1 skill)
        /// </summary>
        public int GetRequiredLevelForSlot(int slotIndex)
        {
            if (slotIndex <= 0) return 1;
            return skill2UnlockLevel + (slotIndex - 1) * levelStepPerSkill;
        }

        /// <summary>
        /// Lấy cấp độ hiện tại của người chơi trong Town.
        /// </summary>
        public int GetPlayerCurrentLevel()
        {
            if (testPlayerLevelOverride > 0)
                return testPlayerLevelOverride;

            if (PlayerStatSystem.Instance != null)
                return PlayerStatSystem.Instance.Level;

            if (SaveManager.Instance?.Data != null)
            {
                int lvl = SaveManager.Instance.Data.level;
                if (lvl <= 1 && SaveManager.Instance.Data.player != null)
                    lvl = SaveManager.Instance.Data.player.level;
                return Mathf.Max(1, lvl);
            }

            return 1;
        }

        /// <summary>
        /// Đồng bộ và sắp xếp danh sách kỹ năng con:
        /// - Sắp xếp tăng dần theo Tier (Tier 1 thấp nhất).
        /// - Lv 10 mở skill 2, từ sau cứ 5 lv mở 1 skill.
        /// </summary>
        private void SyncSubSkillsFromAssets(SkillBranchData branch)
        {
            if (branch == null)
                return;

            int playerLevel = GetPlayerCurrentLevel();

            // 1. Nếu có danh sách ScriptableObject SkillData
            if (branch.skillDataAssets != null && branch.skillDataAssets.Count > 0)
            {
                foreach (SkillData asset in branch.skillDataAssets)
                    SkillLoadout.RegisterData(asset);

                // Sắp xếp tăng dần theo Tier: Tier 1 là thấp nhất
                branch.skillDataAssets.Sort((a, b) =>
                {
                    if (a == null && b == null) return 0;
                    if (a == null) return 1;
                    if (b == null) return -1;
                    int comp = a.tier.CompareTo(b.tier);
                    if (comp != 0) return comp;
                    return string.Compare(a.skillName, b.skillName, StringComparison.Ordinal);
                });

                branch.subSkills.Clear();
                for (int i = 0; i < branch.skillDataAssets.Count; i++)
                {
                    var asset = branch.skillDataAssets[i];
                    if (asset == null)
                        continue;

                    int reqLevel = GetRequiredLevelForSlot(i);
                    // Skill 1 (i=0) mở sẵn, các skill sau yêu cầu level
                    bool unlocked = (i == 0) || (playerLevel >= reqLevel);

                    branch.subSkills.Add(new SubSkillData
                    {
                        skillId = asset.skillId,
                        skillName = asset.skillName,
                        tier = asset.tier,
                        isUnlocked = unlocked,
                        unlockRequirement = $"Mở khóa ở Lv {reqLevel}",
                        skillDescription = asset.GetFullDescription(),
                        icon = asset.icon,
                        skillDataAsset = asset
                    });
                }
            }
            else if (branch.subSkills != null && branch.subSkills.Count > 0)
            {
                // Nếu dùng dữ liệu nhập thủ công: Sắp xếp theo Tier (Tier 1 thấp nhất)
                branch.subSkills.Sort((a, b) => a.tier.CompareTo(b.tier));

                for (int i = 0; i < branch.subSkills.Count; i++)
                {
                    int reqLevel = GetRequiredLevelForSlot(i);
                    branch.subSkills[i].isUnlocked = (i == 0) || (playerLevel >= reqLevel);
                    branch.subSkills[i].unlockRequirement = $"Mở khóa ở Lv {reqLevel}";
                }
            }
        }

        /// <summary>
        /// Khi người chơi chọn 1 skill trong list_skill.
        /// Skill chưa mở: làm tối đi, chỉ hiện mỗi tên (ẩn mô tả).
        /// </summary>
        public void SelectSubSkill(int subSkillIndex)
        {
            if (currentBranchIndex < 0 || currentBranchIndex >= branches.Count)
                return;

            SkillBranchData branch = branches[currentBranchIndex];
            if (branch.subSkills == null || branch.subSkills.Count == 0)
                return;

            if (subSkillIndex < 0 || subSkillIndex >= branch.subSkills.Count)
                subSkillIndex = 0;

            currentSubSkillIndex = subSkillIndex;
            SubSkillData skill = branch.subSkills[subSkillIndex];
            int reqLevel = GetRequiredLevelForSlot(subSkillIndex);

            if (skill.isUnlocked)
                SkillLoadout.Save(branch.branchId, skill.skillId, skill.skillDataAsset);

            // 1. Cập nhật chon_skill: chỉ hiện mỗi tên
            if (chonSkillText != null)
            {
                chonSkillText.text = skill.skillName;
            }

            // 2. Cập nhật mota_skill:
            // - Nếu đã mở: hiện đầy đủ mô tả chiêu và hiệu ứng đặc biệt
            // - Nếu chưa mở: ẩn mô tả, chỉ hiện thông báo mở khóa ở Lv bao nhiêu
            if (motaSkillText != null)
            {
                if (skill.isUnlocked)
                {
                    motaSkillText.text = skill.skillDescription;
                }
                else
                {
                    motaSkillText.text = $"<color=#FF5555>🔒 Mở khóa ở Lv {reqLevel}</color>";
                }
            }

            // 3. Highlight slot đang chọn
            UpdateSlotHighlight(subSkillIndex);
        }

        private void UpdateSlotHighlight(int selectedIndex)
        {
            if (currentBranchIndex < 0 || currentBranchIndex >= branches.Count)
                return;

            SkillBranchData branch = branches[currentBranchIndex];

            for (int i = 0; i < skillSlotButtons.Count; i++)
            {
                Button slot = skillSlotButtons[i];
                if (slot == null)
                    continue;

                Image img = slot.GetComponent<Image>();
                if (img != null)
                {
                    bool isUnlocked = (i < branch.subSkills.Count) && branch.subSkills[i].isUnlocked;
                    if (i == selectedIndex)
                    {
                        img.color = selectedSlotColor;
                    }
                    else
                    {
                        img.color = isUnlocked ? normalSlotColor : lockedSlotColor;
                    }
                }
            }
        }

        private void UpdateBranchButtonsVisual()
        {
            if (chargeButton != null)
            {
                bool isChargeActive = currentBranchIndex < branches.Count &&
                    string.Equals(branches[currentBranchIndex].branchId, "charge", StringComparison.OrdinalIgnoreCase);

                Image img = chargeButton.GetComponent<Image>();
                if (img != null)
                    img.color = isChargeActive ? activeBranchButtonColor : inactiveBranchButtonColor;
            }

            if (shieldButton != null)
            {
                bool isShieldActive = currentBranchIndex < branches.Count &&
                    string.Equals(branches[currentBranchIndex].branchId, "shield", StringComparison.OrdinalIgnoreCase);

                Image img = shieldButton.GetComponent<Image>();
                if (img != null)
                    img.color = isShieldActive ? activeBranchButtonColor : inactiveBranchButtonColor;
            }
        }

        private Image FindSlotIconImage(Button slotBtn)
        {
            Transform iconTransform = slotBtn.transform.Find("Icon") ??
                                      slotBtn.transform.Find("ItemIcon") ??
                                      slotBtn.transform.Find("icon");
            if (iconTransform != null)
            {
                return iconTransform.GetComponent<Image>();
            }

            // Tìm Image con đầu tiên không phải Image nền của nút
            Image[] images = slotBtn.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject != slotBtn.gameObject)
                    return img;
            }
            return null;
        }

        private void EnsureSlotButtons()
        {
            if (skillSlotButtons == null || skillSlotButtons.Count == 0)
            {
                if (listSkillContainer != null)
                {
                    skillSlotButtons = new List<Button>(listSkillContainer.GetComponentsInChildren<Button>(true));
                }
            }
        }

        private void EnsureSkillScrollRect()
        {
            if (!(listSkillContainer is RectTransform viewport))
                return;

            skillScrollRect = viewport.GetComponent<ScrollRect>() ?? viewport.gameObject.AddComponent<ScrollRect>();
            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();

            skillScrollRect.viewport = viewport;
            skillScrollRect.horizontal = false;
            skillScrollRect.vertical = true;
            skillScrollRect.movementType = ScrollRect.MovementType.Clamped;
            skillScrollRect.inertia = true;
            skillScrollRect.scrollSensitivity = 35f;

            RectTransform content = viewport.Find("Content") as RectTransform;
            if (content == null)
            {
                GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter), typeof(GridLayoutGroup));
                contentObject.transform.SetParent(viewport, false);
                content = contentObject.GetComponent<RectTransform>();
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = Vector2.zero;

                ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                GridLayoutGroup oldGrid = viewport.GetComponent<GridLayoutGroup>();
                GridLayoutGroup newGrid = contentObject.GetComponent<GridLayoutGroup>();
                if (oldGrid != null)
                {
                    newGrid.cellSize = oldGrid.cellSize;
                    newGrid.spacing = oldGrid.spacing;
                    newGrid.padding = oldGrid.padding;
                    newGrid.childAlignment = oldGrid.childAlignment;
                    Destroy(oldGrid);
                }
                newGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                newGrid.constraintCount = 2;

                for (int i = viewport.childCount - 1; i >= 0; i--)
                {
                    Transform child = viewport.GetChild(i);
                    if (child != content && child.GetComponent<Button>() != null)
                        child.SetParent(content, false);
                }
            }

            skillScrollRect.content = content;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            skillScrollRect.verticalNormalizedPosition = 1f;

            if (skillScrollRect.GetComponent<UIScrollSound>() == null)
                skillScrollRect.gameObject.AddComponent<UIScrollSound>();
        }

        /// <summary>
        /// Tự động tìm kiếm các GameObject và Component theo đúng cấu trúc Hierarchy của scene.
        /// </summary>
        [ContextMenu("Auto Find References")]
        public void AutoFindReferences()
        {
            // 1. ten_skill
            if (tenSkillText == null)
            {
                Transform tenSkillTransform = transform.Find("KhungSkill/Ten_skill") ??
                                              FindDeepChild(transform, "Ten_skill");
                if (tenSkillTransform != null)
                {
                    tenSkillText = tenSkillTransform.GetComponentInChildren<TMP_Text>();
                }
            }

            // 2. Motadacdiemchieu
            if (motaDacDiemChieuText == null)
            {
                Transform motaTransform = transform.Find("ThongTinSkill/Mota_dacdiemchieu") ??
                                          FindDeepChild(transform, "Mota_dacdiemchieu");
                if (motaTransform != null)
                {
                    motaDacDiemChieuText = motaTransform.GetComponent<TMP_Text>() ??
                                          motaTransform.GetComponentInChildren<TMP_Text>();
                }
            }

            // 3. Nhan_Vat_Chinh (Spine SkeletonGraphic)
            if (nhanVatChinhGraphic == null)
            {
                Transform nvTransform = transform.Find("Nhan_Vat_Chinh") ??
                                        FindDeepChild(transform, "Nhan_Vat_Chinh");
                if (nvTransform != null)
                {
                    nhanVatChinhGraphic = nvTransform.GetComponent<SkeletonGraphic>();
                }
                else
                {
                    nhanVatChinhGraphic = GetComponentInChildren<SkeletonGraphic>(true);
                }
            }

            // 4. Chon_skill
            if (chonSkillText == null)
            {
                Transform chonSkillTransform = transform.Find("KhungSkill/Chon_skill") ??
                                               FindDeepChild(transform, "Chon_skill");
                if (chonSkillTransform != null)
                {
                    chonSkillText = chonSkillTransform.GetComponent<TMP_Text>();
                }
            }

            // 5. Mota_skill
            if (motaSkillText == null)
            {
                Transform motaSkillTransform = transform.Find("KhungSkill/Chon_skill/Mota_skill") ??
                                               FindDeepChild(transform, "Mota_skill");
                if (motaSkillTransform != null)
                {
                    motaSkillText = motaSkillTransform.GetComponent<TMP_Text>();
                }
            }

            // 6. Nút Charge & Shield
            if (chargeButton == null)
            {
                Transform chargeTransform = transform.Find("Charge") ?? FindDeepChild(transform, "Charge");
                if (chargeTransform != null)
                    chargeButton = chargeTransform.GetComponent<Button>();
            }

            if (shieldButton == null)
            {
                Transform shieldTransform = transform.Find("Shield") ?? FindDeepChild(transform, "Shield");
                if (shieldTransform != null)
                    shieldButton = shieldTransform.GetComponent<Button>();
            }

            // 7. Nút Close (X)
            if (closeButton == null)
            {
                Transform closeTransform = transform.Find("KhungSkill/X") ??
                                           transform.Find("X") ??
                                           FindDeepChild(transform, "X");
                if (closeTransform != null)
                    closeButton = closeTransform.GetComponent<Button>();
            }

            // 8. List_Skill & Slots
            if (listSkillContainer == null)
            {
                listSkillContainer = transform.Find("KhungSkill/List_Skill") ??
                                     FindDeepChild(transform, "List_Skill");
            }

            if (listSkillContainer != null && (skillSlotButtons == null || skillSlotButtons.Count == 0))
            {
                skillSlotButtons = new List<Button>(listSkillContainer.GetComponentsInChildren<Button>(true));
            }
        }

        private void InitializeDefaultDataIfNeeded()
        {
            if (branches != null && branches.Count >= 2)
                return;

            branches = new List<SkillBranchData>
            {
                new SkillBranchData
                {
                    branchId = "charge",
                    branchDisplayName = "CHARGE",
                    traitDescription = "Charge forward to deal more damage. You won't be knockback, but will still receive damage.",
                    spineAnimationName = "charge",
                    switchButton = chargeButton,
                    subSkills = new List<SubSkillData>
                    {
                        new SubSkillData
                        {
                            skillId = "charge_01",
                            skillName = "Power Rush",
                            tier = 1,
                            isUnlocked = true,
                            unlockRequirement = "Mặc định mở khóa.",
                            skillDescription = "Charges forward with explosive speed, increasing damage dealt by +25% on impact."
                        },
                        new SubSkillData
                        {
                            skillId = "charge_02",
                            skillName = "Iron Vanguard",
                            tier = 2,
                            isUnlocked = true,
                            unlockRequirement = "Mặc định mở khóa.",
                            skillDescription = "Gain high hyper-armor while charging. Stuns the first obstacle or enemy struck for 1.2s."
                        },
                        new SubSkillData
                        {
                            skillId = "charge_03",
                            skillName = "Crushing Momentum",
                            tier = 3,
                            isUnlocked = false,
                            unlockRequirement = "Mở khóa khi hoàn thành Stage 2.",
                            skillDescription = "Inflicts heavy knockback on all enemies along the path and clears minor projectiles."
                        },
                        new SubSkillData
                        {
                            skillId = "charge_04",
                            skillName = "Overdrive Surge",
                            tier = 4,
                            isUnlocked = false,
                            unlockRequirement = "Mở khóa khi đạt cấp độ cao hơn.",
                            skillDescription = "Reduces Charge cooldown by 1.0s and increases movement speed briefly after the charge ends."
                        }
                    }
                },
                new SkillBranchData
                {
                    branchId = "shield",
                    branchDisplayName = "SHIELD",
                    // Khựng lại 1s. Giảm 80% sát thương nhận vào, phản lại 5 DMG -> Dịch sang tiếng Anh
                    traitDescription = "Pause for 1s. Reduce incoming damage by 80% and reflect 5 DMG back to attackers.",
                    spineAnimationName = "shield",
                    switchButton = shieldButton,
                    subSkills = new List<SubSkillData>
                    {
                        new SubSkillData
                        {
                            skillId = "shield_01",
                            skillName = "Iron Aegis",
                            tier = 1,
                            isUnlocked = true,
                            unlockRequirement = "Mặc định mở khóa.",
                            skillDescription = "Strengthens the barrier, raising damage reduction to 90% and prolonging duration by 0.3s."
                        },
                        new SubSkillData
                        {
                            skillId = "shield_02",
                            skillName = "Spike Retaliation",
                            tier = 2,
                            isUnlocked = true,
                            unlockRequirement = "Mặc định mở khóa.",
                            skillDescription = "Reflects an additional 10 DMG (total 15 DMG) back to the attacking enemy."
                        },
                        new SubSkillData
                        {
                            skillId = "shield_03",
                            skillName = "Bulwark Stance",
                            tier = 3,
                            isUnlocked = false,
                            unlockRequirement = "Mở khóa khi hoàn thành Stage 2.",
                            skillDescription = "Heals 6% max HP when successfully withstanding a direct lethal blow while shielding."
                        },
                        new SubSkillData
                        {
                            skillId = "shield_04",
                            skillName = "Counter Shockwave",
                            tier = 4,
                            isUnlocked = false,
                            unlockRequirement = "Mở khóa khi đạt cấp độ cao hơn.",
                            skillDescription = "Emits a shockwave upon blocking that knocks back and slows adjacent enemies by 40%."
                        }
                    }
                }
            };
        }

        private Transform FindDeepChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                    return child;

                Transform result = FindDeepChild(child, childName);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void Reset()
        {
            AutoFindReferences();
            InitializeDefaultDataIfNeeded();
        }
    }
}
