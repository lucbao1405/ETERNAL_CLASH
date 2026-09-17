using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EternalClash.Data;

namespace EternalClash.UI
{
    /// <summary>Shows item details after holding a reward slot for a short time.</summary>
    public sealed class ItemTooltipController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        // Ban cung StartToolTip.prefab trong Resources/UI — chinh sua giao dien truc tiep trong editor.
        private const string PrefabPath = "UI/StartToolTip";

        [SerializeField, Min(0.1f)] private float holdDuration = 0.4f;
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TMP_Text tooltipTitleText;
        [SerializeField] private TMP_Text tooltipDescriptionText;

        private ItemData item;
        private bool holding;
        private bool usesOwnTooltip;
        private Coroutine showRoutine;
        private Coroutine animationRoutine;
        private Canvas rootCanvas;

        private void Awake()
        {
            rootCanvas = GetComponentInParent<Canvas>();

            // Con "StartToolTip" cua o: khoi tao som de neu nguoi dung de object o trang
            // thai active trong editor (de xem truoc) thi no tu TAT khi vao scene.
            Transform own = transform.Find("StartToolTip");
            if (own != null)
            {
                tooltipPanel = own.gameObject;
                usesOwnTooltip = true;
                tooltipPanel.SetActive(false);
            }
            else if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        public void SetItem(ItemData value)
        {
            item = value;
            enabled = value != null;
        }

        public void ClearItem()
        {
            StopHolding();
            item = null;
            enabled = false;
        }

        /// <summary>Opens the existing item detail panel immediately for click-driven UIs.</summary>
        public void ShowItemDetail()
        {
            if (item != null)
                ShowTooltip();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            StopHolding();
            if (item == null)
                return;

            holding = true;
            showRoutine = StartCoroutine(ShowAfterHold());
        }

        public void OnPointerUp(PointerEventData eventData) => StopHolding();

        public void OnPointerExit(PointerEventData eventData) => StopHolding();

        private IEnumerator ShowAfterHold()
        {
            yield return new WaitForSecondsRealtime(holdDuration);
            if (holding && item != null)
                ShowTooltip();
        }

        private void ShowTooltip()
        {
            holding = false;
            EnsureTooltip();
            if (tooltipPanel == null || tooltipTitleText == null || tooltipDescriptionText == null)
                return;

            tooltipTitleText.text = string.IsNullOrEmpty(item.itemName) ? item.itemId : item.itemName;
            tooltipDescriptionText.text = string.IsNullOrEmpty(item.description) ? "No description." : item.description;
            HideEquipmentOnlyRows();

            // Tooltip con cua o da duoc nguoi dung dat vi tri san trong Hierarchy.
            if (!usesOwnTooltip)
                PositionAboveSlot();
            tooltipPanel.SetActive(true);
            tooltipPanel.transform.SetAsLastSibling();

            if (animationRoutine != null)
                StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateTooltip());
        }

