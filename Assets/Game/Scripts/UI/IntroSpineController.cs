using Spine.Unity;
using TMPro;
using UnityEngine;

namespace EternalClash.UI
{
    /// Dat san tren cung canvas intro. State 1 (mo dau): choi animation "intro"
    /// 1 lan. Het chuyen state 2 (giu): lap animation "keep", hien text
    /// "TAP TO ENTER GAME" nhap nhay dam nhat. Nguoi choi bam -> vao scene Town.
    public class IntroSpineController : MonoBehaviour
    {
        [SerializeField] private SkeletonGraphic skeleton;
        [SerializeField] private string openingAnimation = "intro";
        [SerializeField] private string holdAnimation = "keep";
        [SerializeField] private TextMeshProUGUI tapPrompt;
        [SerializeField] private float pulseSpeed = 2.5f;
        [Tooltip("1 = phu kin man hinh. Tang/giam de tuy chinh kich thuoc background.")]
        [SerializeField] private float fitScale = 1f;
        [Tooltip("Lap lai vi tri background theo man hinh: 0 = giua, 0.5 = lech nua man hinh.")]
        [SerializeField] private Vector2 fitOffset;
        [Tooltip("Gioi han thoi gian cho intro truoc khi cho phep bam vao game (giay). <=0 thi cho intro chay het.")]
        [SerializeField] private float introMaxDuration = 3f;

        private bool holdState;
        private bool fitApplied;
        private Canvas cachedCanvas;
        private Vector2 lastCanvasSize;
        private float startTime;

        private void Start()
        {
            if (skeleton == null)
                skeleton = GetComponent<SkeletonGraphic>();
            if (skeleton == null)
            {
                Debug.LogWarning("[Intro] Khong co SkeletonGraphic tren object.");
                enabled = false;
                return;
            }

            if (!skeleton.IsValid)
                skeleton.Initialize(false);

            if (skeleton.AnimationState != null)
            {
                skeleton.AnimationState.Complete += OnTrackComplete;
                Spine.TrackEntry playing = skeleton.AnimationState.GetCurrent(0);
                if (playing == null)
                {
                    // SkeletonGraphic co the chua kip bat dau animation mo dau
                    // (Start cua no chay sau) -> tu phat de khong bo qua state mo dau.
                    if (!string.IsNullOrEmpty(openingAnimation))
                        skeleton.AnimationState.SetAnimation(0, openingAnimation, false);
                    else
                        EnterHoldState();
                }
                else if (playing.IsComplete)
                {
                    EnterHoldState();
                }
            }

            if (tapPrompt != null)
                tapPrompt.gameObject.SetActive(false);

            startTime = Time.unscaledTime;

            FitToScreen();
        }

        private void Update()
        {
            // Canvas doi kich thuoc (rotation, scaler tinh muot, resize cua so)
            // -> canh lai cho background van phu kin man hinh.
            if (cachedCanvas == null && skeleton != null)
                cachedCanvas = skeleton.GetComponentInParent<Canvas>();
            if (cachedCanvas != null)
            {
                RectTransform canvasRect = cachedCanvas.transform as RectTransform;
                if (canvasRect == null)
                    return;
                Vector2 size = canvasRect.rect.size;
                if (Mathf.Abs(size.x - lastCanvasSize.x) > 0.5f ||
                    Mathf.Abs(size.y - lastCanvasSize.y) > 0.5f)
                    FitToScreen();
            }

            if (!holdState && introMaxDuration > 0f &&
                Time.unscaledTime - startTime >= introMaxDuration)
            {
                // Intro dai hon mong doi -> cat sang state giu de cho phep bam vao game.
                EnterHoldState();
            }

            if (!holdState)
                return;

            if (tapPrompt != null)
                tapPrompt.alpha = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);

            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
                EternalClash.Core.SceneLoader.LoadScene("Town");
        }

        private void OnTrackComplete(Spine.TrackEntry trackEntry)
        {
            if (holdState || trackEntry.Animation == null)
                return;
            if (trackEntry.TrackIndex != 0 || trackEntry.Animation.Name != openingAnimation)
                return;
            EnterHoldState();
        }

        private void EnterHoldState()
        {
            if (holdState)
                return;
            holdState = true;

            if (skeleton != null && skeleton.IsValid && skeleton.AnimationState != null && !string.IsNullOrEmpty(holdAnimation))
            {
                // Crossfade ngan de khong bo cua animation intro dang chay giua chung.
                Spine.TrackEntry holdEntry = skeleton.AnimationState.SetAnimation(0, holdAnimation, true);
                holdEntry.MixDuration = 0.25f;
            }

            if (tapPrompt != null)
                tapPrompt.gameObject.SetActive(true);
        }

        /// Phu kin man hinh: scale theo bounding box cua skeleton (x/y/w/h trong
        /// spine.json), canh giua boc tranh vao canvas. Chay duoc ca edit mode
        /// (chuot phai component -> Fit To Screen) lan runtime.
        [ContextMenu("Fit To Screen")]
        private void FitToScreen()
        {
            if (skeleton == null || !skeleton.IsValid || skeleton.SkeletonData == null)
                return;

            Spine.SkeletonData data = skeleton.SkeletonData;
            Canvas canvas = skeleton.GetComponentInParent<Canvas>();
            cachedCanvas = canvas;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            RectTransform rect = skeleton.rectTransform;
            if (canvasRect == null || data.Width <= 0f || data.Height <= 0f)
                return;

            lastCanvasSize = canvasRect.rect.size;

            // Mesh cua SkeletonGraphic dung don vi spine goc (chung don vi voi
            // canvas), khong qua scale cua SkeletonDataAsset.
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one;

            float scale = Mathf.Max(canvasRect.rect.width / data.Width, canvasRect.rect.height / data.Height) * fitScale;
            rect.localScale = new Vector3(scale, scale, 1f);

            float contentCenterX = (data.X + data.Width * 0.5f) * scale;
            float contentCenterY = (data.Y + data.Height * 0.5f) * scale;
            rect.anchoredPosition = new Vector2(
                -contentCenterX + fitOffset.x * canvasRect.rect.width,
                -contentCenterY + fitOffset.y * canvasRect.rect.height);
            fitApplied = true;
        }
    }
}
