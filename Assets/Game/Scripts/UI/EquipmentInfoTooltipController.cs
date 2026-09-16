using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using EternalClash.Data;
using EternalClash.Village;

namespace EternalClash.UI
{
    /// <summary>
    /// Bong bong thong tin vat pham kieu PostKnight cho panel "Trang_Bi" cua scene Town:
    ///   - Giu vao o Vu Khi / Khien / Giap / Lon: hien ten vat pham dang mang, chi so
    ///     tang them va mo ta (neu khong co thi bao "No equipment" / "Coming Soon").
    ///   - Giu vao loai thuoc o hang "RED POTION": hien cong dung cua thuoc.
    /// Nha tay ra (hoac keo ra khoi o) thi bong bong an ngay.
    ///
    /// Tu tim doi tuong theo ten giong TownStatPanelController nen KHONG can keo
    /// tham chieu trong Inspector va khong can sua scene.
    /// </summary>
    public sealed class EquipmentInfoTooltipController : MonoBehaviour
    {
        private const string TownSceneName = "Town";

        public static EquipmentInfoTooltipController Instance { get; private set; }

        // O trang bi -> o tuong ung trong EquipmentSystem (Khien tinh la Accessory).
        // "Lon" (mu) khong co trong EquipmentSystem nen khong nam o day, luon bao trong.
        private static readonly Dictionary<string, ItemSlot> SlotItems =
            new Dictionary<string, ItemSlot>(StringComparer.Ordinal)
            {
                { "Sword", ItemSlot.Weapon },
                { "Shield", ItemSlot.Accessory },
                { "Giap", ItemSlot.Armor }
            };

        // Ten hien thi cua tung o trang bi.
        private static readonly Dictionary<string, string> SlotLabels =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Sword", "Weapon" },
                { "Shield", "Shield" },
                { "Giap", "Armor" }
            };

