using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // Economy
    public int gold = 0;
    public int gem = 0;
    public int oreMaterial = 0;
    public int leatherMaterial = 0;
    public int woodMaterial = 0;

    // Progression
    public int level = 1;
    public int currentExp = 0;
    public int statPoints = 0;
    public int statResetCount = 0;

    // 4 Core Stats
    public int strength = 0;
    public int intelligence = 0;
    public int vitality = 0;
    public int luck = 0;

    // Equipment Tiers (0: Starter, 1: Leather, 2: Knight, 3: Royal)
    public int weaponTier = 0;
    public int armorTier = 0;

    // Stage
    public int stageLevel = 1;

    // Affinity & Bonds
    public int affinityPoints = 0;
    public List<string> unlockedLetters = new List<string>();

    // Monetization
    public bool hasRemovedAds = false;
    public bool isVipActive = false;

    // Player Condition (Injured recovery system)
    public int playerCondition = 0; // 0 = Normal, 1 = Injured
    public int currentHp = 0;
    public int maxHp = 0;
    public long recoveryStartUnixTime = 0;
    public float recoveryRatePerSecond = 5f;
    public int recoveryTargetPercent = 80;
}
