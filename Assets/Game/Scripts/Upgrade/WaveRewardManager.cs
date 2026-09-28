using UnityEngine;
using System;

namespace EternalClash.Upgrade
{
    public enum UpgradeType
    {
        Damage,
        MaxHealth,
        CooldownReduction,
        MoveSpeed
    }

    public class WaveRewardManager : MonoBehaviour
    {
        public static WaveRewardManager Instance;

        public event Action OnRewardAvailable;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void OpenReward()
        {
            Debug.Log("[UPGRADE] Wave reward available");
            OnRewardAvailable?.Invoke();
        }

        public void ApplyUpgrade(UpgradeType type)
        {
            switch(type)
            {
                case UpgradeType.Damage:
                    Debug.Log("[UPGRADE] Damage +20%");
                    break;
                case UpgradeType.MaxHealth:
                    Debug.Log("[UPGRADE] Max HP +30");
                    break;
                case UpgradeType.CooldownReduction:
                    Debug.Log("[UPGRADE] Cooldown -15%");
                    break;
                case UpgradeType.MoveSpeed:
                    Debug.Log("[UPGRADE] Move Speed +10%");
                    break;
            }
        }
    }
}
