using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalClash.Enemy;

namespace EternalClash.UI
{
    /// <summary>
    /// Thanh mau Boss kieu Postknight: avatar tron + thanh do co khung + so HP
    /// "current/max", xuat hien o dau man hinh khi boss chinh thuc tran dau, an di
    /// khi boss chet. Uu tien ban cung "BossHealthBar" da dung trong scene (de chinh
    /// kich thuoc trong Inspector); neu scene thieu thi tu tao tai runtime nhu cu.
    /// BossController goi BossHealthBarUI.ShowFor() khi boss lo mat dat / an don dau.
    /// </summary>
    public class BossHealthBarUI : MonoBehaviour
    {
        private static BossHealthBarUI instance;
        private static Sprite circleSprite;
        private static bool suppressed;

        /// <summary>
        /// An/hien lai thanh mau khi co lop phu dac biet (video quang cao) -
        /// khong dung sortingOrder de tranh tranh chap voi bat ky canvas nao.
        /// </summary>
        public static void SetSuppressed(bool value)
        {
            suppressed = value;
            if (instance != null)
                instance.gameObject.SetActive(!value && instance.boundHealth != null);
        }

        private const float GhostHoldDuration = 0.5f;
        private const float GhostDrainSpeed = 1.6f;

        // Am hon moi popup UI (MainCanvas, Victory/Lose, ad offer...) nhung van
        // tren the gioi game vi la Screen Space Overlay.
        private const int BarSortingOrder = -50;

        private EnemyHealthSystem boundHealth;
        private Image fillImage;
        private Image ghostFill;
        private TMP_Text hpText;
        private float ghostHoldTimer;
        private bool fillInitialized;

        /// <summary>Hien thanh mau cua boss (tu tao UI lan dau tien neu chua co).</summary>
        public static void ShowFor(EnemyHealthSystem health)
        {
            EnsureCreated();
            instance.Bind(health);
        }

        /// <summary>An thanh mau (goi khi boss chet).</summary>
        public static void Hide()
        {
            if (instance != null)
            {
                instance.Unbind();
                instance.gameObject.SetActive(false);
            }
        }

        private static void EnsureCreated()
        {
            if (instance != null)
                return;

            // Uu tien ban cung trong scene; chi tu tao runtime khi scene chua co.
            foreach (BossHealthBarUI existing in Resources.FindObjectsOfTypeAll<BossHealthBarUI>())
            {
                if (existing.gameObject.scene.IsValid())
                {
                    instance = existing;
                    instance.HookupExisting();
                    return;
                }
            }

            var root = new GameObject("BossHealthBar", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = BarSortingOrder;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            instance = root.AddComponent<BossHealthBarUI>();
            instance.BuildUI(root.transform as RectTransform);
        }

        private void BuildUI(RectTransform root)
        {
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            // Cum avatar + thanh, dat tren cung giua man hinh.
            RectTransform holder = CreateRect("Holder", root);
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 1f);
            holder.pivot = new Vector2(0.5f, 1f);
            holder.anchoredPosition = new Vector2(0f, -50f);
            holder.sizeDelta = new Vector2(780f, 72f);

            // Avatar tron (placeholder: trong den vien trang - doi anh chan dung boss sau).
            RectTransform avatar = CreateRect("Avatar", holder);
            avatar.anchorMin = new Vector2(0f, 0.5f);
            avatar.anchorMax = new Vector2(0f, 0.5f);
            avatar.pivot = new Vector2(0.5f, 0.5f);
            avatar.anchoredPosition = Vector2.zero;
            avatar.sizeDelta = new Vector2(76f, 76f);
            Image avatarImage = avatar.gameObject.AddComponent<Image>();
            avatarImage.sprite = GetCircleSprite();
            avatarImage.raycastTarget = false;

            // Thanh mau dung chung bo khung + ruot do voi thanh mau Player (Resources/UI/HpBar).
            RectTransform bar = CreateRect("Bar", holder);
            bar.anchorMin = new Vector2(1f, 0.5f);
            bar.anchorMax = new Vector2(1f, 0.5f);
            bar.pivot = new Vector2(1f, 0.5f);
            bar.anchoredPosition = new Vector2(-70f, 0f);
            bar.sizeDelta = new Vector2(700f, 54f);
            Image barImage = bar.gameObject.AddComponent<Image>();
            fillImage = HpBarSprites.ApplyTo(barImage);
            ghostFill = HpBarSprites.ApplyDamageGhost(fillImage);

            // So HP ben trong thanh, canh phai nhu Postknight.
            if (fillImage != null)
            {
                RectTransform textRect = CreateRect("HP_Text", (RectTransform)fillImage.transform.parent);
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = new Vector2(-24f, 0f);
                hpText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
                hpText.alignment = TextAlignmentOptions.Right;
                hpText.fontSize = 28f;
                hpText.color = Color.white;
                hpText.raycastTarget = false;
            }

            gameObject.SetActive(false); // an di cho den khi co boss tran dau
        }

