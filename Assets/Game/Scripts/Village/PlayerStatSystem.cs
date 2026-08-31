namespace EternalClash.Village
{
    public class PlayerStatSystem
    {
        public static PlayerStatSystem Instance { get; private set; }

        public int BasicAttackDamage { get; set; }
        public int ChargeDamage { get; set; }
        public int PotionHealAmount { get; set; }
        public int Level { get; set; }
        public int CurrentExp { get; set; }
        public int RequiredExp { get; set; }
        public int StatPoints { get; set; }
        public int Strength { get; set; }
        public int Intelligence { get; set; }
        public int Vitality { get; set; }
        public int Luck { get; set; }
        public float RareDropRate { get; set; }

        public event System.Action OnStatsChanged;

        public int GetResetCost() => 0;
        public bool CanResetStats() => false;
        public void AllocateStrength() {}
        public void AllocateIntelligence() {}
        public void AllocateVitality() {}
        public void AllocateLuck() {}
        public void ResetStats() {}
        public void AddExp(int amount) {}
    }
}
