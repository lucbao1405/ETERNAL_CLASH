using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EternalClash.Monetization
{
    /// <summary>
    /// Bo dung UI runtime chung cho cac popup monetization (offer overlay,
    /// daily gift, IAP shop). Toan bo UI sinh bang code nen khong phai sua
    /// scene - ke khi can doi giao dien chi can sua kich thuoc/mau o day.
    /// </summary>
    internal static class MonetizationUI
    {
        private static Canvas overlayCanvas;

        /// <summary>
        /// Canvas overlay dung rieng cho cac popup monetization, nam tren moi
        /// popup khac (sortingOrder cao) va khong bi huy khi doi scene.
        /// </summary>
        internal static Canvas OverlayCanvas
        {
            get
            {
                if (overlayCanvas != null)
                    return overlayCanvas;

                GameObject root = new GameObject("MonetizationOverlayCanvas");
                Object.DontDestroyOnLoad(root);

                overlayCanvas = root.AddComponent<Canvas>();
                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                overlayCanvas.sortingOrder = 500;

                CanvasScaler scaler = root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;

                root.AddComponent<GraphicRaycaster>();
                EnsureEventSystem();
                return overlayCanvas;
            }
        }

        internal static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;

            new GameObject("EventSystem (Monetization)",
                typeof(EventSystem), typeof(StandaloneInputModule));
        }

        internal static GameObject CreateDimmedRoot(Transform parent, string name)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.75f);
            return root;
        }

        internal static GameObject CreateCenterPanel(Transform parent, Vector2 size)
        {
            GameObject panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = size;

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0.13f, 0.1f, 0.2f, 0.97f);
            return panel;
        }

        internal static Text CreateLabel(Transform parent, string text, int fontSize, Color color,
            Vector2 anchoredPosition, Vector2 size)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);

            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text label = labelObject.AddComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = fontSize;
            label.color = color;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        internal static Button CreateButton(Transform parent, string text, Vector2 anchoredPosition,
            Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new GameObject(text + "Button", typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image background = buttonObject.AddComponent<Image>();
            background.color = color;

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            Text label = CreateLabel(buttonObject.transform, text, 34, Color.white, Vector2.zero, size);
            label.raycastTarget = false;
            return button;
        }
    }
}
