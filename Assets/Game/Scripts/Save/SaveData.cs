using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // Versioned DTO graph. Legacy root fields below remain serialized so existing
    // saves and callers continue to work during the staged migration.
    public int version = 0;
    public PlayerSaveData player = new PlayerSaveData();
    public CurrencySaveData currency = new CurrencySaveData();
    public InventorySaveData inventory = new InventorySaveData();
    public ProgressSaveData progress = new ProgressSaveData();
    public SettingsSaveData settings = new SettingsSaveData();

    // Economy
    public int gold = 0;
    public int gem = 0;
    public int oreMaterial = 0;
    public int leatherMaterial = 0;
    public int woodMaterial = 0;

    // Named inventory aliases used by shop-facing save data. Copper maps to the
    // existing ore currency and wolf hide maps to the existing leather currency.
    public int copperOre = 0;
    public int wolfHide = 0;
    public int steelOre = 0;

    // Progression
    public int level = 1;
    public int currentExp = 0;
    public int statPoints = 0;
    public int statResetCount = 0;

    // 4 Core Stats
    // Nhan vat moi bat dau voi 1 diem o moi chi so (PlayerStatSystem.BASE_STAT_VALUE).
    public int strength = 1;
    public int intelligence = 1;
    public int vitality = 1;
    public int luck = 1;

    // Equipment Tiers (0: Starter, 1: Leather, 2: Knight, 3: Royal)
    public int weaponTier = 0;
    public int armorTier = 0;
    public EquipmentSaveData equipment = new EquipmentSaveData();
    public List<EquipmentItemSaveData> equipmentInventory = new List<EquipmentItemSaveData>();
    public AbilitySaveData abilities = new AbilitySaveData();

    // Stage
    public int stageLevel = 1;

    // First-game tutorial. These remain simple root fields so they travel through
    // the existing SaveManager and PlayerPrefs payload without a second save path.
    public bool tutorialInitialized;
    public int tutorialStep;
    public string playerName = string.Empty;

    // Affinity & Bonds
    public int affinityPoints = 0;
    public List<string> unlockedLetters = new List<string>();

    // Ela (romance NPC at the first house, unlocked after clearing Stage 3)
    public int elaAffinity = 0;
    public List<string> elaTopicsDone = new List<string>();
    public List<string> elaGiftsReceived = new List<string>();

    // Monetization
    public bool hasRemovedAds = false;
    public bool isVipActive = false;
    public bool starterPackPurchased = false;

    // Kim cuong qua quang cao: gioi han so lan xem trong ngay (reset theo
    // ngay dia phau, tinh tu gemAdDate dang "yyyy-MM-dd").
    public int gemAdsWatchedToday = 0;
    public string gemAdDate = "";

    // Player Condition (Injured recovery system)
    public int playerCondition = 0; // 0 = Normal, 1 = Injured
    public int currentHp = 0;
    public int maxHp = 0;
    public long recoveryStartUnixTime = 0;
    public bool recoveryPopupPending;
    public float recoveryRatePerSecond = 5f;
    public int recoveryTargetPercent = 80;

    // Dang o giua tran. Bat khi vao Battle, tat khi thang / thua. Mo game ma co con
    // bat nghia la app bi tat ngang tran -> tinh la thua (tranh thoat app de ne thua).
    public bool battleInProgress = false;
}

[Serializable]
public class EquipmentSaveData
{
    public EquipmentItemSaveData weapon = new EquipmentItemSaveData();
    public EquipmentItemSaveData shield = new EquipmentItemSaveData();
    public EquipmentItemSaveData armor = new EquipmentItemSaveData();
}

[Serializable]
public class PlayerSaveData
{
    public int level = 1;
    public int currentExp;
    public int statPoints;
    public int statResetCount;
    public int strength;
    public int intelligence;
    public int vitality;
    public int luck;
    public int playerCondition;
    public int currentHp;
    public int maxHp;
    public long recoveryStartUnixTime;
    public float recoveryRatePerSecond = 5f;
    public int recoveryTargetPercent = 80;
}

[Serializable]
public class CurrencySaveData
{
    public int gold;
    public int gem;
    public int oreMaterial;
    public int leatherMaterial;
    public int woodMaterial;
    public int copperOre;
    public int wolfHide;
    public int steelOre;
}

[Serializable]
public class InventorySaveData
{
    public List<EquipmentItemSaveData> equipmentItems = new List<EquipmentItemSaveData>();
    public List<ItemStackSaveData> items = new List<ItemStackSaveData>();
}

[Serializable]
public class ItemStackSaveData
{
    public string itemId = string.Empty;
    public int amount;
}

[Serializable]
public class ProgressSaveData
{
    public int stageLevel = 1;
    public int affinityPoints;
    public List<string> unlockedLetters = new List<string>();
    public AbilitySaveData abilities = new AbilitySaveData();
}

[Serializable]
public class SettingsSaveData
{
    public bool hasRemovedAds;
    public bool isVipActive;
}

[Serializable]
public class EquipmentItemSaveData
{
    public string itemId = string.Empty;
    public int slot = 0;
    public int level = 0;
    public int upgradeLevel = 0;
    // Chi so CONG THEM cua mon trang bi, mac dinh 0 (khong lien quan chi so goc nhan vat).
    public int strength = 0;
    public int intelligence = 0;
    public int vitality = 0;
    public int luck = 0;
    public bool isNew;
}

[Serializable]
public class AbilitySaveData
{
    public int healingLevel;
    public int cooldownLevel;
    public int defenseLevel;
    public int healingExp;
    public int cooldownExp;
    public int defenseExp;
}
