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

        private const string FramePath = "UI/HpBar/UI blood frame";
        private const string FillPath = "UI/HpBar/UI blood fill";

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
    }
}
