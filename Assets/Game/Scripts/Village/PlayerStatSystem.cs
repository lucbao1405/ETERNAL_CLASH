using UnityEngine;
using EternalClash.Character;
using EternalClash.Core.Save;

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
        // Public stats are always the calculated values consumed by UI and gameplay.
        public int Strength => baseStrength + equipmentStrength;
        public int Intelligence => baseIntelligence + equipmentIntelligence;
        public int Vitality => baseVitality + equipmentVitality;
        public int Luck => baseLuck + equipmentLuck;
        public float RareDropRate { get; private set; }

        /// <summary>Ti le chi mang hien tai, 0..1. Tang theo LUCK.</summary>
        public float CritChance { get; private set; }

        /// <summary>He so nhan sat thuong khi chi mang.</summary>
        public float CritMultiplier => CRIT_MULTIPLIER;

        public event System.Action OnStatsChanged;

        private const int BASE_ATTACK = 5;
        private const int BASE_CHARGE = 30;
        private const int BASE_POTION_HEAL = 30;
        private const int EXP_BASE = 20;

        /// <summary>
        /// He so tang yeu cau EXP moi cap. Moc sau = lam tron(moc truoc * he so),
        /// nen chuoi la 100 - 150 - 225 - 338 - 507 - ...
        /// </summary>
        private const float EXP_GROWTH = 1.5f;

        private const int STAT_POINTS_PER_LEVEL = 3;

        /// <summary>So diem tieu cho moi lan nang mot chi so.</summary>
        private const int STAT_POINT_COST = 1;

        /// <summary>Max HP cong them cho moi diem VIT.</summary>
        private const int VIT_MAX_HP = 15;

        /// <summary>
        /// Tong Max HP cong them tu VIT. HealthSystem doc gia tri nay luc Awake nen
        /// bonus duoc ap lai dung moi lan Player spawn, thay vi bi mat theo prefab.
        /// </summary>
        public int BonusMaxHealth => Vitality * VIT_MAX_HP;

        /// <summary>
        /// Max HP goc cua Player khi chua cong VIT. Dung lam gia tri du phong cho
        /// scene Town - o do khong co Player nao de hoi, nhung van can hien thanh mau.
        /// Phai khop voi HealthSystem tren Player prefab; neu lech thi lan dau vao
        /// Battle se tu duoc hieu chinh qua RegisterPlayerBaseMaxHealth().
        /// </summary>
        private const int FALLBACK_PLAYER_MAX_HEALTH = 100;

        private int registeredBaseMaxHealth = -1;
        private int baseStrength;
        private int baseIntelligence;
        private int baseVitality;
        private int baseLuck;
        private int equipmentStrength;
        private int equipmentIntelligence;
        private int equipmentVitality;
        private int equipmentLuck;

        /// <summary>Max HP goc dang dung (da hieu chinh neu tung gap Player that).</summary>
        public int BasePlayerMaxHealth =>
            registeredBaseMaxHealth > 0 ? registeredBaseMaxHealth : FALLBACK_PLAYER_MAX_HEALTH;

        /// <summary>Tong Max HP = mau goc + bonus tu VIT.</summary>
        public int TotalMaxHealth => BasePlayerMaxHealth + BonusMaxHealth;

        /// <summary>
        /// HealthSystem cua Player bao lai mau goc that su tren prefab khi no khoi tao.
        /// Nho vay UI o Town khong phai doan, va neu ai do doi maxHealth tren prefab
        /// thi con so hien ra van dung sau lan vao Battle dau tien.
        /// </summary>
        public void RegisterPlayerBaseMaxHealth(int baseMax)
        {
            if (baseMax <= 0 || registeredBaseMaxHealth == baseMax)
                return;

            registeredBaseMaxHealth = baseMax;
            OnStatsChanged?.Invoke();
        }

        /// <summary>
        /// Chi phi cong 1 diem chi so. UI doc gia tri nay de hien so tren nut,
        /// nen doi hang so o day la nut tu cap nhat theo.
        /// </summary>
        public int StatPointCost => STAT_POINT_COST;

        /// <summary>Con du diem de nang cap khong.</summary>
        public bool CanAllocate => StatPoints >= STAT_POINT_COST;

        // Chi mang: nen 5%, moi diem LUCK them 1%, tran 50%.
        private const float BASE_CRIT_CHANCE = 0.05f;
        // GDD: moi diem LUCK cho +0.5% chi mang (va +1% ti le rot do hiem).
        private const float CRIT_CHANCE_PER_LUCK = 0.005f;
        private const float MAX_CRIT_CHANCE = 0.5f;
        private const float CRIT_MULTIPLIER = 1.5f;

        /// <summary>
        /// EXP can de di tu <paramref name="level"/> len cap ke tiep.
        /// Phai cong don tung buoc chu khong dung luy thua: moi moc duoc lam tron
        /// truoc khi nhan tiep, nen 225 -> 338 (khong phai 337) va 338 -> 507.
        /// </summary>
        private static int CalculateRequiredExp(int level)
        {
            int required = EXP_BASE;

            for (int i = 1; i < level; i++)
                required = Mathf.RoundToInt(required * EXP_GROWTH);

            return required;
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Nguoi choi moi bat dau o cap 1. Neu co save, LoadFromSave() ghi de ngay
            // sau day. Khong khoi tao thi Level = 0 va RequiredExp = 0, khien lan
            // AddExp dau tien lap tuc len cap mien phi.
            if (Level < 1)
            {
                Level = 1;
                RequiredExp = CalculateRequiredExp(Level);
            }
        }

        /// <summary>
        /// Tong EXP nhan duoc ke tu dau tran hien tai (da tinh bonus INT).
        /// Popup ket qua doc gia tri nay de bao dung so kiem duoc trong tran,
        /// thay vi cong them mot khoan thuong rieng.
        /// </summary>
        public int SessionExpEarned { get; private set; }

        /// <summary>Dat lai bo dem dau tran. StageManager goi khi bat dau man.</summary>
        public void ResetSessionCounters()
        {
            SessionExpEarned = 0;
        }

        public void AddExp(int amount)
        {
            float multiplier = 1f + (Intelligence * 0.05f);
            int finalExp = Mathf.RoundToInt(amount * multiplier);
            CurrentExp += finalExp;
            SessionExpEarned += finalExp;

            bool leveledUp = false;
            while (CurrentExp >= RequiredExp)
            {
                CurrentExp -= RequiredExp;
                Level++;
                StatPoints += STAT_POINTS_PER_LEVEL;
                RequiredExp = CalculateRequiredExp(Level);
                leveledUp = true;
                Debug.Log($"[LEVEL UP] Level {Level}! +{STAT_POINTS_PER_LEVEL} Stat Points (can {RequiredExp} EXP cho cap sau)");
            }

            if (leveledUp)
                EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.PlayerLevelUp);

            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void AllocateStrength()
        {
            if (!CanAllocate) return;
            StatPoints -= STAT_POINT_COST;
            baseStrength++;
            RecalculateDerivedStats();
            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void AllocateIntelligence()
        {
            if (!CanAllocate) return;
            StatPoints -= STAT_POINT_COST;
            baseIntelligence++;
            RecalculateDerivedStats();
            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void AllocateVitality()
        {
            if (!CanAllocate) return;
            StatPoints -= STAT_POINT_COST;
            baseVitality++;
            RecalculateDerivedStats();

            // Neu dang o Battle va Player dang song thi cap nhat ngay. Con o Town
            // thi khong co Player nao ca - bonus van duoc luu qua Vitality va se
            // duoc HealthSystem doc lai luc Player spawn o tran sau.
            ApplyMaxHealthToLivePlayer();

            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        /// <summary>
        /// Cap nhat Max HP cho Player dang ton tai trong scene (neu co).
        /// Truoc day AllocateVitality() goi thang IncreaseMaxHealth(15), nhung:
        ///  - Trong Town khong co Player nao nen lenh do khong bao gio chay.
        ///  - HealthSystem.Awake() dat lai maxHealth tu prefab moi lan spawn nen
        ///    bonus cong don kieu do khong song qua duoc mot lan doi scene.
        /// </summary>
        public void ApplyMaxHealthToLivePlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            var health = player != null ? player.GetComponent<HealthSystem>() : null;

            if (health != null)
                health.ApplyBonusMaxHealth(BonusMaxHealth);
        }

        public void AllocateLuck()
        {
            if (!CanAllocate) return;
            StatPoints -= STAT_POINT_COST;
            baseLuck++;
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

            StatPoints += baseStrength + baseIntelligence + baseVitality + baseLuck;
            baseStrength = 0;
            baseIntelligence = 0;
            baseVitality = 0;
            baseLuck = 0;

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
            baseIntelligence += amount;
            RecalculateDerivedStats();
            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void AddStrength(int amount)
        {
            baseStrength += amount;
            RecalculateDerivedStats();
            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void AddVitality(int amount)
        {
            baseVitality += amount;
            RecalculateDerivedStats();
            ApplyMaxHealthToLivePlayer();
            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void AddLuck(int amount)
        {
            baseLuck += amount;
            RecalculateDerivedStats();
            SyncToSave();
            OnStatsChanged?.Invoke();
        }

        public void LoadFromSave(SaveData data)
        {
            // Save cu co the co level = 0; ep toi thieu 1 de vong lap trong AddExp
            // khong lap tuc len cap mien phi (RequiredExp = 0 khi Level = 0).
            Level = Mathf.Max(1, data.level);
            CurrentExp = data.currentExp;
            StatPoints = data.statPoints;
            baseStrength = data.strength;
            baseIntelligence = data.intelligence;
            baseVitality = data.vitality;
            baseLuck = data.luck;
            RequiredExp = CalculateRequiredExp(Level);
            RecalculateDerivedStats();
        }

        private void RecalculateDerivedStats()
        {
            BasicAttackDamage = BASE_ATTACK + Strength * 2;
            ChargeDamage = BASE_CHARGE + Strength * 2;
            PotionHealAmount = BASE_POTION_HEAL + Vitality * 2;
            RareDropRate = Luck * 0.01f;

            // LUCK tang ti le chi mang (GDD 3.5). Chan tran o 50% de khong bien
            // moi don thanh chi mang khi cong nhieu diem LUCK.
            CritChance = Mathf.Min(BASE_CRIT_CHANCE + Luck * CRIT_CHANCE_PER_LUCK, MAX_CRIT_CHANCE);
        }

        /// <summary>
        /// Replaces, rather than adds, the currently equipped item contribution.
        /// This keeps repeated equip and save/load restoration idempotent.
        /// </summary>
        public void SetEquipmentBonuses(int strength, int intelligence, int vitality, int luck)
        {
            equipmentStrength = strength;
            equipmentIntelligence = intelligence;
            equipmentVitality = vitality;
            equipmentLuck = luck;
            RecalculateDerivedStats();
            ApplyMaxHealthToLivePlayer();
            OnStatsChanged?.Invoke();
        }

        /// <summary>
        /// Quay xem don danh nay co chi mang khong. Goi tu CombatDamageResolver.
        /// </summary>
        public bool RollCritical()
        {
            return Random.value < CritChance;
        }

        /// <summary>
        /// Nhan sat thuong voi he so chi mang neu quay trung.
        /// Tra ve sat thuong cuoi cung va bao co chi mang hay khong.
        /// </summary>
        public int ApplyCritical(int damage, out bool isCritical)
        {
            isCritical = RollCritical();
            return isCritical ? Mathf.RoundToInt(damage * CritMultiplier) : damage;
        }

        private void SyncToSave()
        {
            if (SaveManager.Instance?.Data == null) return;
            var data = SaveManager.Instance.Data;
            data.level = Level;
            data.currentExp = CurrentExp;
            data.statPoints = StatPoints;
            data.strength = baseStrength;
            data.intelligence = baseIntelligence;
            data.vitality = baseVitality;
            data.luck = baseLuck;
            SaveCoordinator.RequestSave();
        }
    }
}
