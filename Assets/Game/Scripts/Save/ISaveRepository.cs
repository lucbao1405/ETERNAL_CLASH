namespace EternalClash.Core.Save
{
    public interface ISaveRepository
    {
        SaveData Load();
        void Save(SaveData data);
        bool Exists();
        void Delete();
    }
}
