using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EternalClash.Data;
using EternalClash.Village;

namespace EternalClash.UI
{
    [DisallowMultipleComponent]
    public sealed class UpgradeResultPopup : MonoBehaviour
    {
        [SerializeField] private GameObject successPanel;
        [SerializeField] private TMP_Text successTitleText;
        [SerializeField] private TMP_Text successDetailsText;
        [SerializeField] private TMP_Text statIncreaseText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Image itemIcon;
        [SerializeField] private Button successCloseButton;

        private void Awake()
        {
            AutoWire();
            successCloseButton?.onClick.AddListener(Close);
            Close();
        }

        private void OnDestroy()
        {
            successCloseButton?.onClick.RemoveListener(Close);
        }

        public void ShowSuccess(ItemData item, int previousUpgradeLevel, int currentUpgradeLevel,
            string statName, int previousStat, int currentStat)
        {
            if (successTitleText != null)
                successTitleText.text = "UPGRADE SUCCESS";

            int increase = Mathf.Max(0, currentStat - previousStat);
            if (statIncreaseText != null)
                statIncreaseText.text = statName + " +" + increase;
            if (levelText != null)
                levelText.text = "+" + currentUpgradeLevel;
            if (itemIcon != null)
            {
                ItemData catalogItem = item != null ? ItemCatalog.Find(item.itemId) : null;
                itemIcon.sprite = item != null && item.icon != null ? item.icon : catalogItem?.icon;
                itemIcon.enabled = itemIcon.sprite != null;
            }
            if (successDetailsText != null && statIncreaseText == null && levelText == null)
                successDetailsText.text = (item != null ? item.itemName : "") + "\n\nUpgrade +" +
                    previousUpgradeLevel + " -> +" + currentUpgradeLevel + "\n\n" + statName +
                    " +" + previousStat + " -> " + statName + " +" + currentStat;

            successPanel?.SetActive(true);
        }

        public void Close()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
            successPanel?.SetActive(false);
        }

        private void AutoWire()
        {
            successPanel ??= transform.Find("UpGradeSuccess")?.gameObject
                ?? FindChildRecursive(transform, "UpGradeSuccess")?.gameObject;

            successTitleText ??= FindText(successPanel, "Thong_bao/Thong Bao/T")
                ?? FindTextRecursive(successPanel, "T");
            successDetailsText ??= FindText(successPanel, "Thong_bao/Chi_tiet/Chiso_tang")
                ?? FindTextRecursive(successPanel, "Chiso_tang");
            statIncreaseText ??= FindTextRecursive(successPanel, "Chiso_tang");
            levelText ??= FindTextRecursive(successPanel, "+Level");
            itemIcon ??= FindImageRecursive(successPanel, "ItemIcon");
            itemIcon ??= FindImageRecursive(successPanel, "Itemicon");

            successCloseButton ??= successPanel?.transform.Find("X")?.GetComponent<Button>()
                ?? successPanel?.GetComponentInChildren<Button>(true);
        }

        private static TMP_Text FindText(GameObject panel, string path)
        {
            return panel != null ? panel.transform.Find(path)?.GetComponent<TMP_Text>() : null;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
                return null;
            if (string.Equals(root.name, childName, System.StringComparison.OrdinalIgnoreCase))
                return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform result = FindChildRecursive(root.GetChild(index), childName);
                if (result != null)
                    return result;
            }
            return null;
        }

        private static TMP_Text FindTextRecursive(GameObject root, string childName)
        {
            Transform child = FindChildRecursive(root != null ? root.transform : null, childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static Image FindImageRecursive(GameObject root, string childName)
        {
            Transform child = FindChildRecursive(root != null ? root.transform : null, childName);
            return child != null ? child.GetComponent<Image>() : null;
        }
    }
}
