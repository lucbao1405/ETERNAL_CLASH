using UnityEngine;

namespace EternalClash.Combat
{
    public class DamagePopup : MonoBehaviour
    {
        public static DamagePopup Instance;

        public GameObject popupPrefab;

        private void Awake()
        {
            Instance = this;
        }

        public void Show(Vector3 position, int damage)
        {
            Debug.Log("[DAMAGE POPUP] -" + damage);

            if (popupPrefab == null)
                return;

            GameObject popup = Instantiate(popupPrefab, position, Quaternion.identity);

            var text = popup.GetComponent<TMPro.TMP_Text>();
            if (text != null)
                text.text = "-" + damage;
        }
    }
}
