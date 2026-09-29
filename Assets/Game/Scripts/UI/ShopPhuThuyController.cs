using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EternalClash.Core.Save;
using EternalClash.Data;
using EternalClash.Village;

namespace EternalClash.UI
{
    /// <summary>
    /// Phù thủy: bảng nấu thuốc kiểu Postknight — 3 badge chỉ số thuốc, carousel
    /// nguyên liệu (mỗi nguyên liệu nâng một chỉ số), nút INFUSE. Chuyển trang
    /// nguyên liệu bằng vuốt trái/phải hoặc 2 mũi tên.
    /// Toàn bộ node UI nằm trong scene (Town.unity, panel Shop_Phu_Thuy) để chỉnh
    /// trực tiếp trong editor; controller chỉ tự nối tham chiếu theo tên node khi
    /// bật. Nếu scene thiếu layout (mới tạo scene khác), controller tự dựng bằng
    /// code như bản cũ; menu "Bake Panel Into Scene" dựng lại layout vào scene.
    /// Nguyên liệu dùng đúng vật phẩm có sẵn trong ItemCatalog:
    /// leaf_green -> Heal, leaf_red -> Cooldown, leaf_yellow -> Shield.
    /// blue_flower là vật phẩm bond (tặng quà), không dùng nấu thuốc.
    /// </summary>
    public sealed class ShopPhuThuyController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private struct BrewMaterial
        {
            public string itemId;
            public string fallbackName;
            public string fallbackDescription;
            public WitchAbility ability;

            public BrewMaterial(string itemId, string fallbackName, string fallbackDescription,
                WitchAbility ability)
            {
                this.itemId = itemId;
                this.fallbackName = fallbackName;
                this.fallbackDescription = fallbackDescription;
                this.ability = ability;
            }
        }

        private static readonly BrewMaterial[] Materials =
        {
            new BrewMaterial("leaf_green", "Green Leaf",
                "A vibrant green leaf. Brewed into the potion to strengthen its healing power.",
                WitchAbility.Healing),
            new BrewMaterial("leaf_red", "Red Leaf",
                "A fiery red leaf that lets the brew recharge faster.",
                WitchAbility.Cooldown),
            new BrewMaterial("leaf_yellow", "Yellow Leaf",
                "A golden leaf that hardens the knight's guard when brewed into the potion.",
                WitchAbility.Defense)
        };

        private static readonly string[] BadgeLabels = { "HEAL", "COOLDOWN", "SHIELD" };
        private const int BaseHealFallback = 50;
        private const float ProgressWidth = 460f;
        private const float SwipeThreshold = 120f;

        private static Sprite whiteSprite;

        private int selectedMaterial;
        private bool wired;
        private bool dragging;
        private float dragDeltaX;

        private Button closeButton;
        private Button infuseButton;
        private readonly TMP_Text[] badgeValues = new TMP_Text[3];
        private Image materialIcon;
        private TMP_Text materialNameText;
        private TMP_Text materialDescriptionText;
        private TMP_Text materialQuantityText;
        private TMP_Text previewText;
        private RectTransform progressFill;
        private readonly Image[] dots = new Image[3];
        private SaveManager saveManager;

