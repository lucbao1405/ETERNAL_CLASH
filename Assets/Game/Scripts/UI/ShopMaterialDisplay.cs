using EternalClash.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.UI
{
    // Shared presentation only; inventory and currency remain owned by the save systems.
    internal static class ShopMaterialDisplay
    {
        public static void Refresh(Image icon, TMP_Text quantityText, ItemData item, int required, bool enough)
        {
            Color tint = enough ? Color.white : new Color(0.35f, 0.35f, 0.35f, 1f);
            if (icon != null)
            {
                icon.sprite = item != null ? item.icon : null;
                icon.enabled = icon.sprite != null;
                icon.preserveAspect = true;
                icon.color = tint;
            }

            if (quantityText != null)
            {
                quantityText.text = "x" + required;
                quantityText.color = tint;
            }
        }
    }
}
