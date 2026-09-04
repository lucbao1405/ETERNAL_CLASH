using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EternalClash.Game
{
    public class GameOverUIController : MonoBehaviour
    {
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject collectedItemsPanel;
        [SerializeField] private TMP_Text collectedItemsText;
        [SerializeField] private GameObject levelUpPanel;

        private void Awake()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            if (collectedItemsPanel != null)
                collectedItemsPanel.SetActive(false);

            if (levelUpPanel != null)
                levelUpPanel.SetActive(false);
        }

        public void ShowGameOver()
        {
            Time.timeScale = 1f;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            if (collectedItemsPanel != null)
                collectedItemsPanel.SetActive(true);

            if (collectedItemsText != null)
            {
                SaveData data = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
                collectedItemsText.text = data == null
                    ? "Collected items"
                    : $"Collected items\nWeapon Tier: {data.weaponTier}\nArmor Tier: {data.armorTier}";
            }

            if (levelUpPanel != null)
                levelUpPanel.SetActive(true);

            Debug.Log("[GAME OVER UI] Show");
        }
    }
}