        /// <summary>Noi reference vao ban cung trong scene (Holder/Avatar/Bar/Hp_Fill...).</summary>
        private void HookupExisting()
        {
            RectTransform root = (RectTransform)transform;

            // Ep canvas cua ban trong scene xuong duoi moi popup (ban trong scene
            // co the bi dat sortingOrder cao hon hoac la canvas con khac).
            Canvas ownCanvas = GetComponent<Canvas>();
            if (ownCanvas != null)
            {
                ownCanvas.overrideSorting = true;
                ownCanvas.sortingOrder = BarSortingOrder;
            }

            Transform bar = root.Find("Holder/Bar");
            if (bar != null)
            {
                Transform fill = bar.Find(HpBarSprites.FillObjectName);
                Transform ghost = bar.Find(HpBarSprites.GhostObjectName);
                fillImage = fill ? fill.GetComponent<Image>() : null;
                ghostFill = ghost ? ghost.GetComponent<Image>() : null;
                hpText = bar.GetComponentInChildren<TMP_Text>(true);
            }

            Transform avatar = root.Find("Holder/Avatar");
            Image avatarImage = avatar ? avatar.GetComponent<Image>() : null;
            if (avatarImage != null && avatarImage.sprite == null)
                avatarImage.sprite = GetCircleSprite(); // placeholder khi chua gan anh chan boss

            gameObject.SetActive(false); // an di cho den khi co boss tran dau
        }

        private void Bind(EnemyHealthSystem health)
        {
            Unbind();
            boundHealth = health;
            fillInitialized = false;

            if (boundHealth == null)
            {
                gameObject.SetActive(false);
                return;
            }

            boundHealth.OnHealthChanged += UpdateHealth;
            UpdateHealth(boundHealth.CurrentHealth, boundHealth.MaxHealth);
            gameObject.SetActive(!suppressed);
        }

        private void Unbind()
        {
            if (boundHealth != null)
                boundHealth.OnHealthChanged -= UpdateHealth;
            boundHealth = null;
        }

        private void Update()
        {
            // Boss bi huy ma khong bao chet (huy scene): tu an di.
            if (boundHealth == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (ghostFill == null || fillImage == null || !fillInitialized)
                return;

            // Lop vang "mau vua mat" giong thanh mau Player: tut ngay, giu 0.5s roi rut.
            float red = fillImage.fillAmount;
            float ghost = ghostFill.fillAmount;
            if (ghost <= red)
            {
                if (!Mathf.Approximately(ghost, red))
                    ghostFill.fillAmount = red;
                ghostHoldTimer = 0f;
                return;
            }

            if (ghostHoldTimer > 0f)
            {
                ghostHoldTimer -= Time.unscaledDeltaTime;
                return;
            }

            ghostFill.fillAmount = Mathf.MoveTowards(ghost, red, GhostDrainSpeed * Time.unscaledDeltaTime);
        }

        private void UpdateHealth(int current, int max)
        {
            if (hpText != null)
                hpText.SetText("{0}/{1}", current, max);

            if (fillImage != null)
            {
                float target = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
                bool lostHp = fillInitialized && target < fillImage.fillAmount;
                fillImage.fillAmount = target;

                if (ghostFill != null)
                {
                    if (!fillInitialized || ghostFill.fillAmount < target)
                        ghostFill.fillAmount = target;
                    else if (lostHp)
                        ghostHoldTimer = GhostHoldDuration;
                }

                fillInitialized = true;
            }
        }

        private void OnDestroy()
        {
            Unbind();
            if (instance == this)
                instance = null;
        }

        private static RectTransform CreateRect(string name, RectTransform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Sprite tron vien trang (placeholder cho avatar boss), tao 1 lan dung chung.</summary>
        private static Sprite GetCircleSprite()
        {
            if (circleSprite != null)
                return circleSprite;

            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.5f - 3f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(radius - dist + 1f);
                    bool isRim = dist > radius - 4f;
                    Color color = isRim
                        ? new Color(1f, 1f, 1f, alpha)
                        : new Color(0.18f, 0.16f, 0.20f, alpha);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return circleSprite;
        }
    }
}