        private static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite == null)
                    whiteSprite = Sprite.Create(Texture2D.whiteTexture,
                        new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                        new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
                return whiteSprite;
            }
        }

        private void Awake()
        {
            EnsureWired();
        }

        private void OnEnable()
        {
            EnsureWired();
            SubscribeToSaveChanges();
            Refresh();
        }

        private void OnDisable()
        {
            if (saveManager != null)
                saveManager.SaveChanged -= OnSaveChanged;
            saveManager = null;
        }

        private void SubscribeToSaveChanges()
        {
            SaveManager current = SaveManager.Instance;
            if (current == saveManager)
                return;
            if (saveManager != null)
                saveManager.SaveChanged -= OnSaveChanged;
            saveManager = current;
            if (saveManager != null)
                saveManager.SaveChanged += OnSaveChanged;
        }

        private void OnSaveChanged(SaveData _)
        {
            Refresh();
        }

        public void Open()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            ShopPanelAnimator animator = GetComponent<ShopPanelAnimator>();
            if (animator != null)
                animator.Close();
            else
                gameObject.SetActive(false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = true;
            dragDeltaX = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragging)
                dragDeltaX += eventData.delta.x;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;
            dragging = false;
            if (dragDeltaX <= -SwipeThreshold)
                ShowPage(1);
            else if (dragDeltaX >= SwipeThreshold)
                ShowPage(-1);
        }

        public void ShowNextPage()
        {
            ShowPage(1);
        }

        public void ShowPreviousPage()
        {
            ShowPage(-1);
        }

        public void SelectMaterial(int index)
        {
            if (index < 0 || index >= Materials.Length || index == selectedMaterial)
                return;
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            selectedMaterial = index;
            Refresh();
        }

        private void ShowPage(int delta)
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            selectedMaterial = (selectedMaterial + delta + Materials.Length) % Materials.Length;
            Refresh();
        }

        public void InfuseSelected()
        {
            AlchemistUpgradeSystem witch = AlchemistUpgradeSystem.Instance;
            if (witch == null)
                return;

            BrewMaterial material = Materials[selectedMaterial];
            if (witch.IsMaxed(material.ability))
            {
                ToastMessage.Show("Cooldown fully upgraded.");
                return;
            }
            if (!witch.TryInfuse(material.ability))
            {
                ToastMessage.Show("Not enough " + ResolveMaterialName(material) + ".");
                return;
            }

            Refresh();
        }

        [ContextMenu("Bake Panel Into Scene")]
        public void BakePanelIntoScene()
        {
            wired = false;
            BuildPanel();
            WireReferences();
            wired = true;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
        }

        private void EnsureWired()
        {
            if (wired)
                return;
            if (transform.Find("NguyenLieu") == null)
                BuildPanel();
            WireReferences();
            wired = true;
        }

        private void BuildPanel()
        {
            Sprite lootFrame = CaptureLootFrame();
            DeleteGeneratedNodes();
            AuthorKeptNodes();
            BuildBadges();
            BuildMaterialPanel(lootFrame);
            BuildDotsAndPreview();
        }

        private Sprite CaptureLootFrame()
        {
            Transform slot = FindDeepChild(transform, "Slot_1");
            if (slot != null)
            {
                Image image = slot.GetComponent<Image>();
                if (image != null && image.sprite != null)
                    return image.sprite;
            }
            Transform panel = transform.Find("NguyenLieu");
            Image panelImage = panel != null ? panel.GetComponent<Image>() : null;
            return panelImage != null ? panelImage.sprite : null;
        }

        private void DeleteGeneratedNodes()
        {
            foreach (string name in new[] { "Khung", "ThongTinNangCap", "Vat_Pham_Can", "TenMuc", "NguyenLieu", "Preview", "UpGradeSuccess", "UpGradeFail" })
                DestroyNode(transform.Find(name));
            foreach (string label in BadgeLabels)
                DestroyNode(transform.Find("Badge_" + label));
            for (int index = 0; index < Materials.Length; index++)
                DestroyNode(transform.Find("Dot_" + index));
        }

        private void DestroyNode(Transform node)
        {
            if (node == null)
                return;
            if (Application.isPlaying)
                Destroy(node.gameObject);
            else
                DestroyImmediate(node.gameObject);
        }

        private void AuthorKeptNodes()
        {
            Transform nameNode = transform.Find("Name");
            TMP_Text nameText = nameNode != null ? nameNode.GetComponentInChildren<TMP_Text>(true) : null;
            if (nameText != null)
                nameText.text = "RED POTION";

            Transform closeNode = transform.Find("X");
            if (closeNode != null)
            {
                RectTransform rect = (RectTransform)closeNode;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(330f, 575f);
                rect.sizeDelta = new Vector2(100f, 100f);
            }

            Transform infuseNode = transform.Find("UPGRADE");
            if (infuseNode != null)
            {
                RectTransform rect = (RectTransform)infuseNode;
                rect.anchoredPosition = new Vector2(0f, -330f);
                rect.sizeDelta = new Vector2(420f, 100f);
                TMP_Text label = infuseNode.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = "INFUSE";
                    label.color = new Color(0.29f, 0.18f, 0.11f);
                    RectTransform labelRect = (RectTransform)label.transform;
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = Vector2.zero;
                    labelRect.offsetMax = Vector2.zero;
                }
            }
        }

        private void WireReferences()
        {
            closeButton = transform.Find("X")?.GetComponent<Button>();
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            infuseButton = transform.Find("UPGRADE")?.GetComponent<Button>();
            if (infuseButton != null)
            {
                infuseButton.onClick.RemoveAllListeners();
                infuseButton.onClick.AddListener(InfuseSelected);
            }

            for (int index = 0; index < BadgeLabels.Length; index++)
            {
                badgeValues[index] = FindDeepText(transform.Find("Badge_" + BadgeLabels[index])?.gameObject, "Value");
                BindBadge(transform.Find("Badge_" + BadgeLabels[index]), index);
            }

            Transform panel = transform.Find("NguyenLieu");
            Transform inner = panel != null ? panel.Find("Inside") : null;
            Transform iconFrame = panel != null ? panel.Find("OAnh") : null;
            iconFrame ??= inner != null ? inner.Find("OAnh") : null;
            iconFrame ??= FindDeepChild(panel, "OAnh");
            Transform iconNode = iconFrame != null ? iconFrame.Find("Anh") : null;
            iconNode ??= FindDeepChild(iconFrame, "Anh");
            materialIcon = iconNode != null ? iconNode.GetComponent<Image>() : null;
            materialQuantityText = FindDeepText(iconFrame != null ? iconFrame.gameObject : null, "Soluong");
            materialNameText = FindDeepText(inner != null ? inner.gameObject : null, "Ten");
            materialDescriptionText = FindDeepText(inner != null ? inner.gameObject : null, "Mota");

            Transform bar = inner != null ? inner.Find("TienDo") : null;
            Transform fill = bar != null ? bar.Find("Fill") : null;
            progressFill = fill as RectTransform;

            BindButton(FindDeepChild(transform, "MuiTen_<"), ShowPreviousPage);
            BindButton(FindDeepChild(transform, "MuiTen_>"), ShowNextPage);

            for (int index = 0; index < Materials.Length; index++)
                dots[index] = transform.Find("Dot_" + index)?.GetComponent<Image>();

            previewText = FindDeepText(gameObject, "Preview");
        }

        private static void BindButton(Transform node, UnityEngine.Events.UnityAction action)
        {
            Button button = node != null ? node.GetComponent<Button>() : null;
            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void BindBadge(Transform badge, int index)
        {
            if (badge == null)
                return;
            Button button = badge.GetComponent<Button>();
            if (button == null)
            {
                button = badge.gameObject.AddComponent<Button>();
                button.targetGraphic = badge.GetComponent<Image>();
            }
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectMaterial(index));
        }

        private void BuildBadges()
        {
            Sprite frameText = transform.Find("Name")?.GetComponent<Image>()?.sprite;
            for (int index = 0; index < BadgeLabels.Length; index++)
            {
                RectTransform badge = MakeNode(transform, "Badge_" + BadgeLabels[index],
                    new Vector2((index - 1) * 270f, 480f), new Vector2(250f, 92f));
                Image background = badge.gameObject.AddComponent<Image>();
                background.sprite = frameText;
                background.type = frameText != null ? Image.Type.Sliced : Image.Type.Simple;

                TMP_Text label = MakeText(badge, "Label", BadgeLabels[index], 24,
                    new Color(0.48f, 0.32f, 0.16f), TextAlignmentOptions.Center);
                label.rectTransform.anchoredPosition = new Vector2(0f, 22f);
                label.rectTransform.sizeDelta = new Vector2(230f, 36f);

                TMP_Text value = MakeText(badge, "Value", "-", 42,
                    new Color(0.29f, 0.18f, 0.11f), TextAlignmentOptions.Center);
                value.fontStyle = FontStyles.Bold;
                value.rectTransform.anchoredPosition = new Vector2(0f, -14f);
                value.rectTransform.sizeDelta = new Vector2(230f, 52f);

                Button badgeButton = badge.gameObject.AddComponent<Button>();
                badgeButton.targetGraphic = background;
                int capturedIndex = index;
                badgeButton.onClick.AddListener(() => SelectMaterial(capturedIndex));
            }
        }

        private void BuildMaterialPanel(Sprite lootFrame)
        {
            Sprite frameText = transform.Find("Name")?.GetComponent<Image>()?.sprite;

            RectTransform title = MakeNode(transform, "TenMuc", new Vector2(0f, 330f), new Vector2(460f, 62f));
            Image titleBackground = title.gameObject.AddComponent<Image>();
            titleBackground.sprite = frameText;
            titleBackground.type = frameText != null ? Image.Type.Sliced : Image.Type.Simple;
            TMP_Text titleText = MakeText(title, "Text", "MATERIAL SELECTION", 30,
                new Color(0.35f, 0.23f, 0.10f), TextAlignmentOptions.Center);
            titleText.fontStyle = FontStyles.Bold;
            Stretch(titleText.rectTransform);

            RectTransform panel = MakeNode(transform, "NguyenLieu", new Vector2(0f, 105f), new Vector2(700f, 330f));
            Image panelFrame = panel.gameObject.AddComponent<Image>();
            panelFrame.sprite = lootFrame;
            panelFrame.type = lootFrame != null ? Image.Type.Sliced : Image.Type.Simple;
            panelFrame.color = lootFrame != null ? Color.white : new Color(0.24f, 0.17f, 0.12f);

            RectTransform inner = MakeNode(panel, "Inside", Vector2.zero, new Vector2(652f, 282f));
            Image innerBackground = inner.gameObject.AddComponent<Image>();
            innerBackground.sprite = WhiteSprite;
            innerBackground.color = new Color(0.16f, 0.12f, 0.09f);

            RectTransform iconFrame = MakeNode(inner, "OAnh", new Vector2(-235f, 10f), new Vector2(190f, 190f));
            Image iconFrameImage = iconFrame.gameObject.AddComponent<Image>();
            iconFrameImage.sprite = lootFrame;
            iconFrameImage.type = lootFrame != null ? Image.Type.Sliced : Image.Type.Simple;
            iconFrameImage.color = lootFrame != null ? Color.white : new Color(0.45f, 0.32f, 0.18f);

            RectTransform iconNode = MakeNode(iconFrame, "Anh", Vector2.zero, new Vector2(160f, 160f));
            Image icon = iconNode.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;

            TMP_Text quantity = MakeText(iconFrame, "Soluong", "x0", 30, new Color(1f, 0.84f, 0.37f),
                TextAlignmentOptions.BottomRight);
            quantity.fontStyle = FontStyles.Bold;
            quantity.rectTransform.anchoredPosition = new Vector2(48f, -70f);
            quantity.rectTransform.sizeDelta = new Vector2(84f, 40f);

            TMP_Text name = MakeText(inner, "Ten", "-", 40, new Color(1f, 0.84f, 0.37f),
                TextAlignmentOptions.Left);
            name.fontStyle = FontStyles.Bold;
            name.rectTransform.anchoredPosition = new Vector2(80f, 100f);
            name.rectTransform.sizeDelta = new Vector2(500f, 52f);

            TMP_Text description = MakeText(inner, "Mota", "-", 26, new Color(0.85f, 0.80f, 0.70f),
                TextAlignmentOptions.Left);
            description.rectTransform.anchoredPosition = new Vector2(80f, 22f);
            description.rectTransform.sizeDelta = new Vector2(500f, 116f);
            description.enableWordWrapping = true;

            RectTransform barBackground = MakeNode(inner, "TienDo", new Vector2(80f, -75f),
                new Vector2(ProgressWidth + 20f, 30f));
            Image barBackgroundImage = barBackground.gameObject.AddComponent<Image>();
            barBackgroundImage.sprite = WhiteSprite;
            barBackgroundImage.color = new Color(0.09f, 0.07f, 0.05f);

            RectTransform fill = MakeNode(barBackground, "Fill", Vector2.zero, Vector2.zero);
            fill.anchorMin = new Vector2(0f, 0.5f);
            fill.anchorMax = new Vector2(0f, 0.5f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = new Vector2(4f, 0f);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = WhiteSprite;

            MakeArrow(inner, new Vector2(-365f, 0f), "<");
            MakeArrow(inner, new Vector2(365f, 0f), ">");
        }

        private void BuildDotsAndPreview()
        {
            for (int index = 0; index < Materials.Length; index++)
            {
                RectTransform dot = MakeNode(transform, "Dot_" + index,
                    new Vector2((index - 1) * 36f, -105f), new Vector2(20f, 20f));
                Image dotImage = dot.gameObject.AddComponent<Image>();
                dotImage.sprite = WhiteSprite;
                dotImage.color = index == 0 ? new Color(1f, 0.84f, 0.37f) : new Color(0.42f, 0.36f, 0.27f);
            }

            TMP_Text preview = MakeText(transform, "Preview", "-", 28, new Color(0.48f, 0.35f, 0.20f),
                TextAlignmentOptions.Center);
            preview.rectTransform.anchoredPosition = new Vector2(0f, -170f);
            preview.rectTransform.sizeDelta = new Vector2(700f, 46f);
        }

        private void MakeArrow(Transform parent, Vector2 position, string glyph)
        {
            RectTransform arrow = MakeNode(parent, "MuiTen_" + glyph, position, new Vector2(60f, 90f));
            Image hitArea = arrow.gameObject.AddComponent<Image>();
            hitArea.sprite = WhiteSprite;
            hitArea.color = new Color(0f, 0f, 0f, 0.01f);
            Button button = arrow.gameObject.AddComponent<Button>();
            button.targetGraphic = hitArea;
            TMP_Text glyphText = MakeText(arrow, "Text", glyph, 60, new Color(1f, 0.84f, 0.37f),
                TextAlignmentOptions.Center);
            glyphText.fontStyle = FontStyles.Bold;
            Stretch(glyphText.rectTransform);
        }

        public void Refresh()
        {
            AlchemistUpgradeSystem witch = AlchemistUpgradeSystem.Instance;

            int baseHeal = PlayerStatSystem.Instance != null ? PlayerStatSystem.Instance.PotionHealAmount : BaseHealFallback;
            int healingLevel = witch != null ? witch.HealingLevel : 0;
            int cooldownLevel = witch != null ? witch.CooldownLevel : 0;
            int defenseLevel = witch != null ? witch.DefenseLevel : 0;
            if (badgeValues[0] != null)
                badgeValues[0].text = "+" + Mathf.RoundToInt(baseHeal * (1f + AlchemistUpgradeSystem.GetHealingBonusPercent(healingLevel) / 100f));
            if (badgeValues[1] != null)
                badgeValues[1].text = AlchemistUpgradeSystem.GetCooldownValue(cooldownLevel) + "s";
            if (badgeValues[2] != null)
                badgeValues[2].text = "+" + AlchemistUpgradeSystem.GetShieldValue(defenseLevel);

            BrewMaterial material = Materials[selectedMaterial];
            int owned = AlchemistUpgradeSystem.GetMaterialAmount(
                SaveManager.Instance?.Data?.inventory?.items, material.itemId);
            int currentExp = witch != null ? witch.GetCurrentExp(material.ability) : 0;
            int expToNext = witch != null
                ? AlchemistUpgradeSystem.GetExpToNextLevel(material.ability, witch.GetLevel(material.ability))
                : 1;

            ItemData item = ItemCatalog.Find(material.itemId);
            if (materialIcon != null)
            {
                materialIcon.sprite = item != null ? item.icon : null;
                materialIcon.enabled = materialIcon.sprite != null;
            }
            if (materialNameText != null)
                materialNameText.text = ResolveMaterialName(material).ToUpperInvariant();
            if (materialDescriptionText != null)
                materialDescriptionText.text = item != null && !string.IsNullOrEmpty(item.description)
                    ? item.description
                    : material.fallbackDescription;
            if (materialQuantityText != null)
                materialQuantityText.text = "x" + owned;
            if (progressFill != null)
            {
                float ratio = expToNext > 0 ? Mathf.Clamp01((float)currentExp / expToNext) : 0f;
                progressFill.sizeDelta = new Vector2(4f + (ProgressWidth - 8f) * ratio, 22f);
            }
            for (int index = 0; index < dots.Length; index++)
                if (dots[index] != null)
                    dots[index].color = index == selectedMaterial ? new Color(1f, 0.84f, 0.37f) : new Color(0.42f, 0.36f, 0.27f);
            for (int index = 0; index < BadgeLabels.Length; index++)
            {
                Image background = transform.Find("Badge_" + BadgeLabels[index])?.GetComponent<Image>();
                if (background != null)
                    background.color = index == selectedMaterial
                        ? new Color(1f, 0.92f, 0.70f)
                        : Color.white;
            }
            if (previewText != null)
                previewText.text = FormatPreview(material.ability, witch, currentExp, expToNext);
            if (infuseButton != null)
                infuseButton.interactable = witch != null && owned >= 1 && !witch.IsMaxed(material.ability);
        }

        private static string FormatPreview(WitchAbility ability, AlchemistUpgradeSystem witch, int currentExp, int expToNext)
        {
            if (witch != null && witch.IsMaxed(ability))
                return "Cooldown " + AlchemistUpgradeSystem.GetCooldownValue(witch.CooldownLevel) + "s (MAX)";

            int level = witch != null ? witch.GetLevel(ability) : 0;
            return FormatStatChange(ability, level, level + 1) + "  (EXP " + currentExp + "/" + expToNext + ")";
        }

        private static string FormatStatChange(WitchAbility ability, int before, int after)
        {
            switch (ability)
            {
                case WitchAbility.Healing:
                    return "Heal +" + AlchemistUpgradeSystem.GetHealingBonusPercent(before) + "% -> +" +
                        AlchemistUpgradeSystem.GetHealingBonusPercent(after) + "%";
                case WitchAbility.Cooldown:
                    return "Cooldown " + AlchemistUpgradeSystem.GetCooldownValue(before) + "s -> " +
                        AlchemistUpgradeSystem.GetCooldownValue(after) + "s";
                default:
                    return "Shield +" + AlchemistUpgradeSystem.GetShieldValue(before) + " -> +" +
                        AlchemistUpgradeSystem.GetShieldValue(after);
            }
        }

        private static string ResolveMaterialName(BrewMaterial material)
        {
            ItemData item = ItemCatalog.Find(material.itemId);
            return item != null && !string.IsNullOrEmpty(item.itemName) ? item.itemName : material.fallbackName;
        }

        private static RectTransform MakeNode(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject node = new GameObject(name, typeof(RectTransform));
            RectTransform rect = node.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static TMP_Text MakeText(Transform parent, string name, string content, int size, Color color,
            TextAlignmentOptions alignment)
        {
            GameObject node = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            node.transform.SetParent(parent, false);
            TMP_Text text = node.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.rectTransform.sizeDelta = new Vector2(200f, 50f);
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Transform FindDeepChild(Transform root, string objectName)
        {
            if (root == null)
                return null;
            if (root.name == objectName)
                return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform result = FindDeepChild(root.GetChild(index), objectName);
                if (result != null)
                    return result;
            }
            return null;
        }

        private static TMP_Text FindDeepText(GameObject root, string objectName)
        {
            Transform found = root != null ? FindDeepChild(root.transform, objectName) : null;
            return found != null ? found.GetComponent<TMP_Text>() : null;
        }
    }
}
