using System.Collections;
using System.Collections.Generic;
using EternalClash.Audio;
using EternalClash.Item;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EternalClash.Core
{
    /// <summary>
    /// Quan ly toan bo am thanh: nhac nen theo scene, am thanh su kien, am thanh quai,
    /// tieng click cua nut bam. Clip lay tu Resources/SoundLibrary.asset.
    ///
    /// Tu tao khi game chay (khong can dat vao scene). Neu scene co san 1 ban
    /// (vd Battle), ban do chi dung de ghi de nhac nen cua scene qua musicOnStart.
    /// Code gameplay goi qua GameAudio.Play(...) - an toan ca khi chua co AudioController.
    /// </summary>
    public sealed class AudioController : Singleton<AudioController>
    {
        private const string PrefMusicVolume = "AUDIO_MUSIC_VOLUME";
        private const string PrefSfxVolume = "AUDIO_SFX_VOLUME";
        private const string PrefMuted = "AUDIO_MUTED";
        private const int SfxPoolSize = 10;
        private const float MusicFadeSeconds = 0.6f;

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Music")]
        [Tooltip("Ghi de nhac nen cua scene chua ban AudioController nay. De trong thi lay theo SoundLibrary.")]
        [SerializeField] private AudioClip musicOnStart;
        [SerializeField] private bool playMusicOnStart = true;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;

        [Header("SFX")]
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        [Header("Data")]
        [SerializeField] private SoundLibrary library;

        public bool IsMuted { get; private set; }
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        private readonly List<AudioSource> sfxPool = new List<AudioSource>();
        private readonly Dictionary<SoundId, float> lastPlayTime = new Dictionary<SoundId, float>();
        private readonly Dictionary<string, float> lastEnemyPlayTime = new Dictionary<string, float>();
        private readonly HashSet<Button> skillButtons = new HashSet<Button>();
        private float currentMusicEntryVolume = 1f;
        private Coroutine musicFade;
        private int nextPoolIndex;
        private bool pendingClick;
        private Vector2 pointerDownPosition;
        private bool pointerDownTracked;
        private bool settingsDirty;
        private int lastButtonFeedbackFrame = -1;

        // ------------------------------------------------------------------
        // Khoi tao
        // ------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;

            var host = new GameObject("AudioController");
            host.AddComponent<AudioController>();
        }

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this)
            {
                // Ban dat san trong scene: chi mang y nghia "nhac nen cua scene nay".
                if (playMusicOnStart && musicOnStart != null)
                    GameAudioOverride.SetSceneMusic(gameObject.scene.name, musicOnStart);
                return;
            }

            if (library == null)
                library = Resources.Load<SoundLibrary>(SoundLibrary.ResourcePath);
            if (library == null)
                Debug.LogWarning("[AUDIO] Khong tim thay Resources/" + SoundLibrary.ResourcePath + " - game se khong co am thanh.");

            LoadSettings();
            EnsureSources();
            ApplyVolumes();

            SceneManager.sceneLoaded += OnSceneLoaded;
            ItemPickup.OnItemCollected += OnItemCollected;
        }

        protected override void OnDestroy()
        {
            if (Instance != this)
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            ItemPickup.OnItemCollected -= OnItemCollected;
            base.OnDestroy();
        }

        private void Start()
        {
            if (Instance == this)
                OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single)
                return;

            // AudioController song qua moi scene: am dang phat do (vd nhac Win dai
            // ~10s) se keo sang scene moi neu khong tat.
            StopAllSfx();
            FlushSettings();
            CacheSkillButtons();
            BindVolumeSliders(scene);
            PlaySceneMusic(scene.name);
        }

        private void StopAllSfx()
        {
            foreach (AudioSource source in sfxPool)
            {
                if (source != null)
                    source.Stop();
            }

            if (sfxSource != null)
                sfxSource.Stop();
        }

        // ------------------------------------------------------------------
        // Thanh am luong trong bang Setting
        // ------------------------------------------------------------------

        private const string MusicSliderParent = "AmLuong_Music";
        private const string SfxSliderParent = "AmLuong_FX";

        /// <summary>
        /// Tim Slider nam duoi GameObject ten AmLuong_Music / AmLuong_FX (bang Setting
        /// o Town) va noi vao am luong. Tim theo ten luc chay nen khong can keo tha
        /// trong Inspector, khong mat khi merge scene.
        /// </summary>
        private void BindVolumeSliders(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Slider slider in root.GetComponentsInChildren<Slider>(true))
                {
                    Transform parent = slider.transform.parent;
                    if (parent == null)
                        continue;

                    if (parent.name == MusicSliderParent)
                        BindSlider(slider, musicVolume, SetMusicVolume);
                    else if (parent.name == SfxSliderParent)
                        BindSlider(slider, sfxVolume, SetSfxVolume);
                }
            }
        }

        private static void BindSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.RemoveListener(onChanged);
            slider.onValueChanged.AddListener(onChanged);
        }

        private void EnsureSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
            }

            // Pool rieng cho SFX de moi am co cao do (pitch) rieng va phat chong nhau duoc.
            while (sfxPool.Count < SfxPoolSize)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                sfxPool.Add(source);
            }
        }

        // ------------------------------------------------------------------
        // Nhac nen
        // ------------------------------------------------------------------

        private void PlaySceneMusic(string sceneName)
        {
            AudioClip overrideClip = GameAudioOverride.GetSceneMusic(sceneName);
            SoundLibrary.SceneMusic entry = library != null ? library.FindMusic(sceneName) : null;

            AudioClip clip = overrideClip != null ? overrideClip : entry?.clip;
            float entryVolume = entry != null ? entry.volume : 0.5f;

            if (clip == null)
            {
                StopMusic();
                return;
            }

            PlayMusic(clip, entryVolume);
        }

        public void PlayMusic(AudioClip clip)
        {
            PlayMusic(clip, currentMusicEntryVolume);
        }

        public void PlayMusic(AudioClip clip, float entryVolume)
        {
            if (clip == null || musicSource == null)
                return;

            currentMusicEntryVolume = Mathf.Clamp01(entryVolume);

            if (musicSource.clip == clip && musicSource.isPlaying)
            {
                ApplyVolumes();
                return;
            }

            if (musicFade != null)
                StopCoroutine(musicFade);
            musicFade = StartCoroutine(FadeToMusic(clip));
        }

        public void StopMusic()
        {
            if (musicSource == null || !musicSource.isPlaying)
                return;

            if (musicFade != null)
                StopCoroutine(musicFade);
            musicFade = StartCoroutine(FadeToMusic(null));
        }

        private IEnumerator FadeToMusic(AudioClip next)
        {
            float target = TargetMusicVolume();

            if (musicSource.isPlaying)
            {
                float start = musicSource.volume;
                for (float t = 0f; t < MusicFadeSeconds; t += Time.unscaledDeltaTime)
                {
                    musicSource.volume = Mathf.Lerp(start, 0f, t / MusicFadeSeconds);
                    yield return null;
                }
                musicSource.Stop();
            }

            if (next == null)
            {
                musicSource.clip = null;
                musicFade = null;
                yield break;
            }

            musicSource.clip = next;
            musicSource.loop = true;
            musicSource.volume = 0f;
            musicSource.Play();

            for (float t = 0f; t < MusicFadeSeconds; t += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(0f, target, t / MusicFadeSeconds);
                yield return null;
            }
            musicSource.volume = target;
            musicFade = null;
        }

        // ------------------------------------------------------------------
        // Am thanh su kien
        // ------------------------------------------------------------------

        public void Play(SoundId id)
        {
            if (id == SoundId.None || library == null)
                return;

            SoundLibrary.Sound sound = library.FindSound(id);
            if (sound == null)
                return;

            if (IsButtonFeedback(id))
                lastButtonFeedbackFrame = Time.frameCount;

            float now = Time.unscaledTime;
            if (lastPlayTime.TryGetValue(id, out float last) && now - last < sound.minInterval)
                return;

            if (PlayClip(PickClip(sound.clips), sound.volume, sound.pitchVariance))
                lastPlayTime[id] = now;
        }

        /// <summary>
        /// Am nut bam tu phat trong handler (ke ca UIClick) thi bo tieng click
        /// toan cung frame, tranh mot lan bam bi phong hai tieng click.
        /// </summary>
        private static bool IsButtonFeedback(SoundId id)
        {
            return id == SoundId.StatSelect || id == SoundId.UpgradeAccept ||
                   id == SoundId.ItemUpgrade || id == SoundId.ChestOpen ||
                   id == SoundId.PanelScroll || id == SoundId.UIClick;
        }

        public void PlayEnemy(GameObject enemy, EnemySound type)
        {
            if (enemy == null || library == null)
                return;

            SoundLibrary.EnemySounds sounds = library.FindEnemy(enemy.name);
            if (sounds == null)
                return;

            AudioClip[] clips = type == EnemySound.Attack ? sounds.attack
                : type == EnemySound.Hit ? sounds.hit
                : type == EnemySound.Taunt ? sounds.taunt
                : sounds.death;

            // Nhieu quai trung don cung luc (Luot kiem) thi chi phat 1 tieng moi loai.
            string key = sounds.enemyName + type;
            float now = Time.unscaledTime;
            if (lastEnemyPlayTime.TryGetValue(key, out float last) && now - last < 0.06f)
                return;

            if (PlayClip(PickClip(clips), sounds.volume, sounds.pitchVariance))
                lastEnemyPlayTime[key] = now;
        }

        // API cu, giu de tuong thich.
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            PlayClip(clip, volumeScale, 0f);
        }

        private bool PlayClip(AudioClip clip, float volume, float pitchVariance)
        {
            if (clip == null || IsMuted || sfxVolume <= 0f)
                return false;

            AudioSource source = NextPoolSource();
            if (source == null)
                return false;

            source.clip = clip;
            source.volume = Mathf.Clamp01(volume) * sfxVolume;
            source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            source.Play();
            return true;
        }

        private AudioSource NextPoolSource()
        {
            if (sfxPool.Count == 0)
                return sfxSource;

            // Uu tien nguon dang ranh; het nguon ranh thi thay nguon cu nhat.
            for (int i = 0; i < sfxPool.Count; i++)
            {
                int index = (nextPoolIndex + i) % sfxPool.Count;
                if (!sfxPool[index].isPlaying)
                {
                    nextPoolIndex = (index + 1) % sfxPool.Count;
                    return sfxPool[index];
                }
            }

            AudioSource oldest = sfxPool[nextPoolIndex];
            nextPoolIndex = (nextPoolIndex + 1) % sfxPool.Count;
            return oldest;
        }

        private static AudioClip PickClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;
            if (clips.Length == 1)
                return clips[0];
            return clips[Random.Range(0, clips.Length)];
        }

        // ------------------------------------------------------------------
        // Su kien tu dong
        // ------------------------------------------------------------------

        private void OnItemCollected(ItemPickup pickup, int amount)
        {
            if (pickup == null)
                return;

            switch (pickup.itemType)
            {
                case ItemType.Gold:
                    Play(SoundId.PickupCoin);
                    break;
                case ItemType.Ore:
                case ItemType.Leather:
                case ItemType.Wood:
                case ItemType.Flower:
                    Play(SoundId.PickupMaterial);
                    break;
                case ItemType.Potion:
                    Play(SoundId.PlayerPotion);
                    break;
            }
        }

        private void Update()
        {
            if (Instance != this)
                return;

            DetectButtonClick();
        }

        private void LateUpdate()
        {
            // Nut bam xu ly onClick trong EventSystem.Update; toi LateUpdate moi biet
            // nut do co phat am rieng hay khong.
            if (!pendingClick)
                return;

            pendingClick = false;
            if (lastButtonFeedbackFrame != Time.frameCount)
                Play(SoundId.UIClick);
        }

        /// <summary>
        /// Tieng click cho moi nut bam trong game, ke ca nut sinh ra luc chay,
        /// ma khong phai gan vao tung nut. Bo qua nut skill (skill co am rieng).
        /// </summary>
        private void DetectButtonClick()
        {
            if (EventSystem.current == null)
                return;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    pointerDownPosition = touch.position;
                    pointerDownTracked = true;
                }
                else if (touch.phase == TouchPhase.Ended)
                {
                    QueueClickIfTapped(touch.position);
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                pointerDownPosition = Input.mousePosition;
                pointerDownTracked = true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                QueueClickIfTapped(Input.mousePosition);
            }
        }

        /// <summary>
        /// Chi tinh la click nut khi tha tay IT NGUYEN CHO (khong qua nguong drag
        /// cua EventSystem) VA diem tha trung nut bam. Vuot world, cuon list
        /// skill... thi im lang, khong phat tieng click o diem tha tay.
        /// </summary>
        private void QueueClickIfTapped(Vector2 upPosition)
        {
            if (!pointerDownTracked)
                return;

            pointerDownTracked = false;

            float dragThreshold = EventSystem.current != null
                ? EventSystem.current.pixelDragThreshold
                : 10f;
            if (Vector2.Distance(pointerDownPosition, upPosition) > dragThreshold)
                return;

            var pointer = new PointerEventData(EventSystem.current) { position = upPosition };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count == 0)
                return;

            Button button = hits[0].gameObject.GetComponentInParent<Button>();
            if (button == null || !button.IsInteractable() || skillButtons.Contains(button))
                return;

            pendingClick = true;
        }

        private void CacheSkillButtons()
        {
            skillButtons.Clear();
            foreach (EternalClash.UI.SkillUIController ui in FindObjectsOfType<EternalClash.UI.SkillUIController>(true))
            {
                if (ui == null || ui.skillButtons == null)
                    continue;
                foreach (Button button in ui.skillButtons)
                {
                    if (button != null)
                        skillButtons.Add(button);
                }
            }
        }

        // ------------------------------------------------------------------
        // Cai dat am luong (luu vao PlayerPrefs)
        // ------------------------------------------------------------------

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            SaveSettings();
            ApplyVolumes();
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            SaveSettings();
            ApplyVolumes();
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            SaveSettings();
            ApplyVolumes();
        }

        private void LoadSettings()
        {
            musicVolume = PlayerPrefs.GetFloat(PrefMusicVolume, musicVolume);
            sfxVolume = PlayerPrefs.GetFloat(PrefSfxVolume, sfxVolume);
            IsMuted = PlayerPrefs.GetInt(PrefMuted, 0) == 1;
        }

        /// <summary>
        /// Chi ghi vao bo nho PlayerPrefs (keo thanh truot goi lien tuc moi frame).
        /// Ghi xuong o dia o FlushSettings: doi scene, app vao nen, thoat game.
        /// </summary>
        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(PrefMusicVolume, musicVolume);
            PlayerPrefs.SetFloat(PrefSfxVolume, sfxVolume);
            PlayerPrefs.SetInt(PrefMuted, IsMuted ? 1 : 0);
            settingsDirty = true;
        }

        private void FlushSettings()
        {
            if (!settingsDirty)
                return;
            settingsDirty = false;
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && Instance == this)
                FlushSettings();
        }

        private void OnApplicationQuit()
        {
            if (Instance == this)
                FlushSettings();
        }

        private float TargetMusicVolume()
        {
            return IsMuted ? 0f : musicVolume * currentMusicEntryVolume;
        }

        private void ApplyVolumes()
        {
            if (musicSource != null && musicFade == null)
                musicSource.volume = TargetMusicVolume();

            float sfx = IsMuted ? 0f : sfxVolume;
            if (sfxSource != null)
                sfxSource.volume = sfx;
        }
    }

    /// <summary>
    /// Nhac nen ghi de theo scene, dang ky boi ban AudioController dat san trong scene
    /// (vd Battle co musicOnStart = Battle.mp3).
    /// </summary>
    internal static class GameAudioOverride
    {
        private static readonly Dictionary<string, AudioClip> sceneMusic = new Dictionary<string, AudioClip>();

        public static void SetSceneMusic(string sceneName, AudioClip clip)
        {
            if (!string.IsNullOrEmpty(sceneName) && clip != null)
                sceneMusic[sceneName] = clip;
        }

        public static AudioClip GetSceneMusic(string sceneName)
        {
            return sceneName != null && sceneMusic.TryGetValue(sceneName, out AudioClip clip) ? clip : null;
        }
    }
}
