using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.UI
{
    /// <summary>
    /// Bo anh thanh mau dung chung cho Town va Battle (Resources/UI/HpBar), tach tu
    /// anh goc "UI blood":
    ///   - frame: vien cam den + ruot DEN (phan mau da mat)
    ///   - fill : chi phan do, khong vien; dat khop vao long khung, Filled Horizontal
    /// </summary>
    public static class HpBarSprites
    {
        public const string OriginalSpriteName = "UI blood";
        public const string FillObjectName = "Hp_Fill";
        public const string GhostObjectName = "Hp_Ghost";

        private const string FramePath = "UI/HpBar/UI blood frame";
        private const string FillPath = "UI/HpBar/UI blood fill";
        // Ban trang cua anh ruot (cung hinh dang), de to mau tuy y.
        private const string FillWhitePath = "UI/HpBar/UI blood fill white";

        /// <summary>Mau doan HP vua bi tru, hien ra sau ruot do.</summary>
        public static readonly Color GhostColor = new Color(1f, 0.82f, 0.15f, 1f);

        // Vi tri long khung tren anh frame 2048x333 (ti le 0..1), do luc tach anh.
        private static readonly Vector2 FillAnchorMin = new Vector2(0.02407f, 0.25586f);
        private static readonly Vector2 FillAnchorMax = new Vector2(0.97769f, 0.85435f);

        private static Sprite frameSprite;
        private static Sprite fillSprite;
        private static bool loadAttempted;

        /// <summary>Nap 2 anh. False neu thieu anh nao (vd bi xoa khi merge).</summary>
        public static bool TryLoad(out Sprite frame, out Sprite fill)
        {
            if (!loadAttempted)
            {
                loadAttempted = true;
                frameSprite = Resources.Load<Sprite>(FramePath);
                fillSprite = Resources.Load<Sprite>(FillPath);
                if (frameSprite == null || fillSprite == null)
                    Debug.LogWarning("[HpBar] Khong tim thay Resources/" + FramePath + " hoac " + FillPath + ".");
            }

            frame = frameSprite;
            fill = fillSprite;
            return frame != null && fill != null;
        }

        /// <summary>
        /// Bien 'background' thanh khung va tao lop ruot do ben trong (con dau tien, ve
        /// duoi chu so HP). Tra ve Image ruot de ben goi chinh fillAmount; null neu
        /// thieu anh. Goi lai lan nua thi tra ve lop da tao.
        /// </summary>
        public static Image ApplyTo(Image background)
        {
            if (background == null)
                return null;

            Transform existing = background.transform.Find(FillObjectName);
            if (existing != null)
                return existing.GetComponent<Image>();

            if (!TryLoad(out Sprite frame, out Sprite fill))
                return null;

            background.sprite = frame;
            background.type = Image.Type.Simple;
            background.color = Color.white;
            background.preserveAspect = false;

            var go = new GameObject(FillObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = background.gameObject.layer;

            var rect = (RectTransform)go.transform;
            rect.SetParent(background.transform, false);
            rect.anchorMin = FillAnchorMin;
            rect.anchorMax = FillAnchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();

            Image image = go.GetComponent<Image>();
            image.sprite = fill;
            image.material = background.material;
            image.raycastTarget = false;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
            return image;
        }

        /// <summary>
        /// Tao lop "mau vua mat" mau vang, dat NGAY DUOI ruot do va trung khop vi tri.
        /// Ruot do tut xuong truoc, lop vang o lai mot luc nen doan vua bi tru hien ra
        /// mau vang. Tra ve null neu thieu anh.
        /// </summary>
        public static Image ApplyDamageGhost(Image fill)
        {
            if (fill == null)
                return null;

            Transform parent = fill.transform.parent;
            if (parent == null)
                return null;

            Transform existing = parent.Find(GhostObjectName);
            if (existing != null)
                return existing.GetComponent<Image>();

            // Anh trang de to vang duoc; thieu thi dung luon anh do (van thay lech mau nhe).
            Sprite ghostSprite = Resources.Load<Sprite>(FillWhitePath) ?? fill.sprite;
            if (ghostSprite == null)
                return null;

            var go = new GameObject(GhostObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = fill.gameObject.layer;

            var source = (RectTransform)fill.transform;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.offsetMin = source.offsetMin;
            rect.offsetMax = source.offsetMax;
            rect.localScale = source.localScale;
            // Ve truoc ruot do -> mau vang chi lo ra o doan vua bi tru.
            rect.SetSiblingIndex(source.GetSiblingIndex());

            Image image = go.GetComponent<Image>();
            image.sprite = ghostSprite;
            image.material = fill.material;
            image.color = GhostColor;
            image.raycastTarget = false;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = fill.fillAmount;
            return image;
        }
    }
}
