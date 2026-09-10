using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalClash.Village;

namespace EternalClash.UI
{
    /// <summary>
    /// Win / Lose result popup.
    ///
    /// ShowVictory(exp, gold, time, items) -> banner Victory + EXP + Gold + Time
    /// + danh sách item đã gộp (battle loot + chest reward).
    ///
    /// ShowDefeat(exp, time, items) -> banner Defeat + Time survived + EXP nếu có
    /// + item nhặt được trong trận (không có chest).
    ///
    /// Danh sách item được đổ vào:
    ///  1. Một grid các RewardItemSlot (itemSlotPrefab hoặc slot con sẵn có).
    ///  2. Nếu không có grid/slot -> text list (itemListText) "Tên xN".
    ///
    /// Nút Return/Continue được bind tự động; khi bấm thì gọi continueAction.
    /// </summary>
    public class BattleResultUI : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI expText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Slider expBar;

        [Header("Items")]
        [SerializeField] private RectTransform itemGrid;
        [SerializeField] private GameObject itemSlotPrefab;
        [SerializeField] private TextMeshProUGUI itemListText;
        [SerializeField] private GameObject itemsRoot;

        [Header("Action")]
        [SerializeField] private Button returnButton;

        public bool HasExitControl => returnButton != null;

        private Action onContinue;
        private bool buttonBound;
        private bool visualsResolved;
        private readonly List<RewardItemSlot> slotPool = new List<RewardItemSlot>();

