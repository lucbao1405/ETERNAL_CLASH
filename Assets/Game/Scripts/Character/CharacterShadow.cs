using UnityEngine;
using Spine.Unity;

namespace EternalClash.Character
{
    /// <summary>
    /// Bong dem elip mo duoi chan nhan vat kieu PostKnight. Sprite gradient duoc
    /// ve bang code (khong can asset), tu can theo do rong hinh nhan vat va dat
    /// xuong chan moi frame, ho ca voi SpriteRenderer lan MeshRenderer cua Spine.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterShadow : MonoBehaviour
    {
        [SerializeField, Tooltip("Renderer lam moc do chan. De trong se tu chon renderer rong nhat trong children.")]
        private Renderer targetRenderer;

        [SerializeField] private float widthScale = 1.05f;
        [SerializeField, Range(0.05f, 1f)] private float heightScale = 0.34f;
        [SerializeField] private float yOffset = 0.03f;
        [SerializeField, Range(0f, 1f)] private float opacity = 0.62f;
        [SerializeField, Tooltip("-1 = ve sau nhan vat. Neu nhan vat cung sorting layer voi duong dat thi dung 1.")]
        private int sortOrderOffset = -1;

        private Transform shadowTransform;
        private SpriteRenderer shadowRenderer;
        private static Sprite softEllipse;

        public int SortOrderOffset
        {
            get => sortOrderOffset;
            set => sortOrderOffset = value;
        }

        private void Awake()
        {
            shadowTransform = new GameObject("Shadow").transform;
            shadowTransform.SetParent(transform, false);
            shadowRenderer = shadowTransform.gameObject.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = GetSoftEllipseSprite();
            shadowRenderer.color = new Color(0f, 0f, 0f, opacity);
        }

        private void LateUpdate()
        {
            if (targetRenderer == null)
                targetRenderer = FindWidestBodyRenderer();
            if (targetRenderer == null)
                return;

            Bounds bounds = targetRenderer.bounds;
            float width = bounds.extents.x * 2f * widthScale;
            if (width <= 0.0001f)
                return;

            // Shadow la con cua nhan vat nen chia lai cho scale cua cha de kich
            // thuoc dat bang don vi the gioi (prefab co the bi scale khac 1).
            Vector3 parentScale = transform.lossyScale;
            shadowTransform.position = new Vector3(
                bounds.center.x,
                bounds.min.y + yOffset,
                bounds.center.z);
            shadowTransform.localScale = new Vector3(
                width / Mathf.Abs(parentScale.x),
                width * heightScale / Mathf.Abs(parentScale.y),
                1f);

            shadowRenderer.sortingLayerID = targetRenderer.sortingLayerID;
            shadowRenderer.sortingOrder = targetRenderer.sortingOrder + sortOrderOffset;
        }

        private Renderer FindWidestBodyRenderer()
        {
            Renderer chosen = null;
            float chosenBottom = float.MaxValue;
            float chosenWidth = 0f;

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                // Chi tinh Sprite/Mesh (body hoac Spine skeleton), bo qua particle,
                // trail va chinh bong dem vua tao.
                if (!(renderer is SpriteRenderer || renderer is MeshRenderer))
                    continue;
                if (renderer == shadowRenderer)
                    continue;

                // Chan nhan vat la diem thap nhat cua hinh (loai bo thanh mau
                // bay ben tren). Neu bang nhau thi lay hinh rong nhat.
                Bounds bounds = renderer.bounds;
                float bottom = bounds.min.y;
                float width = bounds.extents.x * 2f;

                if (chosen == null
                    || bottom < chosenBottom - 0.01f
                    || (bottom <= chosenBottom + 0.01f && width > chosenWidth))
                {
                    chosen = renderer;
                    chosenBottom = bottom;
                    chosenWidth = width;
                }
            }

            return chosen;
        }

