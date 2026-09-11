using System;
using System.Collections.Generic;
using UnityEngine;

namespace EternalClash.Audio
{
    /// <summary>
    /// Du lieu am thanh cua game: nhac nen theo scene, am thanh theo su kien, am
    /// thanh theo tung loai quai. Sua trong Inspector cua Resources/SoundLibrary.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Game/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        public const string ResourcePath = "SoundLibrary";

        [Serializable]
        public class SceneMusic
        {
            [Tooltip("Ten scene (Town, Battle...).")]
            public string sceneName;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.5f;
        }

        [Serializable]
        public class Sound
        {
            public SoundId id;
            [Tooltip("Nhieu clip thi moi lan phat chon ngau nhien 1 clip.")]
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("Do lech cao do ngau nhien, giup am lap lai nghe bot nham chan.")]
            [Range(0f, 0.3f)] public float pitchVariance = 0.05f;
            [Tooltip("Khoang cach toi thieu (giay) giua 2 lan phat, tranh chong am khi xay ra lien tuc.")]
            [Min(0f)] public float minInterval = 0.05f;
        }

        [Serializable]
        public class EnemySounds
        {
            [Tooltip("Ten prefab quai (Slime, Wolf, Goblin Archer). So khop theo ten GameObject.")]
            public string enemyName;
            public AudioClip[] attack;
            public AudioClip[] hit;
            public AudioClip[] death;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0f, 0.3f)] public float pitchVariance = 0.08f;
        }

        [Header("Nhac nen theo scene")]
        public List<SceneMusic> music = new List<SceneMusic>();

        [Header("Am thanh theo su kien")]
        public List<Sound> sounds = new List<Sound>();

        [Header("Am thanh theo loai quai")]
        public List<EnemySounds> enemies = new List<EnemySounds>();

        public SceneMusic FindMusic(string sceneName)
        {
            foreach (SceneMusic entry in music)
            {
                if (entry != null && string.Equals(entry.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }
            return null;
        }

        public Sound FindSound(SoundId id)
        {
            foreach (Sound entry in sounds)
            {
                if (entry != null && entry.id == id)
                    return entry;
            }
            return null;
        }

        /// <summary>Tim bo am thanh cua quai theo ten GameObject (bo "(Clone)").</summary>
        public EnemySounds FindEnemy(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return null;

            string cleanName = objectName.Replace("(Clone)", string.Empty).Trim();
            EnemySounds best = null;
            foreach (EnemySounds entry in enemies)
            {
                if (entry == null || string.IsNullOrEmpty(entry.enemyName))
                    continue;

                if (string.Equals(cleanName, entry.enemyName, StringComparison.OrdinalIgnoreCase))
                    return entry;

                if (best == null && cleanName.IndexOf(entry.enemyName, StringComparison.OrdinalIgnoreCase) >= 0)
                    best = entry;
            }
            return best;
        }
    }
}
