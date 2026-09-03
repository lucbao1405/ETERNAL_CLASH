using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.Enemy
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private EnemyHealthSystem healthSystem;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image delayedFillImage;
        [SerializeField] private float delayBeforeDeplete = 0.4f;
        [SerializeField] private float depleteSpeed = 1.5f;

        private float targetFillAmount = 1f;
        private float delayTimer;

        private void Awake()
        {
            if (healthSystem == null)
                healthSystem = GetComponentInParent<EnemyHealthSystem>();
        }

        private void Start()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += UpdateHealthBar;
                UpdateHealthBar(healthSystem.CurrentHealth, healthSystem.MaxHealth);
            }

            if (fillImage != null)
                fillImage.fillAmount = targetFillAmount;

            if (delayedFillImage != null)
                delayedFillImage.fillAmount = targetFillAmount;
        }

        private void OnDestroy()
        {
            if (healthSystem != null)
                healthSystem.OnHealthChanged -= UpdateHealthBar;
        }

        private void Update()
        {
            if (delayedFillImage == null)
                return;

            if (delayedFillImage.fillAmount <= targetFillAmount)
                return;

            if (delayTimer > 0f)
            {
                delayTimer -= Time.deltaTime;
                return;
            }

            delayedFillImage.fillAmount = Mathf.MoveTowards(delayedFillImage.fillAmount, targetFillAmount, depleteSpeed * Time.deltaTime);
        }

        private void UpdateHealthBar(int current, int max)
        {
            if (max <= 0)
                return;

            targetFillAmount = (float)current / max;

            if (fillImage != null)
                fillImage.fillAmount = targetFillAmount;

            if (delayedFillImage != null && delayedFillImage.fillAmount < targetFillAmount)
                delayedFillImage.fillAmount = targetFillAmount;

            delayTimer = delayBeforeDeplete;
        }
    }
}
