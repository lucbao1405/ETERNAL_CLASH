using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    public SaveData Data { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void Load()
    {
        string json = PlayerPrefs.GetString("SAVE_DATA", "");

        if (string.IsNullOrEmpty(json))
        {
            Data = new SaveData();
            return;
        }

        Data = JsonUtility.FromJson<SaveData>(json);
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(Data);
        PlayerPrefs.SetString("SAVE_DATA", json);
        PlayerPrefs.Save();
    }

    public void ResetSave()
    {
        Data = new SaveData();
        Save();
    }
}
