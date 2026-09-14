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



        /// <summary>Khoang cach toi thieu (giay) giua 2 lan ghi save xuong bo nho may.</summary>
        private const float MinSaveInterval = 1f;

        private bool savePending;
        private float lastSaveTime = float.NegativeInfinity;

        /// <summary>
        /// Xin luu, ghi vao cuoi frame. Nhieu lan xin trong 1 frame chi ghi 1 lan; xin
        /// lien tuc (vd nhat 10 dong xu) thi ghi toi da 1 lan moi MinSaveInterval giay.
        /// Du lieu trong bo nho (Data) van cap nhat ngay, chi viec ghi xuong may bi gom.
        /// Can ghi ngay (vd truoc khi xoa save) thi goi Save().
        /// </summary>
        public void RequestSave()
        {
            // Dong bo truong cu -> DTO ngay (chi gan gia tri, re). Lo ren / phu thuy doc
            // data.currency.gold; neu doi toi luc ghi (<= 1s) ho se thay so vang cu va
            // khi tru tien se ghi de tra lai vang vua tieu.
            if (Data != null)
                SaveMigrationManager.SynchronizeDtosFromLegacy(Data);

            savePending = true;
        }

        /// <summary>Ghi ngay neu dang co yeu cau luu chua ghi.</summary>
        public void FlushPendingSave()
        {
            if (savePending)
                Save();
        }

        private void LateUpdate()
        {
            if (savePending && Time.unscaledTime - lastSaveTime >= MinSaveInterval)
                Save();
        }

        // Dien thoai co the kill app bat cu luc nao sau khi xuong nen: ghi het truoc.
        private void OnApplicationPause(bool paused)
        {
            if (paused && Instance == this)
                FlushPendingSave();
        }

        private void OnApplicationQuit()
        {
            if (Instance == this)
                FlushPendingSave();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                FlushPendingSave();
        }

        public void Save()
        {
            savePending = false;
            lastSaveTime = Time.unscaledTime;

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
            savePending = false;
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
            // Bo yeu cau luu dang cho, neu khong LateUpdate se ghi lai du lieu vua xoa.
            savePending = false;
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
