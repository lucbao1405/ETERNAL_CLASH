namespace EternalClash.Core.Services
{
    public interface IGoldService
    {
        int Gold { get; }
        int OreMaterial { get; }
        int LeatherMaterial { get; }

        void AddGold(int amount);
        bool SpendGold(int amount);
        void AddMaterials(int ore, int leather);
        bool SpendMaterials(int ore, int leather);
    }
}