        // Cong dung cac loai thuoc: ten object -> {ten thuoc, mo ta}.
        private static readonly Dictionary<string, string[]> PotionInfo =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                { "Healing_Potion", new[] { "Healing Potion", "Restores health instantly when injured in battle." } },
                { "Cooldown_Potion", new[] { "Cooldown Potion", "Reduces skill cooldown so you can use skills sooner." } },
                { "Defense_Potion", new[] { "Defense Potion", "Increases guarding power for a period of time." } }
            };

        private static readonly Color ItemTitleColor = new Color(0.16f, 0.45f, 0.95f);
        private static readonly Color EmptyTitleColor = new Color(0.40f, 0.42f, 0.47f);
        private static readonly Color SubtitleColor = new Color(0.42f, 0.38f, 0.30f);
        private static readonly Color StatsColor = new Color(0.20f, 0.55f, 0.28f);
        private static readonly Color DescColor = new Color(0.35f, 0.37f, 0.42f);
        private static readonly Color BubbleFillColor = new Color(1f, 0.985f, 0.95f, 0.98f);
        private static readonly Color BubbleBorderColor = new Color(0.28f, 0.24f, 0.20f, 1f);

        private Canvas rootCanvas;
        private RectTransform bubbleRoot;
        private RectTransform bodyRect;
        private RectTransform tailDownRect;
        private RectTransform tailUpRect;
        private TMP_Text titleText;
        private TMP_Text subtitleText;
        private TMP_Text statsText;
        private TMP_Text descText;
        private CanvasGroup bubbleGroup;
        private Sprite roundedSprite;
        private Sprite tailDownSprite;
        private Sprite tailUpSprite;

        private Coroutine showRoutine;
        private Coroutine hideRoutine;
        private Transform lastAnchor;
        private bool bubbleVisible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            // Giong TownStatPanelController: controller bi huy moi khi roi Town nen
            // phai bat sceneLoaded de tu tao lai moi lan vao Town.
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

            if (FindObjectOfType<EquipmentInfoTooltipController>() != null)
                return;

            new GameObject("EquipmentInfoTooltipController (Runtime)")
                .AddComponent<EquipmentInfoTooltipController>();

            Debug.Log("[ItemInfoBubble] Da tu tao controller cho scene Town.");
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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            // Panel bi dong hoac o bi an thi bong bong phai bien theo.
            if (bubbleVisible && (lastAnchor == null || !lastAnchor.gameObject.activeInHierarchy))
                Hide();
        }

        private void Initialize()
        {
            Scene scene = gameObject.scene;
            Transform panel = FindEquipmentPanel(scene);
            if (panel == null)
            {
                Debug.LogWarning("[ItemInfoBubble] Khong tim thay panel 'Trang_Bi' co o vu khi trong scene Town.");
                return;
            }

            rootCanvas = panel.GetComponentInParent<Canvas>();

            WireEquipmentSlot(panel, "Sword");
            WireEquipmentSlot(panel, "Shield");
            WireEquipmentSlot(panel, "Giap");
            WireEquipmentSlot(panel, "Lon");

            int potions = 0;
            foreach (Transform t in panel.GetComponentsInChildren<Transform>(true))
            {
                if (!PotionInfo.ContainsKey(t.name) || t.GetComponent<EquipmentSlotHold>() != null)
                    continue;

                Transform target = t;
                EnsureClickTarget(target, () => ShowPotion(target), Hide);
                potions++;
            }

            Debug.Log($"[ItemInfoBubble] Da noi cac o trang bi va {potions} loai thuoc trong panel Trang_Bi.");
        }

        private static Transform FindEquipmentPanel(Scene scene)
        {
            if (!scene.IsValid())
                return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    // Co hai panel "Trang_Bi"; panel can tim la panel co o "Sword".
                    if (t.name == "Trang_Bi" && FindDescendant(t, "Sword") != null)
                        return t;
                }
            }

            return null;
        }

        private void WireEquipmentSlot(Transform panel, string objectName)
        {
            Transform slot = FindDescendant(panel, objectName);
            if (slot == null)
            {
                Debug.LogWarning($"[ItemInfoBubble] Khong tim thay o '{objectName}' trong panel Trang_Bi.");
                return;
            }

            EnsureClickTarget(slot, () => OnSlotPressed(slot, objectName), Hide);
        }

        private static void EnsureClickTarget(Transform slot, Action onPressed, Action onReleased)
        {
            EquipmentSlotHold hold = slot.GetComponent<EquipmentSlotHold>();
            if (hold == null)
            {
                hold = slot.gameObject.AddComponent<EquipmentSlotHold>();
                // Dam bao o bam duoc: dung lai Image co san (nhieu o da co san Image
                // tren root nen khong duoc them moi), khong thi them mot Image trong suot.
                Image hitArea = slot.GetComponent<Image>();
                if (hitArea == null)
                {
                    hitArea = slot.gameObject.AddComponent<Image>();
                    hitArea.color = Color.clear;
                }
                hitArea.raycastTarget = true;
            }

            hold.Pressed = onPressed;
            hold.Released = onReleased;
        }

        // ------------------------------------------------------------------
        // Noi dung bong bong
        // ------------------------------------------------------------------

        private void OnSlotPressed(Transform slot, string slotName)
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);

            // O "Lon" (sach) chua lam -> chi bao Coming Soon.
            if (slotName == "Lon")
            {
                Show(slot, "Coming Soon", EmptyTitleColor, null, null, null);
                return;
            }

            string label = SlotLabels.TryGetValue(slotName, out string l) ? l : slotName;
            ItemData item = null;
            if (SlotItems.TryGetValue(slotName, out ItemSlot sysSlot) && EquipmentSystem.Instance != null)
                item = EquipmentSystem.Instance.GetEquippedItem(sysSlot);

            if (item == null)
            {
                Show(slot, label, EmptyTitleColor, "No equipment", null,
                    "Visit the Blacksmith to buy or change equipment.");
                return;
            }

            int tier = Mathf.Max(1, Mathf.Max(item.level, Mathf.Max(item.weaponTier, item.armorTier)));
            string subtitle = $"{label} - Lv {tier}";
            if (item.upgradeLevel > 0)
                subtitle += $" (+{item.upgradeLevel})";

            Show(slot, PrettifyName(item), ItemTitleColor, subtitle, BuildStatLine(item), item.description);
        }

        private void ShowPotion(Transform potion)
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);

            if (!PotionInfo.TryGetValue(potion.name, out string[] info))
                return;

            Show(potion, info[0], ItemTitleColor, "Consumable", null, info[1]);
        }

        private void Show(Transform anchor, string title, Color titleColor, string subtitle, string stats, string desc)
        {
            RectTransform anchorRect = anchor as RectTransform;
            if (anchorRect == null)
                return;

            EnsureBuilt();
            if (bubbleRoot == null)
                return;

            titleText.text = title;
            titleText.color = titleColor;

            subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            subtitleText.text = subtitle ?? string.Empty;

            statsText.gameObject.SetActive(!string.IsNullOrEmpty(stats));
            statsText.text = stats ?? string.Empty;

            descText.gameObject.SetActive(!string.IsNullOrEmpty(desc));
            descText.text = desc ?? string.Empty;

            bubbleRoot.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(bodyRect);
            PlaceNear(anchorRect);
            bubbleRoot.SetAsLastSibling();

            bubbleVisible = true;
            lastAnchor = anchor;

            if (showRoutine != null)
                StopCoroutine(showRoutine);
            showRoutine = StartCoroutine(FadeIn());
        }

        private void Hide()
        {
            bubbleVisible = false;
            lastAnchor = null;

            if (showRoutine != null)
            {
                StopCoroutine(showRoutine);
                showRoutine = null;
            }

            if (bubbleRoot != null)
                bubbleRoot.gameObject.SetActive(false);
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < 0.15f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.15f);
                t = 1f - (1f - t) * (1f - t);
                bubbleRoot.localScale = Vector3.one * (0.85f + 0.15f * t);
                bubbleGroup.alpha = t;
                yield return null;
            }

            bubbleRoot.localScale = Vector3.one;
            bubbleGroup.alpha = 1f;
        }

        // ------------------------------------------------------------------
        // Xep dat bong bong gan o vua cham
        // ------------------------------------------------------------------

        private void PlaceNear(RectTransform anchor)
        {
            RectTransform canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
            if (canvasRect == null)
                return;

            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            Camera cam = rootCanvas.worldCamera;
            Vector2 topScreen = RectTransformUtility.WorldToScreenPoint(cam, (corners[1] + corners[2]) * 0.5f);
            Vector2 bottomScreen = RectTransformUtility.WorldToScreenPoint(cam, (corners[0] + corners[3]) * 0.5f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, topScreen, cam, out Vector2 topLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, bottomScreen, cam, out Vector2 bottomLocal);

            float tail = tailDownRect.rect.height;
            float bodyHeight = bodyRect.rect.height;
            float gap = 8f;
            float margin = 16f;

            Rect canvasArea = canvasRect.rect;

            // Uu tien dat tren o; neu vuot dinh man hinh thi dat duoi o.
            bool above = topLocal.y + gap + tail + bodyHeight * 0.5f <= canvasArea.yMax;
            float y = above
                ? topLocal.y + gap + tail + bodyHeight * 0.5f
                : bottomLocal.y - gap - tail - bodyHeight * 0.5f;

            float halfWidth = bodyRect.rect.width * 0.5f;
            float x = Mathf.Clamp(
                (topLocal.x + bottomLocal.x) * 0.5f,
                canvasArea.xMin + margin + halfWidth,
                canvasArea.xMax - margin - halfWidth);

            bubbleRoot.anchoredPosition = new Vector2(x, y);

            // Duoi bong bon chi xuong o (hoac len len neu bong nam duoi o).
            tailDownRect.gameObject.SetActive(above);
            tailUpRect.gameObject.SetActive(!above);
        }

        // ------------------------------------------------------------------
        // Tao giao dien bong bong bang code
        // ------------------------------------------------------------------

        private void EnsureBuilt()
        {
            if (bubbleRoot != null)
                return;

            if (rootCanvas == null)
                rootCanvas = FindObjectOfType<Canvas>();
            if (rootCanvas == null)
                return;

            if (roundedSprite == null)
                roundedSprite = CreateRoundedSprite();
            if (tailDownSprite == null)
                tailDownSprite = CreateTailSprite(true);
            if (tailUpSprite == null)
                tailUpSprite = CreateTailSprite(false);

            bubbleRoot = new GameObject("ItemInfoBubble", typeof(RectTransform), typeof(CanvasGroup))
                .GetComponent<RectTransform>();
            bubbleRoot.SetParent(rootCanvas.transform, false);
            bubbleRoot.anchorMin = new Vector2(0.5f, 0.5f);
            bubbleRoot.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRoot.pivot = new Vector2(0.5f, 0.5f);
            bubbleRoot.sizeDelta = Vector2.zero;
            bubbleRoot.localScale = Vector3.one;

            bubbleGroup = bubbleRoot.GetComponent<CanvasGroup>();
            bubbleGroup.blocksRaycasts = false;
            bubbleGroup.interactable = false;

            bubbleRoot.gameObject.SetActive(false);

            // Than bong bon: nen bo tron co vien toi, chua cac dong chu.
            GameObject body = new GameObject("Body", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            body.transform.SetParent(bubbleRoot, false);
            bodyRect = body.GetComponent<RectTransform>();
            bodyRect.sizeDelta = new Vector2(460f, 0f);

            Image bodyImage = body.GetComponent<Image>();
            bodyImage.sprite = roundedSprite;
            bodyImage.type = Image.Type.Sliced;
            bodyImage.color = BubbleFillColor;
            bodyImage.raycastTarget = false;

            VerticalLayoutGroup layout = body.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 20, 20);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = body.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            titleText = CreateText("Title", bodyRect, 34f, TextAlignmentOptions.Center, ItemTitleColor, true);
            subtitleText = CreateText("Subtitle", bodyRect, 20f, TextAlignmentOptions.Center, SubtitleColor, false);
            statsText = CreateText("Stats", bodyRect, 24f, TextAlignmentOptions.Center, StatsColor, true);
            descText = CreateText("Description", bodyRect, 20f, TextAlignmentOptions.Left, DescColor, false);

            // Hai duoi bong bon (chi xuong hoac len), khong tham gia layout.
            tailDownRect = CreateTail("Tail_Down", bodyRect, tailDownSprite);
            tailUpRect = CreateTail("Tail_Up", bodyRect, tailUpSprite);
        }

        private static RectTransform CreateTail(string name, RectTransform parent, Sprite sprite)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = BubbleFillColor;
            image.raycastTarget = false;

            LayoutElement element = go.GetComponent<LayoutElement>();
            element.ignoreLayout = true;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(36f, 20f);
            rect.localRotation = Quaternion.identity;
            rect.gameObject.SetActive(false);
            return rect;
        }

        private static TMP_Text CreateText(string name, Transform parent, float size,
            TextAlignmentOptions alignment, Color color, bool bold)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            TMP_Text text = go.GetComponent<TMP_Text>();
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            if (bold)
                text.fontStyle = FontStyles.Bold;
            return text;
        }

        // ------------------------------------------------------------------
        // Sprite nen bo tron va duoi bong bon (ve bang code, khong can asset)
        // ------------------------------------------------------------------

        private static Sprite CreateRoundedSprite()
        {
            const int size = 128;
            const int radius = 26;
            const int border = 5;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Khoang cach SDF den hinh chu nhat bo tron.
                    float hx = Mathf.Abs(x - (size - 1) * 0.5f) - (size * 0.5f - radius);
                    float hy = Mathf.Abs(y - (size - 1) * 0.5f) - (size * 0.5f - radius);
                    float ox = Mathf.Max(hx, 0f);
                    float oy = Mathf.Max(hy, 0f);
                    float d = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(hx, hy), 0f) - radius;

                    float inside = Mathf.Clamp01(-d + 0.5f);
                    float band = Mathf.Clamp01(border + d + 0.5f); // 1 trong vien, 0 ben trong

                    Color fill = band > 0.5f ? BubbleBorderColor : Color.white;
                    byte a = (byte)Mathf.RoundToInt(inside * 255f);
                    pixels[y * size + x] = new Color32(
                        (byte)Mathf.RoundToInt(fill.r * 255f),
                        (byte)Mathf.RoundToInt(fill.g * 255f),
                        (byte)Mathf.RoundToInt(fill.b * 255f),
                        a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Vector4 borderRect = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                100f, 0u, SpriteMeshType.FullRect, borderRect);
        }

        private static Sprite CreateTailSprite(bool apexDown)
        {
            const int w = 36;
            const int h = 22;

            Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1);
                    // v = 0 o day (canh rong), v = 1 o dinh (mut).
                    float v = apexDown ? 1f - y / (float)(h - 1) : y / (float)(h - 1);
                    float edge = (1f - v) - Mathf.Abs(u - 0.5f) * 2f;
                    float alpha = Mathf.Clamp01(edge * (w * 0.5f));

                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            return Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        // ------------------------------------------------------------------
        // Tien ich
        // ------------------------------------------------------------------

        private static string PrettifyName(ItemData item)
        {
            string raw = !string.IsNullOrEmpty(item.itemName) ? item.itemName : item.itemId;
            if (string.IsNullOrEmpty(raw))
                return "Item";

            if (raw.IndexOf(' ') >= 0)
                return raw;

            // "iron_sword" -> "Iron Sword" de hien thi dep hon.
            string[] words = raw.Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }

            return string.Join(" ", words);
        }

        private static string BuildStatLine(ItemData item)
        {
            List<string> parts = new List<string>(4);
            if (item.strBonus > 0) parts.Add($"STR +{item.strBonus}");
            if (item.intBonus > 0) parts.Add($"INT +{item.intBonus}");
            if (item.vitBonus > 0) parts.Add($"VIT +{item.vitBonus}");
            if (item.luckBonus > 0) parts.Add($"LUCK +{item.luckBonus}");
            return parts.Count > 0 ? string.Join("   ", parts) : null;
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

        /// <summary>
        /// Chuyen giu (nhan xuong hien bong bong, nha ra thi an) cua mot o trang bi
        /// len controller. OnPointerExit de keo ngut tay ra khoi o cung an bong bong.
        /// </summary>
        private sealed class EquipmentSlotHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
        {
            public Action Pressed;
            public Action Released;

            public void OnPointerDown(PointerEventData eventData) => Pressed?.Invoke();

            public void OnPointerUp(PointerEventData eventData) => Released?.Invoke();

            public void OnPointerExit(PointerEventData eventData) => Released?.Invoke();
        }
    }
}
