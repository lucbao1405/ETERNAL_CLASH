using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Village
{
    public class PlayerStatSystem : MonoBehaviour
    {
        public static PlayerStatSystem Instance { get; private set; }

        public int BasicAttackDamage { get; private set; }
        public int ChargeDamage { get; private set; }
        public int PotionHealAmount { get; private set; }
        public int Level { get; private set; }
        public int CurrentExp { get; private set; }
        public int RequiredExp { get; private set; }
        public int StatPoints { get; private set; }
        public int Strength { get; private set; }
        public int Intelligence { get; private set; }
        public int Vitality { get; private set; }
        public int Luck { get; private set; }
        public float RareDropRate { get; private set; }

        public event System.Action OnStatsChanged;

        private const int BASE_ATTACK = 5;
        private const int BASE_CHARGE = 30;
        private const int BASE_POTION_HEAL = 30;
        private const int EXP_BASE = 100;
        private const int STAT_POINTS_PER_LEVEL = 3;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AddExp(int amount)
        {
            float multiplier = 1f + (Intelligence * 0.05f);
            int finalExp = Mathf.RoundToInt(amount * multiplier);
            CurrentExp += finalExp;

            while (CurrentExp >= RequiredExp)
            {
                CurrentExp -= RequiredExp;
                Level++;
                StatPoints += STAT_POINTS_PER_LEVEL;
                RequiredExp = EXP_BASE * Level;
                Debug.Log($"[LEVEL UP] Level {Level}! +{STAT_POINTS_PER_LEVEL} Stat Points");
            }

            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void AllocateStrength()
        {
            if (StatPoints <= 0) return;
            StatPoints--;
            Strength++;
            RecalculateDerivedStats();
            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void AllocateIntelligence()
        {
            if (StatPoints <= 0) return;
            StatPoints--;
            Intelligence++;
            RecalculateDerivedStats();
            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void AllocateVitality()
        {
            if (StatPoints <= 0) return;
            StatPoints--;
            Vitality++;
            RecalculateDerivedStats();

            var player = GameObject.FindGameObjectWithTag("Player");
            var health = player?.GetComponent<HealthSystem>();
            if (health != null) health.IncreaseMaxHealth(15);

            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void AllocateLuck()
        {
            if (StatPoints <= 0) return;
            StatPoints--;
            Luck++;
            RecalculateDerivedStats();
            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public int GetResetCost()
        {
            var data = SaveManager.Instance?.Data;
            if (data == null) return 0;
            return data.statResetCount == 0 ? 0 : 500 * Level;
        }

        public bool CanResetStats()
        {
            int cost = GetResetCost();
            if (cost == 0) return true;
            return GoldSystem.Instance != null && GoldSystem.Instance.Gold >= cost;
        }

        public void ResetStats()
        {
            if (!CanResetStats()) return;
            int cost = GetResetCost();
            if (cost > 0) GoldSystem.Instance.SpendGold(cost);

            StatPoints += Strength + Intelligence + Vitality + Luck;
            Strength = 0;
            Intelligence = 0;
            Vitality = 0;
            Luck = 0;

            RecalculateDerivedStats();

            if (SaveManager.Instance?.Data != null)
                SaveManager.Instance.Data.statResetCount++;

            SyncToSave();
            OnStatsChanged?.Invoke();
            Debug.Log("[STATS] Reset complete");
        }

        public void ApplyWeaponTierBonus(int tier)
        {
            BasicAttackDamage = BASE_ATTACK + Strength * 2 + tier * 10;
            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void ApplyArmorTierBonus(int tier)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            var health = player?.GetComponent<HealthSystem>();
            if (health != null) health.IncreaseMaxHealth(tier * 20);
            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void AddIntelligence(int amount)
        {
            Intelligence += amount;
            RecalculateDerivedStats();
            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void AddVitality(int amount)
        {
            Vitality += amount;
            var player = GameObject.FindGameObjectWithTag("Player");
            var health = player?.GetComponent<HealthSystem>();
            if (health != null) health.IncreaseMaxHealth(15 * amount);
            RecalculateDerivedStats();
            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void AddLuck(int amount)
        {
            Luck += amount;
            RareDropRate = Luck * 0.01f;
            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void LoadFromSave(SaveData data)
        {
            Level = data.level;
            CurrentExp = data.currentExp;
            StatPoints = data.statPoints;
            Strength = data.strength;
            Intelligence = data.intelligence;
            Vitality = data.vitality;
            Luck = data.luck;
            RequiredExp = EXP_BASE * Level;
            RecalculateDerivedStats();
        }

        private void RecalculateDerivedStats()
        {
            BasicAttackDamage = BASE_ATTACK + Strength * 2;
            ChargeDamage = BASE_CHARGE + Strength * 2;
            PotionHealAmount = BASE_POTION_HEAL + Vitality * 2;
            RareDropRate = Luck * 0.01f;
        }

        private void SyncToSave()
        {
            if (SaveManager.Instance?.Data == null) return;
            var data = SaveManager.Instance.Data;
            data.level = Level;
            data.currentExp = CurrentExp;
            data.statPoints = StatPoints;
            data.strength = Strength;
            data.intelligence = Intelligence;
            data.vitality = Vitality;
            data.luck = Luck;
            SaveManager.Instance.Save();
        }
    }
}
