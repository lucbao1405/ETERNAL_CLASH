using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.Enemy
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private EnemyHealthSystem healthSystem;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image delayedFillImage;
        [SerializeField] private float delayBeforeDeplete = 0.3f;
        [SerializeField] private float depleteSpeed = 1.5f;

        [Header("Juice")]
        [SerializeField] private Color fillColor = Color.red;
        [SerializeField] private Color lostHealthColor = Color.yellow;

        private TextMeshProUGUI hpText;
        private float targetFillAmount = 1f;
        private float displayedFillAmount = 1f;
        private float delayTimer;

        private void Awake()
        {
            if (healthSystem == null)
                healthSystem = GetComponentInParent<EnemyHealthSystem>();

            ResolveReferences();
            FixCanvasDepth();
        }

        private void Start()
        {
            if (fillImage != null)
                fillImage.color = fillColor;
            if (delayedFillImage != null)
                delayedFillImage.color = lostHealthColor;

            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += UpdateHealthBar;
                UpdateHealthBar(healthSystem.CurrentHealth, healthSystem.MaxHealth);

                // Khoi tao ruot hien thi dung gia tri ban dau, khong chay hieu ung.
                displayedFillAmount = targetFillAmount;
                if (fillImage != null)
                    fillImage.fillAmount = displayedFillAmount;
                if (delayedFillImage != null)
                    delayedFillImage.fillAmount = displayedFillAmount;
            }
        }

        private void OnDestroy()
        {
            if (healthSystem != null)
                healthSystem.OnHealthChanged -= UpdateHealthBar;
        }

        private void Update()
        {
            // Vien vang (delayed fill) de lai 0.3 giay roi moi tut theo.
            if (delayedFillImage != null)
            {
                if (delayedFillImage.fillAmount <= targetFillAmount)
                {
                    delayedFillImage.fillAmount = targetFillAmount;
                }
                else
                {
                    if (delayTimer > 0f)
                    {
                        delayTimer -= Time.deltaTime;
                    }
                    else
                    {
                        delayedFillImage.fillAmount = Mathf.MoveTowards(
                            delayedFillImage.fillAmount,
                            targetFillAmount,
                            depleteSpeed * Time.deltaTime);
                    }
                }
            }
        }

        private void ResolveReferences()
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            if (fillImage == null)
                fillImage = FindImage(images, "HP_Fill");
            if (delayedFillImage == null)
                delayedFillImage = FindImage(images, "HP_DelayedFill");

            if (fillImage != null && delayedFillImage != null)
            {
                Transform delayed = delayedFillImage.transform;
                Transform fill = fillImage.transform;
                if (delayed.GetSiblingIndex() > fill.GetSiblingIndex())
                    delayed.SetSiblingIndex(fill.GetSiblingIndex());
            }

            Canvas worldCanvas = GetComponentInChildren<Canvas>(true);
            if (worldCanvas == null || worldCanvas.renderMode != RenderMode.WorldSpace)
                return;

            Transform canvasRect = worldCanvas.transform;
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child == transform || !string.Equals(child.name, "HP_Text", System.StringComparison.Ordinal))
                    continue;

                if (hpText == null)
                    hpText = child.GetComponent<TextMeshProUGUI>();

                if (child.parent != canvasRect)
                    child.SetParent(canvasRect, false);

                break;
            }
        }

        private void FixCanvasDepth()
        {
            Canvas worldCanvas = GetComponentInChildren<Canvas>(true);
            if (worldCanvas == null || worldCanvas.renderMode != RenderMode.WorldSpace)
                return;

            Vector3 localPosition = worldCanvas.transform.localPosition;
            worldCanvas.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        }

        private static Image FindImage(Image[] images, string name)
        {
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].name == name)
                    return images[i];
            }

            return null;
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

            if (hpText != null)
                hpText.text = current + "/" + max;

            delayTimer = delayBeforeDeplete;
        }
    }
}
