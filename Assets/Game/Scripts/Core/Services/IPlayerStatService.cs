namespace EternalClash.Core.Services
{
    /// <summary>
    /// Interface quản lý Player Stats.
    /// Gameplay chỉ giao tiếp qua interface này.
    /// </summary>
    public interface IPlayerStatService
    {

        int Level { get; }


        int Experience { get; }



        int Strength { get; }


        int Intelligence { get; }


        int Vitality { get; }


        int Luck { get; }



        int AttackDamage { get; }


        int HealAmount { get; }



        void AddExperience(int amount);



        void AddStrength();


        void AddIntelligence();


        void AddVitality();


        void AddLuck();

    }
}