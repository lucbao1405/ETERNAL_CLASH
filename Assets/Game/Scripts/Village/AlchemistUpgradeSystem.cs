using EternalClash.Core.Save;
using UnityEngine;

namespace EternalClash.Village
{
    /// <summary>Persistent material-only upgrade state for Witch abilities.</summary>
    public sealed class AlchemistUpgradeSystem : MonoBehaviour
    {
        public static AlchemistUpgradeSystem Instance { get; private set; }
        public int HealingLevel { get; private set; } = 1;
        public int CooldownLevel { get; private set; } = 1;

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

        public MaterialType GetRequiredMaterial(WitchAbility ability)
        {
            return ability == WitchAbility.Healing ? MaterialType.Wood : MaterialType.Ore;
        }

        public int GetUpgradeCost(WitchAbility ability)
        {
            int level = ability == WitchAbility.Healing ? HealingLevel : CooldownLevel;
            int baseCost = ability == WitchAbility.Healing ? 3 : 2;
            return Mathf.CeilToInt(baseCost * Mathf.Pow(1.25f, Mathf.Max(0, level - 1)));
        }

        public bool CanUpgradeHealing() => CanUpgrade(WitchAbility.Healing);
        public bool CanUpgradeCooldown() => CanUpgrade(WitchAbility.Cooldown);
        public bool TryUpgradeHealing() => TryUpgrade(WitchAbility.Healing);
        public bool TryUpgradeCooldown() => TryUpgrade(WitchAbility.Cooldown);

        public bool CanUpgrade(WitchAbility ability)
        {
            GoldSystem resources = GoldSystem.Instance;
            return resources != null && resources.GetMaterial(GetRequiredMaterial(ability)) >= GetUpgradeCost(ability);
        }

        public bool TryUpgrade(WitchAbility ability)
        {
            GoldSystem resources = GoldSystem.Instance;
            MaterialType material = GetRequiredMaterial(ability);
            int cost = GetUpgradeCost(ability);
            if (resources == null || resources.GetMaterial(material) < cost)
                return false;

            // The pre-check keeps this material transaction atomic from the Witch flow's perspective.
            if (!resources.SpendMaterial(material, cost))
                return false;

            if (ability == WitchAbility.Healing)
                HealingLevel++;
            else
                CooldownLevel++;

            SaveData data = SaveManager.Instance?.Data;
            if (data != null)
            {
                data.abilities ??= new AbilitySaveData();
                data.abilities.healingLevel = HealingLevel;
                data.abilities.cooldownLevel = CooldownLevel;
                SaveCoordinator.RequestSave();
            }

            Debug.Log($"[WITCH] {ability} upgraded using {cost} {material}.");
            return true;
        }

        public void LoadFromSave(SaveData data)
        {
            HealingLevel = Mathf.Max(1, data?.abilities?.healingLevel ?? 1);
            CooldownLevel = Mathf.Max(1, data?.abilities?.cooldownLevel ?? 1);
        }
    }

    public enum WitchAbility
    {
        Healing,
        Cooldown
    }
}