        internal static Sprite GetSoftEllipseSprite()
        {
            // Object Unity bi huy khi thoat Play mode nen truong static phai
            // kiem tra lai bang "== null" (fake null) thay vi reference thuan.
            if (softEllipse != null)
                return softEllipse;

            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            Color32[] pixels = new Color32[size * size];
            float half = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // Chuong soi mong: dac giua, mo dan lien mach den ria (do doc
                    // bang 0 tai ria) -> khong con vien cung nhu falloff plateau.
                    float alpha = Mathf.Pow(Mathf.SmoothStep(1f, 0f, d), 0.6f);

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            // PPM = size de sprite dai dung 1 don vi the gioi, scale truc tiep theo width.
            softEllipse = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            return softEllipse;
        }
    }

    /// <summary>
    /// Bong dem cho nhan vat UI (NPC la SkeletonGraphic trong canvas). SpriteRenderer
    /// khong hoat dong trong UI canvas nen bong la mot Image ANH EM dat TRUOC
    /// SkeletonGraphic trong cung Page (UI con ve sau cha, nen phai dung sibling),
    /// kich thuoc va vi tri lay tu mesh that cua skeleton moi frame.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterShadowUi : MonoBehaviour
    {
        [SerializeField] private float widthScale = 1.15f;
        [SerializeField, Range(0.05f, 1f)] private float heightScale = 0.3f;
        [SerializeField, Range(0f, 1f)] private float opacity = 0.62f;

        private RectTransform targetRect;
        private RectTransform shadowRect;
        private SkeletonGraphic skeleton;

        private void Awake()
        {
            targetRect = transform as RectTransform;
            skeleton = GetComponent<SkeletonGraphic>();

            shadowRect = new GameObject("Shadow", typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
            shadowRect.SetParent(transform.parent, false);
            // Dung truoc chinh NPC trong thu tu sibling -> ve ra sau NPC nhung
            // van tren nen duong (house/ground la sibling truoc do).
            shadowRect.SetSiblingIndex(transform.GetSiblingIndex());

            var image = shadowRect.GetComponent<UnityEngine.UI.Image>();
            image.sprite = CharacterShadow.GetSoftEllipseSprite();
            image.color = new Color(0f, 0f, 0f, opacity);
            image.raycastTarget = false;
        }

        private void LateUpdate()
        {
            Bounds bounds = CalculateVisualBounds();
            if (bounds.size.y <= 0.0001f)
                return;

            // Chan nhan vat = goc local skeleton (0,0), nen tam bong neo vung
            // do thay vi tam bbox (bbox lech vi kiem/khien che phia truoc) va
            // khong theo min.y dao dong khi idle animation - bong phai luon
            // nam yen tren mat dat. Toan bo tinh trong khong gian cha chung
            // (Content) -> khong phu thuoc lossyScale/canvas.
            shadowRect.localPosition = targetRect.localPosition;

            // Be rong theo chieu cao nhan vat (bbox ngang bi phinh boi vu khi);
            // nha cua lon rong thi lay theo bbox ngang.
            float width = Mathf.Max(bounds.size.y * 0.6f, bounds.size.x * 0.55f) * widthScale;
            shadowRect.sizeDelta = new Vector2(width * targetRect.localScale.x, width * heightScale * targetRect.localScale.y);
        }

        private Bounds CalculateVisualBounds()
        {
            // Skeleton thuc su ve la mesh vua render (chiem ca khung act dong);
            // fallback ve rect cua NPC khi mesh chua duoc tao.
            if (skeleton != null && skeleton.IsValid && skeleton.isActiveAndEnabled)
            {
                Mesh mesh = skeleton.GetLastMesh();
                if (mesh != null && mesh.vertexCount > 0)
                    return mesh.bounds;
            }
            return new Bounds(targetRect.rect.center, targetRect.rect.size);
        }
    }

    /// <summary>
    /// Tu them bong cho nhan vat: object mang tag Player/Enemy (bat ke o root hay
    /// con, tinh ve root) va NPC co NPCShopDialogueController. Quet dinh ky de bat
    /// ca doi tuong spawn trong luc choi ma khong phai sua tung noi spawn.
    /// </summary>
    public static class CharacterShadowBootstrapper
    {
        private const float ScanInterval = 0.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            GameObject host = new GameObject("CharacterShadowBootstrap");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<Scanner>();
        }

        private sealed class Scanner : MonoBehaviour
        {
            private float timer;

            private void Update()
            {
                timer += Time.unscaledDeltaTime;
                if (timer < ScanInterval)
                    return;

                timer = 0f;
                Scan();
            }

            private void Scan()
            {
                foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
                    Ensure(go.transform.root.gameObject, -1);
                foreach (GameObject go in GameObject.FindGameObjectsWithTag("Enemy"))
                    Ensure(go.transform.root.gameObject, -1);

                // Nhan vat UI con lai (Nhan_Vat_Chinh o Town...): moi
                // SkeletonGraphic deu la nhan vat can bong. NPC da duoc them o
                // tren nen check GetComponent cho idempotent.
                foreach (SkeletonGraphic skeleton in
                    FindObjectsByType<SkeletonGraphic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (skeleton.GetComponent<CharacterShadowUi>() == null)
                        skeleton.gameObject.AddComponent<CharacterShadowUi>();
                }
            }

            private static void Ensure(GameObject root, int orderOffset)
            {
                CharacterShadow shadow = root.GetComponent<CharacterShadow>();
                if (shadow == null)
                {
                    shadow = root.AddComponent<CharacterShadow>();
                    shadow.SortOrderOffset = orderOffset;
                }
            }
        }
    }
}
