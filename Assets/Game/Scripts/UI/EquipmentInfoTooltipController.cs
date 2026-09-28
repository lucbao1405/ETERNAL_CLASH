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
    /// Tooltip thong tin vat pham kieu StartToolTip (khung giay + duoi tam giac, giong
    /// tooltip chi so trong panel Town) cho panel "Trang_Bi" cua scene Town:
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

        // Ban cung StartToolTip.prefab trong Resources/UI — chinh sua giao dien truc tiep trong editor.
        private const string StartToolTipPrefabPath = "UI/StartToolTip";

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

                // Tu tat StartToolTip con neu dang de active trong editor.
                Transform ownPotionTooltip = target.Find("StartToolTip");
                if (ownPotionTooltip != null)
                    ownPotionTooltip.gameObject.SetActive(false);
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

            // Neu nguoi dung de StartToolTip con o trang thai active trong editor (de xem
            // truoc) thi tu tat khi vao scene, tranh tooltip placeholder hien loan.
            Transform ownTooltip = slot.Find("StartToolTip");
            if (ownTooltip != null)
                ownTooltip.gameObject.SetActive(false);
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

            // 1) Con "StartToolTip" rieng cua o (tao bang menu Tools/UI hoac tu tao luc chay)
            //    — hien dung tai cho, khong di chuyen, de nguoi dung chinh kich thuoc rieng tung o.
            Transform own = GetOrCreateOwnTooltip(anchorRect);
            if (own != null)
            {
                ShowOwnTooltip(own, title, titleColor, subtitle, stats, desc);
                return;
            }

            // 2) Bubble dung chung (ItemInfoBubble scene / prefab) — logic cu.
            EnsureBuilt();
            if (bubbleRoot == null)
                return;

            // Chi Title va Description la bat buoc; cac dong phu/duoi la tuy chon de co the
            // xua bot trong hierarchy ma khong lam hong tooltip.
            if (titleText == null || descText == null)
            {
                Debug.LogWarning("[ItemInfoBubble] Bubble phai con Tooltip_Title va Tooltip_Description.");
                return;
            }

            titleText.text = title;
            titleText.color = titleColor;

            if (subtitleText != null)
            {
                subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
                subtitleText.text = subtitle ?? string.Empty;
            }

            if (statsText != null)
            {
                statsText.gameObject.SetActive(!string.IsNullOrEmpty(stats));
                statsText.text = stats ?? string.Empty;
            }

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

        private Transform lastOwnTooltip;

        // Tim con StartToolTip cua o; chua co thi TU TAO tu prefab (nam tren o, vua size o)
        // de moi o deu co tooltip, bat ke nguoi dung da tao trong editor hay chua.
        private Transform GetOrCreateOwnTooltip(RectTransform slot)
        {
            Transform own = slot.Find("StartToolTip");
            if (own != null)
                return own;

            GameObject prefab = Resources.Load<GameObject>(StartToolTipPrefabPath);
            if (prefab == null)
                return null;

            GameObject go = Instantiate(prefab, slot);
            go.name = "StartToolTip";

            if (go.transform is RectTransform rt)
            {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 8f);
                rt.localScale = Vector3.one;
                rt.sizeDelta = new Vector2(
                    Mathf.Clamp(slot.rect.width * 2.2f, 240f, 650f),
                    Mathf.Clamp(slot.rect.height * 1.6f, 130f, 260f));
            }

            VerticalLayoutGroup layout = go.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
                Destroy(layout);
            ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
            if (fitter != null)
                Destroy(fitter);

            Canvas canvas = go.GetComponent<Canvas>();
            if (canvas == null)
                canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;

            go.SetActive(false);
            return go.transform;
        }

        // Hien con StartToolTip cua chinh o dang giu (giong tooltip chi so): chi doi chu
        // va bat len, khong di chuyen vi vi tri/size do nguoi dung dat trong Hierarchy.
        private void ShowOwnTooltip(Transform tooltip, string title, Color titleColor, string subtitle, string stats, string desc)
        {
            TMP_Text ownTitle = FindDescendant(tooltip, "Tooltip_Title")?.GetComponent<TMP_Text>();
            TMP_Text ownDesc = FindDescendant(tooltip, "Tooltip_Description")?.GetComponent<TMP_Text>();
            if (ownTitle == null || ownDesc == null)
            {
                Debug.LogWarning("[ItemInfoBubble] StartToolTip tren o thieu Tooltip_Title/Tooltip_Description.");
                return;
            }

            TMP_Text ownSubtitle = FindDescendant(tooltip, "Tooltip_Subtitle")?.GetComponent<TMP_Text>();
            TMP_Text ownStats = FindDescendant(tooltip, "Tooltip_Stats")?.GetComponent<TMP_Text>();

            ownTitle.text = title;
            ownTitle.color = titleColor;

            if (ownSubtitle != null)
            {
                ownSubtitle.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
                ownSubtitle.text = subtitle ?? string.Empty;
            }

            if (ownStats != null)
            {
                ownStats.gameObject.SetActive(!string.IsNullOrEmpty(stats));
                ownStats.text = stats ?? string.Empty;
            }

            ownDesc.gameObject.SetActive(!string.IsNullOrEmpty(desc));
            ownDesc.text = desc ?? string.Empty;

            // Dam bao tooltip con cua o luon ve tren cac o xung quanh.
            Canvas ownCanvas = tooltip.GetComponent<Canvas>();
            if (ownCanvas == null)
                ownCanvas = tooltip.gameObject.AddComponent<Canvas>();
            ownCanvas.overrideSorting = true;
            ownCanvas.sortingOrder = 500;

            if (lastOwnTooltip != null && lastOwnTooltip != tooltip)
                lastOwnTooltip.gameObject.SetActive(false);

            tooltip.gameObject.SetActive(true);
            tooltip.SetAsLastSibling();
            lastOwnTooltip = tooltip;

            bubbleVisible = true;
            lastAnchor = tooltip;
        }

        private void Hide()
        {
            bubbleVisible = false;
            lastAnchor = null;

            if (lastOwnTooltip != null)
            {
                lastOwnTooltip.gameObject.SetActive(false);
                lastOwnTooltip = null;
            }

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

            float tail = tailDownRect != null ? tailDownRect.rect.height : 18f;
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
            if (tailDownRect != null)
                tailDownRect.gameObject.SetActive(above);
            if (tailUpRect != null)
                tailUpRect.gameObject.SetActive(!above);
        }

        // ------------------------------------------------------------------
        // Ban cung StartToolTip.prefab: instantiate va noi cac tham chieu
        // ------------------------------------------------------------------

        private void EnsureBuilt()
        {
            if (bubbleRoot != null)
                return;

            if (rootCanvas == null)
                rootCanvas = FindObjectOfType<Canvas>();
            if (rootCanvas == null)
                return;

            // Uu tien ban cung "ItemInfoBubble" trong scene — tim khap scene de bat chap
            // nguoi dung keo object den dau (root, canvas khac...); khong co thi instantiate prefab.
            Transform existing = FindInScene("ItemInfoBubble");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                GameObject prefab = Resources.Load<GameObject>(StartToolTipPrefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning("[ItemInfoBubble] Khong tim thay Resources/" + StartToolTipPrefabPath + ".prefab.");
                    return;
                }
                go = Instantiate(prefab, rootCanvas.transform);
                go.name = "ItemInfoBubble";
            }

            // Ghep ve canvas cua panel de dat vi tri va thu tu ve dung.
            if (go.transform.parent != rootCanvas.transform)
                go.transform.SetParent(rootCanvas.transform, false);

            bubbleRoot = go.GetComponent<RectTransform>();
            if (bubbleRoot == null)
                return;

            Debug.Log($"[ItemInfoBubble] Dung bubble trong scene: size={bubbleRoot.sizeDelta}, scale={bubbleRoot.localScale}.");

            bubbleGroup = bubbleRoot.GetComponent<CanvasGroup>();
            if (bubbleGroup == null)
                bubbleGroup = go.AddComponent<CanvasGroup>();
            bubbleGroup.blocksRaycasts = false;
            bubbleGroup.interactable = false;

            // Bubble dung cung cung phai ve tren cac panel khac.
            Canvas bubbleCanvas = bubbleRoot.GetComponent<Canvas>();
            if (bubbleCanvas == null)
                bubbleCanvas = go.AddComponent<Canvas>();
            bubbleCanvas.overrideSorting = true;
            bubbleCanvas.sortingOrder = 500;

            go.SetActive(false);

            // Than tooltip chinh la root cua bubble (khung giay).
            bodyRect = bubbleRoot;
            titleText = FindDescendant(go.transform, "Tooltip_Title")?.GetComponent<TMP_Text>();
            subtitleText = FindDescendant(go.transform, "Tooltip_Subtitle")?.GetComponent<TMP_Text>();
            statsText = FindDescendant(go.transform, "Tooltip_Stats")?.GetComponent<TMP_Text>();
            descText = FindDescendant(go.transform, "Tooltip_Description")?.GetComponent<TMP_Text>();
            tailDownRect = FindDescendant(go.transform, "Tail_Down") as RectTransform;
            tailUpRect = FindDescendant(go.transform, "Tail_Up") as RectTransform;
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

        // Tim object theo ten trong toan bo scene hien tai (keo den dau cung tim thay).
        private Transform FindInScene(string objectName)
        {
            Scene scene = gameObject.scene;
            if (!scene.IsValid())
                return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                    return root.transform;

                Transform found = FindDescendant(root.transform, objectName);
                if (found != null)
                    return found;
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
