using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.Enemy
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private EnemyBase enemy;
        [SerializeField] private Image fillImage;

        private void Awake()
        {
            if (enemy == null)
                enemy = GetComponentInParent<EnemyBase>();
        }

        private void Start()
        {
            if (enemy != null)
            {
                enemy.OnHealthChanged += UpdateHealthBar;
                UpdateHealthBar(enemy.GetCurrentHP(), enemy.maxHP);
            }
        }

        private void OnDestroy()
        {
            if (enemy != null)
                enemy.OnHealthChanged -= UpdateHealthBar;
        }

        private void UpdateHealthBar(int current, int max)
        {
            if (fillImage == null || max <= 0)
                return;

            fillImage.fillAmount = (float)current / max;
        }
    }
}
