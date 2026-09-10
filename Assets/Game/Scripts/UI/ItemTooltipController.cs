using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EternalClash.Data;

namespace EternalClash.UI
{
    /// <summary>Shows item details after holding a reward slot for a short time.</summary>
    public sealed class ItemTooltipController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField, Min(0.1f)] private float holdDuration = 0.4f;
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemTypeText;
        [SerializeField] private TMP_Text descriptionText;

        private ItemData item;
        private bool holding;
        private Coroutine showRoutine;
        private Coroutine animationRoutine;
        private Canvas rootCanvas;

        private void Awake()
        {
            rootCanvas = GetComponentInParent<Canvas>();
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
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
            if (tooltipPanel == null)
                return;

            itemNameText.text = string.IsNullOrEmpty(item.itemName) ? item.itemId : item.itemName;
            itemTypeText.text = string.IsNullOrEmpty(item.itemType) ? InferType(item) : item.itemType;
            descriptionText.text = string.IsNullOrEmpty(item.description) ? "No description." : item.description;
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
            if (tooltipPanel != null)
            {
                if (tooltipPanel.GetComponent<CanvasGroup>() == null)
                    tooltipPanel.AddComponent<CanvasGroup>();
                itemNameText ??= FindText(tooltipPanel.transform, "ItemName");
                itemTypeText ??= FindText(tooltipPanel.transform, "ItemType");
                descriptionText ??= FindText(tooltipPanel.transform, "Description");
                if (itemNameText != null && itemTypeText != null && descriptionText != null)
                    return;
            }

            if (rootCanvas == null)
                rootCanvas = FindObjectOfType<Canvas>();
            if (rootCanvas == null)
                return;

            if (tooltipPanel == null)
            {
                tooltipPanel = new GameObject("ItemTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                tooltipPanel.transform.SetParent(rootCanvas.transform, false);
                RectTransform panelRect = tooltipPanel.transform as RectTransform;
                panelRect.sizeDelta = new Vector2(430f, 175f);
                Image background = tooltipPanel.GetComponent<Image>();
                background.color = new Color(0.04f, 0.05f, 0.08f, 0.96f);
                background.raycastTarget = false;
                CreateText("ItemName", panelRect, 30f, TextAlignmentOptions.Center, out itemNameText);
                CreateText("ItemType", panelRect, 22f, TextAlignmentOptions.Center, out itemTypeText);
                CreateText("Description", panelRect, 20f, TextAlignmentOptions.Center, out descriptionText);
            }
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

        private static void CreateText(string name, RectTransform parent, float size, TextAlignmentOptions alignment, out TMP_Text result)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.transform as RectTransform;
            rect.anchorMin = new Vector2(0.08f, 0f);
            rect.anchorMax = new Vector2(0.92f, 1f);
            rect.offsetMin = new Vector2(0f, name == "ItemName" ? 112f : name == "ItemType" ? 73f : 8f);
            rect.offsetMax = new Vector2(0f, name == "ItemName" ? -8f : name == "ItemType" ? -52f : -68f);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            result = text;
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

        private static string InferType(ItemData data)
        {
            if (data == null || string.IsNullOrEmpty(data.itemId))
                return "Item";
            if (data.itemId.IndexOf("ore", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                data.itemId.IndexOf("wood", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                data.itemId.IndexOf("leather", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Material";
            return "Item";
        }
    }
}
