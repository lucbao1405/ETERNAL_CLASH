using UnityEngine;

namespace EternalClash.UI
{
    public class DamagePopupManager : MonoBehaviour
    {
        public static DamagePopupManager Instance { get; private set; }

        [SerializeField] private GameObject damagePopupPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void ShowDamage(Vector3 position, int damage, bool isPlayerDamage = false)
        {
            SpawnPopup(position, "-" + damage, isPlayerDamage ? PopupType.PlayerDamage : PopupType.Damage);
        }

        public void ShowHeal(Vector3 position, int amount)
        {
            SpawnPopup(position, "+" + amount, PopupType.Heal);
        }

        public void ShowBlock(Vector3 position, int amount)
        {
            SpawnPopup(position, "BLOCK " + amount, PopupType.Block);
        }

        private void SpawnPopup(Vector3 position, string value, PopupType type)
        {
            if (damagePopupPrefab == null)
                return;

            GameObject popup = Instantiate(
                damagePopupPrefab,
                position + Vector3.up,
                Quaternion.identity);

            DamagePopup component = popup.GetComponent<DamagePopup>();

            if (component != null)
                component.SetupText(value, type);
        }

        public enum PopupType
        {
            Damage,
            PlayerDamage,
            Heal,
            Block
        }
    }
}
