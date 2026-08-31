namespace EternalClash.Village
{
    public class GoldSystem
    {
        public static GoldSystem Instance { get; private set; }

        public int Gold { get; set; }
        public int OreMaterial { get; set; }
        public int LeatherMaterial { get; set; }

        public event System.Action<int> OnGoldChanged;
        public event System.Action<int, int> OnMaterialsChanged;

        public void AddGold(int amount) {}
        public void AddMaterials(int ore, int leather) {}
    }
}
