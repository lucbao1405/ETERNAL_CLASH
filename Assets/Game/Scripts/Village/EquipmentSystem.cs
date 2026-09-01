using UnityEngine;

namespace EternalClash.Village
{
    public class EquipmentSystem : MonoBehaviour
    {
        public static EquipmentSystem Instance { get; private set; }

        [Header("Weapon Sprites by Tier")]
        [SerializeField] private Sprite[] weaponSprites;

        [Header("Armor Sprites by Tier")]
        [SerializeField] private Sprite[] armorSprites;

        [Header("Player Renderers")]
        [SerializeField] private SpriteRenderer playerWeaponRenderer;
        [SerializeField] private SpriteRenderer playerArmorRenderer;

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

        public void ApplyWeaponTier(int tier)
        {
            if (weaponSprites == null || playerWeaponRenderer == null) return;
            if (tier >= 0 && tier < weaponSprites.Length)
            {
                playerWeaponRenderer.sprite = weaponSprites[tier];
                Debug.Log($"[EQUIPMENT] Weapon visual -> Tier {tier}");
            }
        }

        public void ApplyArmorTier(int tier)
        {
            if (armorSprites == null || playerArmorRenderer == null) return;
            if (tier >= 0 && tier < armorSprites.Length)
            {
                playerArmorRenderer.sprite = armorSprites[tier];
                Debug.Log($"[EQUIPMENT] Armor visual -> Tier {tier}");
            }
        }

        public void RefreshFromSave()
        {
            var data = SaveManager.Instance?.Data;
            if (data == null) return;
            ApplyWeaponTier(data.weaponTier);
            ApplyArmorTier(data.armorTier);
        }
    }
}
