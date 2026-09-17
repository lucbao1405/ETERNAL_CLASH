using System;
using UnityEngine;

namespace EternalClash.Core.Save
{
    public sealed class PlayerPrefsSaveRepository : ISaveRepository
    {
        public const string SaveKey = "SAVE_DATA";
        public const string BackupSaveKey = "SAVE_DATA_BACKUP";

        public bool LastLoadFailed { get; private set; }

        public SaveData Load()
        {
            LastLoadFailed = false;
            if (!Exists())
                return null;

            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
                if (data == null)
                    throw new InvalidOperationException("Deserialized save data was null.");
                return data;
            }
            catch (Exception exception)
            {
                LastLoadFailed = true;
                Debug.LogError($"[SAVE] Failed to load '{SaveKey}'. A new save will be created. {exception}");
                return null;
            }
        }

        public void Save(SaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Preserve the last known-good payload before replacing it.
            if (Exists())
                PlayerPrefs.SetString(BackupSaveKey, PlayerPrefs.GetString(SaveKey));

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public bool Exists()
        {
            return PlayerPrefs.HasKey(SaveKey);
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(BackupSaveKey);
            PlayerPrefs.Save();
        }
    }
}
