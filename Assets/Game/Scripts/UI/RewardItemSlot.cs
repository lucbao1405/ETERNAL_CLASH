using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EternalClash.UI
{
    /// <summary>
    /// One reward item slot inside the Win/Lose popup grid.
    /// Holds an ItemPic image and a Soluong text label.
    /// </summary>
    public class RewardItemSlot : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image itemPic;
        [SerializeField] private TMP_Text soluongText;

        public Image ItemPic => itemPic;
        public TMP_Text SoluongText => soluongText;

        private void Awake()
        {
            if (itemPic == null)
            {
                Image childPic = FindChildImage(transform, "ItemPic");
                itemPic = childPic != null ? childPic : GetComponent<Image>();
            }

            if (soluongText == null)
                soluongText = GetComponentInChildren<TMP_Text>(true);
        }

        // Slot "1..8": root Image là khung slot, icon thật nằm ở Image con "ItemPic".
        private static Image FindChildImage(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (Normalize(child.name) == Normalize(name))
                    return child.GetComponent<Image>();
                Image nested = FindChildImage(child, name);
                if (nested != null)
                    return nested;
            }
            return null;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var buffer = new System.Text.StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c))
                    buffer.Append(char.ToLowerInvariant(c));
            }
            return buffer.ToString();
        }

        public void SetSprite(Sprite sprite)
        {
            if (itemPic != null)
            {
                itemPic.enabled = sprite != null;
                itemPic.sprite = sprite;
            }
        }

        public void SetQuantity(int quantity)
        {
            if (soluongText != null)
            {
                soluongText.enabled = quantity > 0;
                soluongText.text = quantity > 0 ? $"x{quantity}" : string.Empty;
            }
        }

        public void Clear()
        {
            if (itemPic != null)
            {
                itemPic.enabled = false;
                itemPic.sprite = null;
            }

            if (soluongText != null)
            {
                soluongText.text = string.Empty;
                soluongText.enabled = false;
            }

            gameObject.SetActive(false);
        }
    }
}