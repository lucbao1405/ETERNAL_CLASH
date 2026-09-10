using System.Collections.Generic;

namespace EternalClash.Core.Save
{
    public static class SaveMigrationManager
    {
        public static SaveData Migrate(SaveData oldData)
        {
            SaveData data = oldData ?? new SaveData();
            if (data.version < SaveVersion.CurrentVersion)
            {
                if (data.version < 1)
                    MigrateToVersion1(data);
                if (data.version < 2)
                    MigrateToVersion2(data);
                data.version = SaveVersion.CurrentVersion;
            }

            EnsureDtoContainers(data);
            return data;
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
    }
}
