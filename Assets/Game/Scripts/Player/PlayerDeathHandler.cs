using UnityEngine;
using EternalClash.Combat;
using EternalClash.Game;

namespace EternalClash.Player
{
    /// <summary>
    /// Separates death behaviour from DamageReceiver.
    /// Used later for animation, respawn and game over flow.
    /// </summary>
    public class PlayerDeathHandler : MonoBehaviour
    {
        private DamageReceiver damageReceiver;
        private bool handled;

        private void Awake()
        {
            damageReceiver = GetComponent<DamageReceiver>();
        }

        private void Update()
        {
            if (handled || damageReceiver == null)
                return;

            if (damageReceiver.IsDead())
            {
                HandleDeath();
            }
        }

        private void HandleDeath()
        {
            handled = true;
            Debug.Log("Player death handled");

            GameOverUIController gameOverUI = FindObjectOfType<GameOverUIController>();
            if (gameOverUI != null)
                gameOverUI.ShowGameOver();
        }
    }
}
