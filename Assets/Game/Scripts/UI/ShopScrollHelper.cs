using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.UI
{
    public static class ShopScrollHelper
    {
        public static void EnsureScrollRect(GameObject shopPanel)
        {
            if (shopPanel == null)
                return;

            // Panel da co scroll hoat dong (vd ShopPanel goc chua ShopScrollView)
            // thi khong duoc dong vao: viec tao them Viewport/Content va reparent
            // node "Hang" lamroi cac card va hien khung trong.
            ScrollRect existing = shopPanel.GetComponentInChildren<ScrollRect>(true);
            if (existing != null && existing.content != null)
                return;

            ScrollRect scrollRect = shopPanel.GetComponent<ScrollRect>();
            if (scrollRect == null)
                scrollRect = shopPanel.AddComponent<ScrollRect>();

            RectTransform viewport = EnsureViewport(shopPanel.transform);
            RectTransform content = EnsureContent(viewport, shopPanel.transform);

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 30f;
        }

        private static RectTransform EnsureViewport(Transform parent)
        {
            Transform viewportTransform = parent.Find("Viewport");
            if (viewportTransform != null)
                return viewportTransform.GetComponent<RectTransform>();

            GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewportGO.transform.SetParent(parent, false);
            RectTransform viewport = viewportGO.GetComponent<RectTransform>();

            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.sizeDelta = Vector2.zero;
            viewport.pivot = new Vector2(0f, 1f);

            Mask mask = viewportGO.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            Image image = viewportGO.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            return viewport;
        }

        private static RectTransform EnsureContent(RectTransform viewport, Transform shopRoot)
        {
            Transform contentTransform = viewport.Find("Content");
            if (contentTransform != null)
                return contentTransform.GetComponent<RectTransform>();

            GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewport, false);
            RectTransform content = contentGO.GetComponent<RectTransform>();

            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;

            ContentSizeFitter fitter = contentGO.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            Transform hang = shopRoot.Find("Hang");
            if (hang != null && hang.parent != content)
                hang.SetParent(content, false);

            return content;
        }
    }
}
