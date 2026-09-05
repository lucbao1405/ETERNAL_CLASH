namespace EternalClash.Core.Services
{

    /// <summary>
    /// Interface quản lý Save/Load.
    /// Gameplay chỉ giao tiếp qua interface này.
    /// </summary>
    public interface ISaveService
    {


        bool HasSaveData();



        void SaveGame();



        void LoadGame();



        void DeleteSave();



        string GetSaveVersion();


    }

}