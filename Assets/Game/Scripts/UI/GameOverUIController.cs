using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.Game
{
    public class GameOverUIController : MonoBehaviour
    {
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button retryButton;

        private void Awake()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            if (retryButton != null)
                retryButton.onClick.AddListener(RestartGame);
        }

        public void ShowGameOver()
        {
            Time.timeScale = 0f;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            Debug.Log("[GAME OVER UI] Show");
        }

        private void RestartGame()
        {
            Time.timeScale = 1f;
            Debug.Log("[GAME] Restart requested");

            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
    }
}
