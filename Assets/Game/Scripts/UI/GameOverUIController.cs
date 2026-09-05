using UnityEngine;
using TMPro;

namespace EternalClash.Game
{
    public class GameOverUIController : MonoBehaviour
    {
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject collectedItemsPanel;
        [SerializeField] private TMP_Text collectedItemsText;

        private void Awake()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            if (collectedItemsPanel != null)
                collectedItemsPanel.SetActive(false);
        }

        public void ShowCollectedItems(int weaponTier, int armorTier)
        {
            if (collectedItemsPanel != null)
                collectedItemsPanel.SetActive(true);

            if (collectedItemsText != null)
                collectedItemsText.text = $"Collected items\nWeapon Tier: {weaponTier}\nArmor Tier: {armorTier}";
        }
    }
}