        private void Awake()
        {
            ResolveVisuals();
            BindButton();
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        public void ShowVictory(int exp, int gold, float battleTime, IList<ItemReward> items, Action continueAction)
        {
            ShowCore(exp, gold, battleTime, true, items, continueAction);
        }

        public void ShowDefeat(int exp, float battleTime, IList<ItemReward> items, Action continueAction)
        {
            ShowCore(exp, 0, battleTime, false, items, continueAction);
        }

        public void Close()
        {
            if (gameObject != null)
                gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------
        // Population
        // ------------------------------------------------------------------

        private void ShowCore(int exp, int gold, float battleTime, bool victory, IList<ItemReward> items, Action continueAction)
        {
            onContinue = continueAction;
            BindButton();

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            // EXP
            if (expText != null)
            {
                expText.text = $"EXP +{exp}";
                expText.gameObject.SetActive(victory || exp > 0);
            }

            // Gold (chỉ hiện khi thắng)
            if (goldText != null)
            {
                goldText.gameObject.SetActive(victory && gold > 0);
                if (victory && gold > 0)
                    goldText.text = $"Gold +{gold}";
            }

            // Time
            if (timeText != null)
            {
                timeText.text = victory
                    ? $"Time  {FormatTime(battleTime)}"
                    : $"Time Survived  {FormatTime(battleTime)}";
                timeText.gameObject.SetActive(true);
            }

            // XP bar progress (chỉ khi thắng)
            if (expBar != null)
            {
                expBar.gameObject.SetActive(victory);
                if (victory)
                {
                    PlayerStatSystem stats = PlayerStatSystem.Instance;
                    float ratio = stats != null && stats.RequiredExp > 0
                        ? (float)stats.CurrentExp / stats.RequiredExp
                        : 0f;
                    expBar.value = Mathf.Clamp01(ratio);
                }
            }

            PopulateItems(items);
        }

        private void PopulateItems(IList<ItemReward> items)
        {
            int count = items != null ? items.Count : 0;
            bool hasItems = count > 0;

            if (itemsRoot != null)
                itemsRoot.SetActive(hasItems);

            if (itemListText != null)
            {
                itemListText.text = string.Empty;
                itemListText.gameObject.SetActive(false);
            }

            if (itemGrid != null)
            {
                ClearSlots();
                for (int i = 0; i < count; i++)
                {
                    ItemReward reward = items[i];
                    if (reward == null) continue;

                    RewardItemSlot slot = GetOrCreateSlot(i);
                    if (slot == null) break;

                    slot.gameObject.SetActive(true);
                    slot.SetSprite(LoadIcon(reward));
                    slot.SetQuantity(reward.quantity);
                    slot.SetItem(reward.item);
                }
            }
        }

        private void ClearSlots()
        {
            List<RewardItemSlot> slots = CollectSlots();
            foreach (RewardItemSlot slot in slots)
            {
                if (slot != null)
                    slot.Clear();
            }
        }

        private RewardItemSlot GetOrCreateSlot(int index)
        {
            List<RewardItemSlot> slots = CollectSlots();
            if (index < slots.Count)
                return slots[index];

            GameObject template = ResolveSlotTemplate();
            if (template == null)
                return null;

            GameObject clone = Instantiate(template, itemGrid, false);
            RewardItemSlot slot = clone.GetComponent<RewardItemSlot>();
            if (slot == null)
                slot = clone.AddComponent<RewardItemSlot>();
            slotPool.Add(slot);
            slot.Clear();
            return slot;
        }

        private List<RewardItemSlot> CollectSlots()
        {
            var result = new List<RewardItemSlot>();
            var seen = new HashSet<RewardItemSlot>();

            if (itemGrid != null)
            {
                foreach (RewardItemSlot slot in itemGrid.GetComponentsInChildren<RewardItemSlot>(true))
                {
                    if (slot != null && seen.Add(slot))
                        result.Add(slot);
                }
            }

            foreach (RewardItemSlot slot in slotPool)
            {
                if (slot != null && seen.Add(slot))
                    result.Add(slot);
            }

            return result;
        }

        private GameObject ResolveSlotTemplate()
        {
            if (itemSlotPrefab != null)
                return itemSlotPrefab;

            if (itemGrid == null)
                return null;

            foreach (RewardItemSlot slot in itemGrid.GetComponentsInChildren<RewardItemSlot>(true))
            {
                if (slot != null)
                    return slot.gameObject;
            }

            if (itemGrid.childCount > 0)
                return itemGrid.GetChild(0).gameObject;

            return null;
        }

        // ------------------------------------------------------------------
        // Button
        // ------------------------------------------------------------------

        private void BindButton()
        {
            if (buttonBound) return;
            buttonBound = true;
            if (returnButton != null)
                returnButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnContinueClicked()
        {
            Action callback = onContinue;
            onContinue = null;
            Close();
            callback?.Invoke();
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int minutes = total / 60;
            int secs = total % 60;
            return $"{minutes:00}:{secs:00}";
        }

        private static Sprite LoadIcon(ItemReward reward)
        {
            if (reward == null || reward.item == null)
                return null;
            if (reward.item.icon != null)
                return reward.item.icon;
            if (string.IsNullOrEmpty(reward.item.iconSpriteName))
                return null;
            return Resources.Load<Sprite>(reward.item.iconSpriteName);
        }

        // ------------------------------------------------------------------
        // Auto-resolve (best effort) - user vẫn nên gán Inspector
        // ------------------------------------------------------------------

        private void ResolveVisuals()
        {
            if (visualsResolved) return;
            visualsResolved = true;

            if (expText == null)
                expText = FindLabel("Xp_Text", "Xp", "Exp", "EXP");
            if (timeText == null)
                timeText = FindLabel("TImer", "Timer", "Time");
            if (goldText == null)
                goldText = FindLabel("Gold", "Vang", "GoldText");
            if (expBar == null)
                expBar = FindSlider("Xp_Slide", "Xp", "ExpBar");
            if (itemGrid == null)
                itemGrid = FindRect("Hienthivp", "ItemGrid", "RewardGrid");
            if (returnButton == null)
                returnButton = FindButton("Return", "ReturnButton", "Continue", "Done", "Close");
        }

        private TextMeshProUGUI FindLabel(params string[] names)
        {
            foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text == null) continue;

                Transform current = text.transform;
                while (current != null && current != transform)
                {
                    if (HasName(current.name, names))
                        return text;
                    current = current.parent;
                }
            }
            return null;
        }

        private Slider FindSlider(params string[] names)
        {
            foreach (Slider slider in GetComponentsInChildren<Slider>(true))
            {
                if (slider == null) continue;

                Transform current = slider.transform;
                while (current != null && current != transform)
                {
                    if (HasName(current.name, names))
                        return slider;
                    current = current.parent;
                }
            }
            return null;
        }

        private RectTransform FindRect(params string[] names)
        {
            foreach (RectTransform rect in GetComponentsInChildren<RectTransform>(true))
            {
                if (rect == null || rect == transform) continue;
                if (HasName(rect.name, names))
                    return rect;
            }
            return null;
        }

        private Button FindButton(params string[] names)
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button == null) continue;
                if (HasName(button.name, names))
                    return button;
            }
            return null;
        }

        private static bool HasName(string name, params string[] names)
        {
            string normalized = Normalize(name);
            foreach (string candidate in names)
            {
                if (normalized == Normalize(candidate))
                    return true;
            }
            return false;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var buffer = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c))
                    buffer.Append(char.ToLowerInvariant(c));
            }
            return buffer.ToString();
        }
    }
}
