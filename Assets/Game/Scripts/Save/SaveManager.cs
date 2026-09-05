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



        private void Awake()
        {

            if(Instance != null)
            {
                Destroy(gameObject);
                return;
            }


            Instance = this;


            DontDestroyOnLoad(gameObject);


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

            if(Data == null)
            {
                Data = new SaveData();
            }


            hasSaveData = true;


            string json = JsonUtility.ToJson(Data);
            PlayerPrefs.SetString("SAVE_DATA", json);
            PlayerPrefs.Save();
        }



        public void Load()
        {

            string json = PlayerPrefs.GetString("SAVE_DATA", "");


            if(string.IsNullOrEmpty(json))
            {
                Data = new SaveData();
                hasSaveData = false;
                return;
            }


            Data = JsonUtility.FromJson<SaveData>(json);
            hasSaveData = true;
        }



        public void ResetSave()
        {
            Data = new SaveData();
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

            Data = new SaveData();
            hasSaveData = false;


            PlayerPrefs.DeleteKey("SAVE_DATA");
            PlayerPrefs.Save();
        }



        public string GetSaveVersion()
        {
            return SAVE_VERSION;
        }

    }

}
