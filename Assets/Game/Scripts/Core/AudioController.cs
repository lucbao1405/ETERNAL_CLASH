using UnityEngine;

namespace EternalClash.Core
{
    public sealed class AudioController : Singleton<AudioController>
    {
        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Music")]
        [SerializeField] private AudioClip musicOnStart;
        [SerializeField] private bool playMusicOnStart = true;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;

        [Header("SFX")]
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        public bool IsMuted { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this)
            {
                if (musicOnStart != null)
                    Instance.PlayMusic(musicOnStart);

                return;
            }

            EnsureSources();
            ApplyVolumes();

            if (playMusicOnStart && musicOnStart != null)
                PlayMusic(musicOnStart);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null)
                return;

            if (musicSource.clip == clip && musicSource.isPlaying)
                return;

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxSource == null)
                return;

            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            ApplyVolumes();
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
        }

        private void ApplyVolumes()
        {
            float muteMultiplier = IsMuted ? 0f : 1f;

            if (musicSource != null)
                musicSource.volume = musicVolume * muteMultiplier;

            if (sfxSource != null)
                sfxSource.volume = sfxVolume * muteMultiplier;
        }
    }
}