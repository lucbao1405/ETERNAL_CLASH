using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.Tutorial
{
    /// <summary>Reusable pulse outline that does not replace existing button styling.</summary>
    [DisallowMultipleComponent]
    public sealed class TutorialHighlight : MonoBehaviour
    {
        [SerializeField] private Color highlightColor = new Color(1f, 0.82f, 0.2f, 1f);
        [SerializeField, Min(0.1f)] private float pulseSpeed = 3f;

        private Outline outline;
        private bool createdOutline;
        private bool highlighted;

        private void Awake()
        {
            outline = GetComponent<Outline>();
            if (outline == null)
            {
                outline = gameObject.AddComponent<Outline>();
                createdOutline = true;
            }
            outline.enabled = false;
        }

        private void Update()
        {
            if (!highlighted || outline == null)
                return;

            float width = Mathf.Lerp(2f, 7f, (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f);
            outline.effectColor = highlightColor;
            outline.effectDistance = new Vector2(width, -width);
        }

        public void SetHighlighted(bool value)
        {
            highlighted = value;
            if (outline != null)
                outline.enabled = value;
        }

        private void OnDestroy()
        {
            if (createdOutline && outline != null)
                Destroy(outline);
        }
    }
}
