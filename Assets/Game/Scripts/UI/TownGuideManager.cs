using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EternalClash.UI
{
    [DisallowMultipleComponent]
    public class TownGuideManager : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private Transform player;

        [Header("Navigation")]
        [SerializeField] private TownNavigationController navigation;

        [Header("Guides")]
        [SerializeField] private List<TownGuideData> guides = new List<TownGuideData>();

        [Header("Settings")]
        [SerializeField] private float showDistance = 300f;

        [Header("Bubble Style")]
        [SerializeField] private float edgeMargin = 15f;
        [SerializeField] private float stackSpacing = 125f;
        [SerializeField] private float bobSpeed = 3f;
        [SerializeField] private float bobAmplitude = 12f;

        private RectTransform playerRect;
        private readonly List<RectTransform> guideHolders = new List<RectTransform>();
        private readonly List<Vector2> holderBasePositions = new List<Vector2>();

        private void Awake()
        {
            AutoWireReferences();
        }

        private void Start()
        {
            WireGuideButtons();
            CacheHolders();
            CachePages();
        }

        private readonly List<RectTransform> cachedPages = new List<RectTransform>();

        private void CachePages()
        {
            cachedPages.Clear();

            RectTransform content = playerRect != null ? playerRect.parent as RectTransform : null;
            if (content == null)
                return;

            for (int n = 1; n <= 4; n++)
            {
                if (content.Find("Page_" + n) is RectTransform page)
                    cachedPages.Add(page);
            }
        }

        /// <summary>
        /// Cac dich den (Character_Stop_N) nam ngoai cac Page, nen Page bi khoa
        /// khong lam chung bi an di theo. Chi cho phep chi duong khi dich den
        /// nam tren mot Page da mo khoa, nguoc lai bong noi se chi sai duong.
        /// </summary>
        private bool IsTargetReachable(TownGuideData guide)
        {
            if (guide.target == null || !guide.target.gameObject.activeInHierarchy)
                return false;

            RectTransform content = playerRect != null ? playerRect.parent as RectTransform : null;
            if (content == null || cachedPages == null || cachedPages.Count == 0)
                return true; // khong suy dien duoc trang thi giu hanh vi cu

            float targetX = content.InverseTransformPoint(guide.target.position).x;

            for (int i = 0; i < cachedPages.Count; i++)
            {
                RectTransform page = cachedPages[i];
                if (page == null || !page.gameObject.activeInHierarchy)
                    continue;

                float pageX = content.InverseTransformPoint(page.position).x;
                if (Mathf.Abs(targetX - pageX) <= page.rect.width * 0.5f + 1f)
                    return true;
            }

            return false;
        }

        private void AutoWireReferences()
        {
            if (player == null)
            {
                Transform nv = FindDeepChild(transform.root, "Nhan_Vat_Chinh");
                if (nv != null)
                    player = nv;
            }

            if (player != null)
                playerRect = player as RectTransform ?? player.GetComponent<RectTransform>();

            if (navigation == null)
                navigation = FindObjectOfType<TownNavigationController>();

            AutoWireGuides();
        }

        private void AutoWireGuides()
        {
            Transform guideRoot = transform;

            for (int i = 0; i < guides.Count; i++)
            {
                TownGuideData guide = guides[i];

                if (guide.target == null && !string.IsNullOrEmpty(guide.guideName))
                {
                    Transform target = FindDeepChild(transform.root, guide.guideName);
                    if (target != null)
                        guide.target = target;
                }

                if (guide.icon == null && !string.IsNullOrEmpty(guide.guideName))
                {
                    Transform iconChild = guideRoot.Find(guide.guideName);
                    if (iconChild != null)
                    {
                        Transform iconTransform = FindDeepChild(iconChild, "Icon");
                        if (iconTransform != null)
                            guide.icon = iconTransform.GetComponent<Image>();
                        if (guide.icon == null)
                            guide.icon = iconChild.GetComponentInChildren<Image>(true);
                    }
                }

                if (guide.icon == null && guide.target != null)
                {
                    Transform iconChild = guideRoot.Find(guide.target.name);
                    if (iconChild != null)
                        guide.icon = iconChild.GetComponentInChildren<Image>(true);
                }
            }
        }

        private void WireGuideButtons()
        {
            for (int i = 0; i < guides.Count; i++)
            {
                int index = i;
                TownGuideData guide = guides[index];
                if (guide.icon == null)
                    continue;

                Button button = guide.icon.GetComponent<Button>();
                if (button == null)
                    button = guide.icon.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                button.targetGraphic = guide.icon;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnGuideClicked(index));
            }
        }

        // Bubble đóng băng tại node cao nhất dưới Guide root (ví dụ "Guide/Blacksmith").
        private void CacheHolders()
        {
            RectTransform root = transform as RectTransform;
            guideHolders.Clear();
            holderBasePositions.Clear();

            for (int i = 0; i < guides.Count; i++)
            {
                TownGuideData guide = guides[i];
                RectTransform holder = guide.icon != null ? guide.icon.rectTransform : null;

                if (holder != null && root != null)
                {
                    while (holder.parent != null
                        && holder.parent != root
                        && holder.parent is RectTransform)
                    {
                        holder = (RectTransform)holder.parent;
                    }

                    if (holder == root)
                        holder = guide.icon.rectTransform;
                }

                guideHolders.Add(holder);
                holderBasePositions.Add(holder != null ? holder.anchoredPosition : Vector2.zero);
            }
        }

        private void Update()
        {
            if (playerRect == null)
                return;

            int leftSlot = 0;
            int rightSlot = 0;

            for (int i = 0; i < guides.Count; i++)
                UpdateGuide(guides[i], i, ref leftSlot, ref rightSlot);
        }

        private void UpdateGuide(TownGuideData guide, int index, ref int leftSlot, ref int rightSlot)
        {
            if (guide.target == null || guide.icon == null)
                return;

            RectTransform targetRect = guide.target as RectTransform ?? guide.target.GetComponent<RectTransform>();
            if (targetRect == null)
                return;

            RectTransform holder = index < guideHolders.Count ? guideHolders[index] : null;
            if (holder == null)
                return;

            float direction = targetRect.anchoredPosition.x - playerRect.anchoredPosition.x;
            float distance = Mathf.Abs(direction);
            bool visible = distance > showDistance && IsTargetReachable(guide);
            holder.gameObject.SetActive(visible);
            if (!visible)
                return;

            Vector3 scale = holder.localScale;
            scale.x = Mathf.Abs(scale.x) * (direction < 0f ? -1f : 1f);
            holder.localScale = scale;

            // Kiểu Postknight: bám cạnh màn hình theo phía mục tiêu, xếp chồng
            // khi nhiều mốc cùng bên, và nhún nhẹ nhàng.
            RectTransform root = transform as RectTransform;
            float edgeX = root.rect.width * 0.5f - holder.rect.width * 0.5f - edgeMargin;
            float slot = direction < 0f ? leftSlot++ : rightSlot++;
            float bob = Mathf.Sin(Time.unscaledTime * bobSpeed + index) * bobAmplitude;

            holder.anchoredPosition = new Vector2(
                direction < 0f ? -edgeX : edgeX,
                holderBasePositions[index].y + slot * stackSpacing + bob);
        }

        private void OnGuideClicked(int index)
        {
            if (index < 0 || index >= guides.Count)
                return;

            TownGuideData guide = guides[index];
            if (guide.target == null || !IsTargetReachable(guide))
                return;

            if (navigation != null)
                navigation.MoveTo(guide.target, guide.onArrive.Invoke);
            else
                guide.onArrive?.Invoke();
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