        private IEnumerator AnimateTooltip()
        {
            RectTransform rect = tooltipPanel.transform as RectTransform;
            CanvasGroup group = tooltipPanel.GetComponent<CanvasGroup>();
            if (rect == null || group == null)
                yield break;

            rect.localScale = Vector3.zero;
            group.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < 0.18f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.18f);
                t = 1f - (1f - t) * (1f - t);
                rect.localScale = Vector3.one * t;
                group.alpha = t;
                yield return null;
            }
            rect.localScale = Vector3.one;
            group.alpha = 1f;
        }

        private void StopHolding()
        {
            holding = false;
            if (showRoutine != null)
            {
                StopCoroutine(showRoutine);
                showRoutine = null;
            }
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        private void EnsureTooltip()
        {
            if (tooltipPanel == null)
            {
                // 1) Con "StartToolTip" ngay tren o (tao bang menu Tools/UI hoac tu tao luc
                //    chay) — chinh kich thuoc rieng tung o trong Hierarchy.
                Transform own = transform.Find("StartToolTip");
                if (own == null)
                {
                    GameObject prefab = Resources.Load<GameObject>(PrefabPath);
                    if (prefab != null)
                    {
                        GameObject go = Instantiate(prefab, transform);
                        go.name = "StartToolTip";
                        own = go.transform;

                        if (own is RectTransform rt)
                        {
                            rt.anchorMin = new Vector2(0.5f, 1f);
                            rt.anchorMax = new Vector2(0.5f, 1f);
                            rt.pivot = new Vector2(0.5f, 0f);
                            rt.anchoredPosition = new Vector2(0f, 8f);
                            rt.localScale = Vector3.one;
                            RectTransform slotRect = transform as RectTransform;
                            float slotW = slotRect != null ? slotRect.rect.width : 100f;
                            float slotH = slotRect != null ? slotRect.rect.height : 100f;
                            rt.sizeDelta = new Vector2(
                                Mathf.Clamp(slotW * 2.2f, 240f, 650f),
                                Mathf.Clamp(slotH * 1.6f, 130f, 260f));
                        }

                        VerticalLayoutGroup layout = go.GetComponent<VerticalLayoutGroup>();
                        if (layout != null)
                            Destroy(layout);
                        ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
                        if (fitter != null)
                            Destroy(fitter);
                    }
                }

                if (own != null)
                {
                    tooltipPanel = own.gameObject;
                    usesOwnTooltip = true;
                }
                else
                {
                    usesOwnTooltip = false;

                    // 2) Bubble dung chung "ItemInfoBubble" trong scene Town; khong co thi
                    //    instantiate prefab (fallback cu).
                    if (rootCanvas == null)
                        rootCanvas = FindObjectOfType<Canvas>();
                    if (rootCanvas == null)
                        return;

                    Transform existing = FindInScene("ItemInfoBubble");
                    if (existing != null)
                    {
                        tooltipPanel = existing.gameObject;
                    }
                    else
                    {
                        GameObject prefab = Resources.Load<GameObject>(PrefabPath);
                        if (prefab == null)
                        {
                            Debug.LogWarning("[ItemTooltip] Khong tim thay Resources/" + PrefabPath + ".prefab.");
                            return;
                        }
                        tooltipPanel = Instantiate(prefab, rootCanvas.transform);
                        tooltipPanel.name = "StartToolTip";
                    }

                    // Ghep ve canvas cua slot de dat vi tri va thu tu ve dung.
                    if (tooltipPanel.transform.parent != rootCanvas.transform)
                        tooltipPanel.transform.SetParent(rootCanvas.transform, false);
                    Debug.Log($"[ItemTooltip] Dung bubble: {tooltipPanel.name}, size={((RectTransform)tooltipPanel.transform).sizeDelta}.");
                }
                tooltipPanel.SetActive(false);
            }

            if (tooltipPanel.GetComponent<CanvasGroup>() == null)
                tooltipPanel.AddComponent<CanvasGroup>();
            tooltipTitleText ??= FindText(tooltipPanel.transform, "Tooltip_Title");
            tooltipDescriptionText ??= FindText(tooltipPanel.transform, "Tooltip_Description");

            // Dam bao tooltip luon ve tren cac o xung quanh (canvas rieng + overrideSorting).
            Canvas ownCanvas = tooltipPanel.GetComponent<Canvas>();
            if (ownCanvas == null)
                ownCanvas = tooltipPanel.AddComponent<Canvas>();
            ownCanvas.overrideSorting = true;
            ownCanvas.sortingOrder = 500;
        }

        // Tooltip item chi hien ten + mo ta; an cac dong phu/duoi rieng cua bubble Trang_Bi.
        private void HideEquipmentOnlyRows()
        {
            HideRow("Tooltip_Subtitle");
            HideRow("Tooltip_Stats");
            HideRow("Tail_Down");
            HideRow("Tail_Up");
        }

        private void HideRow(string rowName)
        {
            Transform row = tooltipPanel.transform.Find(rowName);
            if (row != null)
                row.gameObject.SetActive(false);
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
                return null;

            var stack = new System.Collections.Generic.Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                Transform cur = stack.Pop();
                if (cur != root && cur.name == name)
                    return cur;
                for (int i = cur.childCount - 1; i >= 0; i--)
                    stack.Push(cur.GetChild(i));
            }
            return null;
        }

        // Tim object theo ten trong toan bo scene hien tai (keo den dau cung tim thay).
        private static Transform FindInScene(string objectName)
        {
            Scene scene = SceneManager.GetActiveScene();
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

        private static TMP_Text FindText(Transform root, string objectName)
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text != null && text.gameObject.name == objectName)
                    return text;
            }
            return null;
        }

        private void PositionAboveSlot()
        {
            RectTransform slot = transform as RectTransform;
            RectTransform tooltipRect = tooltipPanel.transform as RectTransform;
            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            if (slot == null || tooltipRect == null || canvasRect == null)
                return;

            Vector3[] corners = new Vector3[4];
            slot.GetWorldCorners(corners);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(rootCanvas.worldCamera, (corners[1] + corners[2]) * 0.5f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, rootCanvas.worldCamera, out Vector2 localPoint);
            tooltipRect.anchoredPosition = localPoint + new Vector2(0f, tooltipRect.rect.height * 0.5f + 12f);
        }
    }
}
