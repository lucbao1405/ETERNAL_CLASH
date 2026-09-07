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
        [SerializeField] private float delayBeforeDeplete = 0.4f;
        [SerializeField] private float depleteSpeed = 1.5f;

        private TextMeshProUGUI hpText;
        private float targetFillAmount = 1f;
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
