using UnityEngine;
using UnityEngine.UI;
using EternalClash.Character;

namespace EternalClash.UI
{
    public class PlayerHealthUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        private HealthSystem healthSystem;

        private void Start()
        {
            healthSystem = FindObjectOfType<HealthSystem>();

            if (healthSystem == null)
            {
                Debug.LogError("Khong tim thay Player HealthSystem");
                return;
            }

            healthSystem.OnHealthChanged += UpdateHealth;
            UpdateHealth(healthSystem.CurrentHealth, healthSystem.MaxHealth);
        }

        private void UpdateHealth(int current, int max)
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = (float)current / max;
            }
        }

        private void OnDestroy()
        {
            if (healthSystem != null)
                healthSystem.OnHealthChanged -= UpdateHealth;
        }
    }
}
