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

        // Thanh mau giong Town: khung + ruot do (HpBarSprites). Ruot chay mu toi gia tri
        // moi; mat mau chay nhanh de phan hoi don danh ro rang, hoi mau chay cham hon.
        private const float FillSpeedDown = 4f;
        private const float FillSpeedUp = 2f;
        private Image splitFill;
        private float fillTarget = 1f;
        private bool fillInitialized;

        private void Awake()
        {
            if (hpSlider == null)
                hpSlider = GetComponent<Slider>() ?? GetComponentInChildren<Slider>();

            if (hpText == null)
                hpText = GetComponentInChildren<TMP_Text>();

            if (fillImage == null && hpSlider == null)
                fillImage = GetComponent<Image>() ?? GetComponentInChildren<Image>();

            SetupSplitBar();
        }

        /// <summary>
        /// Bien anh nen cua HealthBar thanh khung va tao ruot do ben trong. Thanh cu
        /// (Slider keo gian anh "UI blood" to do) duoc tat di. Thieu anh thi giu thanh cu.
        /// </summary>
        private void SetupSplitBar()
        {
            Image background = GetComponent<Image>();
            Image fill = HpBarSprites.ApplyTo(background);
            if (fill == null)
                return;

            splitFill = fill;

            if (hpSlider != null)
            {
                if (hpSlider.fillRect != null)
                {
                    Transform oldFillArea = hpSlider.fillRect.parent != transform
                        ? hpSlider.fillRect.parent
                        : hpSlider.fillRect;
                    oldFillArea.gameObject.SetActive(false);
                }

                hpSlider.enabled = false;
                hpSlider = null;
            }

            // fillImage trong scene co the tro vao anh nen (nay la khung) hoac chinh Hp_Fill.
            // Ca hai deu bo: ruot do do splitFill dieu khien (chay mu), tranh bi dat
            // fillAmount tuc thi de len hieu ung.
            if (fillImage == background || fillImage == splitFill)
                fillImage = null;
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

            AnimateSplitFill();
        }

        private void AnimateSplitFill()
        {
            if (splitFill == null || !fillInitialized)
                return;

            float current = splitFill.fillAmount;
            if (Mathf.Approximately(current, fillTarget))
                return;

            float speed = fillTarget < current ? FillSpeedDown : FillSpeedUp;
            splitFill.fillAmount = Mathf.MoveTowards(current, fillTarget, speed * Time.unscaledDeltaTime);
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

            if (splitFill != null)
            {
                fillTarget = Mathf.Clamp01((float)current / Mathf.Max(max, 1));
                // Lan dau (vao tran): hien dung ngay, khong chay tu day xuong.
                if (!fillInitialized)
                {
                    fillInitialized = true;
                    splitFill.fillAmount = fillTarget;
                }
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
