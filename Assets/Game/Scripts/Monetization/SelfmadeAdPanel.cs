using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace EternalClash.Monetization
{
    /// <summary>
    /// Man hinh phat quang cao tu lam, DAT SAN trong scene (Battle.unity) de
    /// chinh truc tiep trong Editor - giong cach lam Rest_Recover o Town.
    /// SelfMadeAdPlayer tim panel theo ten "Selfmade_Ad"; scene khong co thi
    /// tu sinh UI runtime du phong.
    /// Cau truc con (tu wire theo ten, khong can wire tay):
    ///   Selfmade_Ad (script nay, an mac dinh)
    ///     └─ AdScreen (khung 620x1100)
    ///          ├─ Video (RawImage hien texture; co the gan them VideoPlayer
    ///          │   va chon clip trong Inspector de dinh video can phat)
    ///          ├─ Still (Image - anh fallback, an mac dinh)
    ///          ├─ CountdownLabel
    ///          └─ CloseButton { CloseLabel }
    /// </summary>
    public sealed class SelfmadeAdPanel : MonoBehaviour
    {
        [SerializeField] private RawImage videoImage;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Image stillImage;
        [SerializeField] private TMP_Text countdownLabel;
        [SerializeField] private Button closeButton;

        public RawImage VideoImage => videoImage;
        public VideoPlayer VideoPlayerComp => videoPlayer;
        public Image StillImage => stillImage;
        public TMP_Text CountdownLabel => countdownLabel;
        public Button CloseButton => closeButton;

        private void Awake()
        {
            EnsureWired();
        }

        /// <summary>Chi dung cho fallback runtime: gan widget thay vi tim trong scene.</summary>
        internal void RuntimeInit(RawImage video, Image still, TMP_Text countdown, Button close)
        {
            videoImage = video;
            stillImage = still;
            countdownLabel = countdown;
            closeButton = close;
        }

        /// <summary>
        /// Host goi truoc khi dung: panel nam an san trong scene nen Awake chua
        /// chay - goi ham nay de tim widget con theo ten (idempotent).
        /// </summary>
        public void EnsureWired()
        {
            Transform videoT = FindDeepChild(transform, "Video");
            if (videoImage == null && videoT != null)
            {
                videoImage = videoT.GetComponent<RawImage>();
                // Tu lai: neu RawImage bi xoa mat (sua tay trong scene) thi tao lai,
                // neu khong video chi co tieng khong co hinh.
                if (videoImage == null)
                {
                    videoImage = videoT.gameObject.AddComponent<RawImage>();
                    videoImage.raycastTarget = false;
                }
            }
            if (videoPlayer == null && videoT != null)
                videoPlayer = videoT.GetComponent<VideoPlayer>();
            if (stillImage == null)
                stillImage = FindDeepChild(transform, "Still")?.GetComponent<Image>();
            if (countdownLabel == null)
                countdownLabel = FindDeepChild(transform, "CountdownLabel")?.GetComponent<TMP_Text>();
            if (closeButton == null)
                closeButton = FindDeepChild(transform, "CloseButton")?.GetComponent<Button>();
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
