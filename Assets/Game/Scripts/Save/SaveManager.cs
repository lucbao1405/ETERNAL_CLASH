using System;
using UnityEngine;

using EternalClash.Core.Services;


namespace EternalClash.Core.Save
{

    /// <summary>
    /// Quản lý lưu và tải dữ liệu game.
    /// Truy cập thông qua ISaveService.
    /// </summary>
    public class SaveManager :
        MonoBehaviour,
        ISaveService
    {


        private const string SAVE_VERSION = "1.0.0";


        public static SaveManager Instance;


        public SaveData Data { get; private set; }


        private bool hasSaveData;
        private ISaveRepository repository;

        public event Action<SaveData> SaveLoaded;
        public event Action<SaveData> SaveChanged;
        public event Action<Exception> SaveFailed;



        private void Awake()
        {

            if(Instance != null)
            {
                Destroy(gameObject);
                return;
            }


            Instance = this;


            DontDestroyOnLoad(gameObject);

            repository = new PlayerPrefsSaveRepository();


            RegisterService();


            Load();
        }



        private void RegisterService()
        {

            if(ServiceRegistry.Has<ISaveService>())
            {
                Debug.LogWarning(
                    "SaveService đã tồn tại."
                );

                return;
            }


            ServiceRegistry.Register<ISaveService>(
                this
            );
        }



        public void Save()
        {
            try
            {
                Data ??= new SaveData();
                SaveMigrationManager.SynchronizeDtosFromLegacy(Data);
                repository.Save(Data);
                hasSaveData = true;
                SaveChanged?.Invoke(Data);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SAVE] Failed to save game. {exception}");
                SaveFailed?.Invoke(exception);
            }
        }



        public void Load()
        {
            try
            {
                Data = repository.Load();
                bool failedToLoad = repository is PlayerPrefsSaveRepository playerPrefsRepository && playerPrefsRepository.LastLoadFailed;
                hasSaveData = Data != null;
                Data = SaveMigrationManager.Migrate(Data, out bool migrated);

                if (failedToLoad)
                {
                    // Replace corrupt data with a valid fallback payload immediately.
                    hasSaveData = true;
                    Save();
                }
                else if (hasSaveData && migrated)
                {
                    // Persist cleanup performed while migrating old inventory data.
                    Save();
                }

                SaveLoaded?.Invoke(Data);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SAVE] Unexpected load failure. Using a new save. {exception}");
                Data = SaveMigrationManager.Migrate(new SaveData());
                hasSaveData = false;
                SaveFailed?.Invoke(exception);
                SaveLoaded?.Invoke(Data);
            }
        }



        public void ResetSave()
        {
            repository.Delete();
            Data = new SaveData();
            hasSaveData = false;
            NewGameEquipmentDefaults.Apply(Data);
            Save();
        }



        public bool HasSaveData()
        {
            return hasSaveData;
        }



        public void SaveGame()
        {
            Save();
        }



        public void LoadGame()
        {
            Load();
        }



        public void DeleteSave()
        {
            repository.Delete();
            Data = new SaveData();
            hasSaveData = false;
        }



        public string GetSaveVersion()
        {
            return SAVE_VERSION;
        }

    }

}
