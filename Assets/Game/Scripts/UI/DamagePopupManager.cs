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

        /// <summary>
        /// Sat thuong chi mang - hien khac mau va to hon de nguoi choi thay ro
        /// diem LUCK dang co tac dung.
        /// </summary>
        public void ShowCriticalDamage(Vector3 position, int damage)
        {
            SpawnPopup(position, "-" + damage + "!", PopupType.Critical);
        }

        public void ShowHeal(Vector3 position, int amount)
        {
            SpawnPopup(position, "+" + amount, PopupType.Heal);
        }

        public void ShowBlock(Vector3 position, int amount)
        {
            SpawnPopup(position, "BLOCK " + amount, PopupType.Block);
        }

        /// <summary>Bao so combo (Postknight style "x3"). Vi tri = diem quai bi danh.</summary>
        public void ShowCombo(Vector3 position, int combo)
        {
            // Cao hon so damage mot chut de khong che nhau.
            SpawnPopup(position + Vector3.up * 0.8f, "x" + combo, PopupType.Combo);
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
            Block,
            // Them vao cuoi enum de khong lam lech gia tri da serialize san.
            Critical,
            Combo
        }
    }
}
