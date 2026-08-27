using UnityEngine;
using UnityEngine.UI;
using EternalClash.Character;

namespace EternalClash.Enemy
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private HealthSystem healthSystem;
        [SerializeField] private Image fillImage;

        private void Awake()
        {
            if (healthSystem == null)
                healthSystem = GetComponentInParent<HealthSystem>();
        }

        private void Start()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += UpdateHealthBar;
                UpdateHealthBar(healthSystem.CurrentHealth, healthSystem.MaxHealth);
            }
        }

        private void OnDestroy()
        {
            if (healthSystem != null)
                healthSystem.OnHealthChanged -= UpdateHealthBar;
        }

        private void UpdateHealthBar(int current, int max)
        {
            if (fillImage == null || max <= 0)
                return;

            fillImage.fillAmount = (float)current / max;
        }
    }
}
