using System.Collections.Generic;
using EternalClash.Data;

namespace EternalClash.Core.Save
{
    public static class SaveMigrationManager
    {
        public static SaveData Migrate(SaveData oldData, out bool changed)
        {
            SaveData data = oldData ?? new SaveData();
            changed = oldData == null;
            if (data.version < SaveVersion.CurrentVersion)
            {
                if (data.version < 1)
                    MigrateToVersion1(data);
                if (data.version < 2)
                    MigrateToVersion2(data);
                if (data.version < 3)
                    MigrateToVersion3(data);
                data.version = SaveVersion.CurrentVersion;
                changed = true;
            }

            EnsureDtoContainers(data);
            changed |= RemoveCurrencyInventoryEntries(data);
            return data;
        }

        public static SaveData Migrate(SaveData oldData)
        {
            return Migrate(oldData, out _);
        }

        public static void SynchronizeDtosFromLegacy(SaveData data)
        {
            EnsureDtoContainers(data);
            PlayerSaveData player = data.player;
            player.level = data.level; player.currentExp = data.currentExp; player.statPoints = data.statPoints; player.statResetCount = data.statResetCount;
            player.strength = data.strength; player.intelligence = data.intelligence; player.vitality = data.vitality; player.luck = data.luck;
            player.playerCondition = data.playerCondition; player.currentHp = data.currentHp; player.maxHp = data.maxHp;
            player.recoveryStartUnixTime = data.recoveryStartUnixTime; player.recoveryRatePerSecond = data.recoveryRatePerSecond; player.recoveryTargetPercent = data.recoveryTargetPercent;
            CurrencySaveData currency = data.currency;
            currency.gold = data.gold; currency.gem = data.gem; currency.oreMaterial = data.oreMaterial; currency.leatherMaterial = data.leatherMaterial;
            currency.woodMaterial = data.woodMaterial; currency.copperOre = data.copperOre; currency.wolfHide = data.wolfHide; currency.steelOre = data.steelOre;
            data.inventory.equipmentItems = data.equipmentInventory ?? new List<EquipmentItemSaveData>();
            data.progress.stageLevel = data.stageLevel; data.progress.affinityPoints = data.affinityPoints;
            data.progress.unlockedLetters = data.unlockedLetters ?? new List<string>(); data.progress.abilities = data.abilities ?? new AbilitySaveData();
            data.settings.hasRemovedAds = data.hasRemovedAds; data.settings.isVipActive = data.isVipActive;
            data.version = SaveVersion.CurrentVersion;
        }

        private static void MigrateToVersion1(SaveData data)
        {
            SynchronizeDtosFromLegacy(data);
        }

        private static void MigrateToVersion2(SaveData data)
        {
            // Version 1 saved equipped bonuses into the player base stats. Remove the
            // current equipment snapshot once so version 2 can calculate it separately.
            EquipmentSaveData equipment = data.equipment;
            if (equipment == null)
                return;

            RemoveEquipmentBonuses(data, equipment.weapon);
            RemoveEquipmentBonuses(data, equipment.armor);
            RemoveEquipmentBonuses(data, equipment.shield);
        }

        private static void RemoveEquipmentBonuses(SaveData data, EquipmentItemSaveData item)
        {
            if (item == null || (string.IsNullOrEmpty(item.itemId) && item.level <= 0))
                return;

            data.strength -= item.strength;
            data.intelligence -= item.intelligence;
            data.vitality -= item.vitality;
            data.luck -= item.luck;
        }

        private static void EnsureDtoContainers(SaveData data)
        {
            data.player ??= new PlayerSaveData(); data.currency ??= new CurrencySaveData(); data.inventory ??= new InventorySaveData();
            data.inventory.items ??= new List<ItemStackSaveData>();
            data.equipment ??= new EquipmentSaveData(); data.progress ??= new ProgressSaveData(); data.settings ??= new SettingsSaveData();
            data.equipmentInventory ??= new List<EquipmentItemSaveData>(); data.unlockedLetters ??= new List<string>(); data.abilities ??= new AbilitySaveData();
        }

        private static void MigrateToVersion3(SaveData data)
        {
            // Preserve all equipment and upgrade levels while replacing only the old,
            // overtuned starting-iron snapshot bonuses with their GDD progression.
            RebalanceIronEquipment(data.equipment?.weapon);
            RebalanceIronEquipment(data.equipment?.armor);
            RebalanceIronEquipment(data.equipment?.shield);

            foreach (EquipmentItemSaveData item in data.equipmentInventory ?? new List<EquipmentItemSaveData>())
                RebalanceIronEquipment(item);
        }

        private static void RebalanceIronEquipment(EquipmentItemSaveData item)
        {
            if (item == null || !NewGameEquipmentDefaults.TryGetIronProgressionBonuses(
                    item.itemId, item.upgradeLevel, out int strength, out int vitality))
                return;

            if (string.Equals(item.itemId, NewGameEquipmentDefaults.IronArmorId,
                    System.StringComparison.OrdinalIgnoreCase))
                item.itemId = NewGameEquipmentDefaults.IronArmorId;

            item.strength = strength;
            item.intelligence = 0;
            item.vitality = vitality;
            item.luck = 0;
        }

        private static bool RemoveCurrencyInventoryEntries(SaveData data)
        {
            List<ItemStackSaveData> items = data.inventory.items;
            int initialCount = items.Count;
            items.RemoveAll(item => item == null || item.amount <= 0 || IsCurrencyItem(item.itemId));
            return items.Count != initialCount;
        }

        private static bool IsCurrencyItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return true;

            ItemData item = ItemCatalog.Find(itemId);
            if (item != null && string.Equals(item.itemType, "Currency", System.StringComparison.OrdinalIgnoreCase))
                return true;

            string normalized = itemId.Trim().ToLowerInvariant();
            return normalized == "coin" || normalized == "gold" || normalized == "diamond" ||
                normalized == "diamon" || normalized == "gem" || normalized == "gems" ||
                normalized.Contains("currency");
        }
    }
}
