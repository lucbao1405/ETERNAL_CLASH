using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace EternalClash.Monetization
{
    /// <summary>
    /// Trinh phat quang cao tu san xuat (khong dung mang quang cao ben ngoai).
    /// Content do team tu lam, dat vao thu muc con "Ads" cua mot thu muc
    /// Resources bat ki (dang dung: Assets/Game/Resources/Ads):
    ///   - VideoClip (.mp4 keo vao project)  -> uu tien phat video
    ///   - Sprite (.png/.jpg)                -> neu khong co video thi hien anh + dem nguoc
    ///   - Khong co gi                       -> panel gia lap 3s de van test duoc flow
    /// Luat skip video: phai xem toi thieu MinWatchSecondsForSkip giay thi nut X
    /// moi mo - luc do bam X van duoc huong (skip = nhan thuong). Video chet het
    /// cung duoc thuong. Video loi khong chay duoc thi tu quay ve dem nguoc.
    ///
    /// UI: uu tien panel "Selfmade_Ad" dat san trong scene; truoc moi lan phat,
    /// cau truc node duoc kiem tra va tu sua lai neu thieu (tu lai khi bi xoa
    /// nham trong luc test). Scene khong co panel thi tu sinh UI runtime.
    /// </summary>
    internal static class SelfMadeAdPlayer
    {
        /// <summary>Duoc load qua Resources.LoadAll nen chi can nam trong thu muc
        /// con "Ads" cua mot thu muc Resources bat ki.</summary>
        private const string AdResourcesFolder = "Ads";
        private const int ImageCountdownSeconds = 5;
        private const int PlaceholderCountdownSeconds = 3;

        /// <summary>So giay toi thieu phai xem video truoc khi nut skip mo.
        /// Skip sau moc nay van duoc huong thuong.</summary>
        private const int MinWatchSecondsForSkip = 7;

        private static SelfAdHost host;

        internal static void Play(string placement, Action<bool> onResult)
        {
            EnsureHost();
            host.Play(placement, onResult);
        }

        private static void EnsureHost()
        {
            if (host != null)
                return;

            host = new GameObject("SelfMadeAdPlayer (Runtime)").AddComponent<SelfAdHost>();
            UnityEngine.Object.DontDestroyOnLoad(host.gameObject);
        }

        private sealed class SelfAdHost : MonoBehaviour
        {
            private SelfmadeAdPanel panel;
            private bool panelIsRuntimeBuilt;
            private VideoPlayer videoPlayer;
            private RenderTexture videoTexture;
            private Coroutine countdownRoutine;
            private bool skipUnlocked;

            private Action<bool> onResult;
            private bool completed;
            private bool prevAudioPause;
            private float prevTimeScale = 1f;

            public void Play(string placement, Action<bool> result)
            {
                // Chong mo hai ad cung luc - dong ban cu va bao khong thuong.
                CloseWithoutReward(true);

                onResult = result;
                completed = false;
                skipUnlocked = false;

                // Dung toan bo game trong luc phat ad: dong bang time va tam ngu
                // moi am thanh cua game. Tieng ad khong bi anh huong vi VideoPlayer
                // phat bang Direct (scene) hoac duoc bat ignoreListenerPause.
                prevAudioPause = AudioListener.pause;
                prevTimeScale = Time.timeScale;
                AudioListener.pause = true;
                Time.timeScale = 0f;

                panel = FindScenePanel();
                panelIsRuntimeBuilt = panel == null;
                if (panelIsRuntimeBuilt)
                    panel = BuildRuntimePanel();

                // Tu sua cau truc node truoc moi lan phat - chong mat ket noi khi
                // bi xoa/sua nham trong luc test.
                EnsurePanelStructure(panel.transform);
                panel.EnsureWired();
                panel.transform.SetAsLastSibling();
                panel.gameObject.SetActive(true);

                if (panel.CloseButton != null)
                    panel.CloseButton.onClick.AddListener(CloseWithoutReward);

                VideoClip[] clips = Resources.LoadAll<VideoClip>(AdResourcesFolder);
                if (clips != null && clips.Length > 0)
                {
                    PlayClip(clips[UnityEngine.Random.Range(0, clips.Length)]);
                    return;
                }

                // Khong co video: nut X = tu choi (flow van tu dong xong sau dem nguoc).
                // CloseWithoutReward da duoc gan o dau Play() nen khong gan lai lan nua.

                Sprite[] stills = Resources.LoadAll<Sprite>(AdResourcesFolder);
                if (stills != null && stills.Length > 0)
                {
                    if (panel.StillImage != null)
                    {
                        panel.StillImage.gameObject.SetActive(true);
                        panel.StillImage.sprite = stills[UnityEngine.Random.Range(0, stills.Length)];
                        panel.StillImage.preserveAspect = true;
                    }
                    StartCountdown(ImageCountdownSeconds);
                    return;
                }

                // Chua co content tu lam - van cho chay flow de test.
                if (panel.CountdownLabel != null)
                    panel.CountdownLabel.text = "Ad will appear here\n(Resources/Ads)";
                StartCountdown(PlaceholderCountdownSeconds);
            }

            private static SelfmadeAdPanel FindScenePanel()
            {
                foreach (SelfmadeAdPanel candidate in Resources.FindObjectsOfTypeAll<SelfmadeAdPanel>())
                {
                    if (candidate != null && candidate.gameObject.scene.IsValid())
                        return candidate;
                }

                return null;
            }

            private static SelfmadeAdPanel BuildRuntimePanel()
            {
                GameObject root = MonetizationUI.CreateDimmedRoot(MonetizationUI.OverlayCanvas.transform,
                    "SelfMadeAdOverlay");
                SelfmadeAdPanel panel = root.AddComponent<SelfmadeAdPanel>();
                EnsurePanelStructure(panel.transform);
                return panel;
            }

            /// <summary>
            /// Dam bao AdScreen/Video/Still/CountdownLabel/CloseButton ton tai -
            /// node thieu gi se duoc tao lai voi cau hinh mac dinh.
            /// </summary>
            private static void EnsurePanelStructure(Transform panelRoot)
            {
                Transform screen = panelRoot.Find("AdScreen");
                if (screen == null)
                {
                    GameObject screenGO = new GameObject("AdScreen", typeof(RectTransform));
                    screenGO.transform.SetParent(panelRoot, false);
                    RectTransform screenRect = screenGO.GetComponent<RectTransform>();
                    screenRect.anchorMin = screenRect.anchorMax = screenRect.pivot = Vector2.one * 0.5f;
                    screenRect.sizeDelta = new Vector2(620f, 1100f);
                    Image frame = screenGO.AddComponent<Image>();
                    frame.color = Color.black;
                    screen = screenGO.transform;
                }

                Transform videoT = screen.Find("Video");
                if (videoT == null)
                {
                    GameObject videoGO = new GameObject("Video", typeof(RectTransform), typeof(RawImage));
                    videoGO.transform.SetParent(screen, false);
                    var raw = videoGO.GetComponent<RawImage>();
                    raw.raycastTarget = false;
                    StretchFull((RectTransform)videoGO.transform);
                    videoGO.SetActive(false);
                }
                else if (videoT.GetComponent<RawImage>() == null)
                {
                    var raw = videoT.gameObject.AddComponent<RawImage>();
                    raw.raycastTarget = false;
                }

                Transform stillT = screen.Find("Still");
                if (stillT == null)
                {
                    GameObject stillGO = new GameObject("Still", typeof(RectTransform), typeof(Image));
                    stillGO.transform.SetParent(screen, false);
                    stillGO.GetComponent<Image>().raycastTarget = false;
                    StretchFull((RectTransform)stillGO.transform);
                    stillGO.SetActive(false);
                }

                Transform countdownT = screen.Find("CountdownLabel");
                if (countdownT == null)
                {
                    GameObject countdownGO = new GameObject("CountdownLabel", typeof(RectTransform),
                        typeof(TMPro.TextMeshProUGUI));
                    countdownGO.transform.SetParent(screen, false);
                    var rect = (RectTransform)countdownGO.transform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
                    rect.anchoredPosition = new Vector2(0f, -560f);
                    rect.sizeDelta = new Vector2(580f, 140f);
                    var label = countdownGO.GetComponent<TMPro.TextMeshProUGUI>();
                    label.alignment = TMPro.TextAlignmentOptions.Center;
                    label.fontSize = 32;
                    label.color = Color.white;
                    label.raycastTarget = false;
                }

                if (screen.Find("CloseButton") == null)
                {
                    GameObject closeGO = new GameObject("CloseButton", typeof(RectTransform),
                        typeof(Image), typeof(Button));
                    closeGO.transform.SetParent(screen, false);
                    var closeRect = (RectTransform)closeGO.transform;
                    closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = Vector2.one * 0.5f;
                    closeRect.anchoredPosition = new Vector2(250f, 470f);
                    closeRect.sizeDelta = new Vector2(80f, 80f);
                    closeGO.GetComponent<Image>().color = new Color(0.6f, 0.2f, 0.2f);

                    GameObject closeLabel = new GameObject("CloseLabel", typeof(RectTransform),
                        typeof(TMPro.TextMeshProUGUI));
                    closeLabel.transform.SetParent(closeGO.transform, false);
                    var labelRect = (RectTransform)closeLabel.transform;
                    labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = Vector2.one * 0.5f;
                    labelRect.sizeDelta = new Vector2(80f, 80f);
                    var closeText = closeLabel.GetComponent<TMPro.TextMeshProUGUI>();
                    closeText.text = "X";
                    closeText.alignment = TMPro.TextAlignmentOptions.Center;
                    closeText.fontSize = 40;
                    closeText.color = Color.white;
                    closeText.raycastTarget = false;
                }
            }

            private static void StretchFull(RectTransform rect)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            private void StartCountdown(int seconds)
            {
                countdownRoutine = StartCoroutine(RunCountdown(seconds));
            }

            private IEnumerator RunCountdown(int seconds)
            {
                int remaining = seconds;
                while (remaining > 0)
                {
                    if (panel != null && panel.CountdownLabel != null)
                        panel.CountdownLabel.text = $"Reward in {remaining}s";
                    yield return new WaitForSecondsRealtime(1f);
                    remaining--;
                }

                Complete(true);
            }

            private void PlayClip(VideoClip clip)
            {
                videoTexture = new RenderTexture(1080, 1920, 0);

                // Uu tien VideoPlayer dat san trong scene (clip da chon trong
                // Inspector cua nguoi lam game); khong co thi tao tren host.
                if (panel.VideoPlayerComp != null)
                {
                    videoPlayer = panel.VideoPlayerComp;
                    if (videoPlayer.clip == null)
                        videoPlayer.clip = clip;
                }
                else
                {
                    videoPlayer = gameObject.AddComponent<VideoPlayer>();
                    videoPlayer.clip = clip;
                    // Phat dung khi game bi dong bang (timeScale = 0) va giu tieng ad
                    // khong bi cat boi AudioListener.pause da bat o tren (Direct bo qua
                    // AudioListener, giong VideoPlayer dat san trong scene).
                    videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
                    videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
                }

                videoPlayer.playOnAwake = false;
                videoPlayer.renderMode = VideoRenderMode.APIOnly;
                videoPlayer.targetTexture = videoTexture;
                videoPlayer.isLooping = false;
                videoPlayer.loopPointReached += OnVideoFinished;
                videoPlayer.errorReceived += OnVideoError;
                videoPlayer.prepareCompleted += OnPrepared;

                if (panel.VideoImage != null)
                {
                    panel.VideoImage.gameObject.SetActive(true);
                    panel.VideoImage.texture = videoTexture;
                }

                // Luat skip: xem du MinWatchSecondsForSkip giay thi nut X mo va
                // bam X van duoc huong. Video ngan hon moc nay thi het video la
                // duoc thuong luon, nut X khong bao gio mo.
                if (panel.CloseButton != null)
                {
                    panel.CloseButton.interactable = false;
                    // Play() da gan CloseWithoutReward truoc SkipWithReward; neu de
                    // ca hai thi bam X (skip) se bi listener tu choi chay truoc va
                    // nuot ket qua - bo listener tu choi di, X khi skip mo luon
                    // duoc huong theo luat "skip = nhan thuong".
                    panel.CloseButton.onClick.RemoveListener(CloseWithoutReward);
                    panel.CloseButton.onClick.AddListener(SkipWithReward);
                    float clipSeconds = clip.frameRate > 0 ? (float)(clip.frameCount / clip.frameRate) : 0f;
                    float skipAfter = Mathf.Min(MinWatchSecondsForSkip, clipSeconds);
                    if (skipAfter <= 0.5f)
                        UnlockSkip();
                    else
                        countdownRoutine = StartCoroutine(RunSkipUnlock(skipAfter));
                }

                if (panel.CountdownLabel != null)
                    panel.CountdownLabel.text = skipUnlocked ? string.Empty : $"Skip in {MinWatchSecondsForSkip}s";

                // Prepare truoc roi moi Play - cach chay chuan cua VideoPlayer APIOnly.
                videoPlayer.Prepare();
            }

            private IEnumerator RunSkipUnlock(float seconds)
            {
                int remaining = Mathf.CeilToInt(seconds);
                while (remaining > 0)
                {
                    if (panel != null && panel.CountdownLabel != null)
                        panel.CountdownLabel.text = $"Skip in {remaining}s";
                    yield return new WaitForSecondsRealtime(1f);
                    remaining--;
                }

                UnlockSkip();
                countdownRoutine = null;
            }

            private void UnlockSkip()
            {
                skipUnlocked = true;
                if (panel != null && panel.CloseButton != null)
                    panel.CloseButton.interactable = true;
                if (panel != null && panel.CountdownLabel != null)
                    panel.CountdownLabel.text = "You can skip now";
            }

            /// <summary>Bam X SAU khi mo skip: van duoc huong thuong.</summary>
            private void SkipWithReward()
            {
                Complete(true);
            }

            private void OnPrepared(VideoPlayer player)
            {
                player.prepareCompleted -= OnPrepared;
                player.Play();
            }

            /// <summary>Video loi khong chay duoc (codec, file hong...): khong
            /// de flow bi ket - quay ve panel dem nguoc ngan roi van tra thuong.</summary>
            private void OnVideoError(VideoPlayer player, string message)
            {
                Debug.LogWarning("[Ads] VideoPlayer error: " + message);
                TearDownVideoPlayer();
                if (panel != null && panel.VideoImage != null)
                    panel.VideoImage.gameObject.SetActive(false);
                StartCountdown(PlaceholderCountdownSeconds);
            }

            private void OnVideoFinished(VideoPlayer player)
            {
                Complete(true);
            }

            private void TearDownVideoPlayer()
            {
                if (videoPlayer != null)
                {
                    videoPlayer.loopPointReached -= OnVideoFinished;
                    videoPlayer.prepareCompleted -= OnPrepared;
                    videoPlayer.errorReceived -= OnVideoError;
                    videoPlayer.Stop();

                    if (panel != null && panel.VideoPlayerComp == videoPlayer)
                    {
                        // VideoPlayer cua scene: chi stop, khong huy component.
                    }
                    else
                    {
                        Destroy(videoPlayer);
                    }

                    videoPlayer = null;
                }

                if (videoTexture != null)
                {
                    videoTexture.Release();
                    Destroy(videoTexture);
                    videoTexture = null;
                }
            }

            private void CloseWithoutReward()
            {
                CloseWithoutReward(true);
            }

            private void CloseWithoutReward(bool notify)
            {
                Complete(false, notify);
            }

            private void Complete(bool success, bool notify = true)
            {
                if (completed)
                    return;
                completed = true;

                // Tra lai trang thai game nhu truoc khi mo ad.
                AudioListener.pause = prevAudioPause;
                Time.timeScale = prevTimeScale;

                if (countdownRoutine != null)
                {
                    StopCoroutine(countdownRoutine);
                    countdownRoutine = null;
                }

                TearDownVideoPlayer();

                if (panel != null)
                {
                    if (panel.CloseButton != null)
                    {
                        panel.CloseButton.onClick.RemoveListener(SkipWithReward);
                        panel.CloseButton.onClick.RemoveListener(CloseWithoutReward);
                        panel.CloseButton.interactable = true;
                    }
                    if (panel.VideoImage != null)
                    {
                        panel.VideoImage.texture = null;
                        panel.VideoImage.gameObject.SetActive(false);
                    }
                    panel.gameObject.SetActive(false);
                    if (panelIsRuntimeBuilt)
                        Destroy(panel.gameObject);
                    panel = null;
                }

                Action<bool> callback = onResult;
                onResult = null;
                if (notify)
                    callback?.Invoke(success);
            }
        }
    }
}
