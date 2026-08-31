using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalClash.Character;

namespace EternalClash.UI
{
    public class PlayerHealthUI : MonoBehaviour
    {
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text hpText;
        private HealthSystem healthSystem;

        private void Awake()
        {
            if (hpSlider == null)
                hpSlider = GetComponent<Slider>() ?? GetComponentInChildren<Slider>();

            if (hpText == null)
                hpText = GetComponentInChildren<TMP_Text>();

            if (fillImage == null && hpSlider == null)
                fillImage = GetComponent<Image>() ?? GetComponentInChildren<Image>();
        }

        private void Start()
        {
            FindPlayerHealth();
        }

        private void FindPlayerHealth()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                healthSystem = player.GetComponent<HealthSystem>();
                if (healthSystem != null)
                {
                    healthSystem.OnHealthChanged -= UpdateHealth;
                    healthSystem.OnHealthChanged += UpdateHealth;
                    UpdateHealth(healthSystem.CurrentHealth, healthSystem.MaxHealth);
                }
            }
        }

        private void Update()
        {
            if (healthSystem == null)
            {
                FindPlayerHealth();
            }
        }

        public void UpdateHealth(int current, int max)
        {
            if (hpSlider != null)
            {
                hpSlider.maxValue = Mathf.Max(max, 1);
                hpSlider.value = current;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = (float)current / Mathf.Max(max, 1);
            }

            if (hpText != null)
            {
                hpText.text = $"{current}/{max}";
            }
        }

        private void OnDestroy()
        {
            if (healthSystem != null)
                healthSystem.OnHealthChanged -= UpdateHealth;
        }
    }
}
