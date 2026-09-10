using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.UI
{
    /// <summary>Reveals chest-only rewards one player tap at a time after HOLD.</summary>
    public sealed class ChestRewardRevealController : MonoBehaviour
    {
        [SerializeField] private RectTransform itemDisplay;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemName;
        [SerializeField] private TextMeshProUGUI itemQuantity;
        [SerializeField] private RectTransform chestAnchor;
        [SerializeField] private RectTransform displayAnchor;
        [SerializeField, Min(0.01f)] private float flightDuration = 0.4f;

        public bool IsReadyForTap { get; private set; }
        public bool IsComplete { get; private set; }
        public event Action AllItemsRevealed;

        private readonly List<ItemReward> chestRewards = new List<ItemReward>();
        private int nextIndex;
        private Coroutine flight;

        public void Configure(
            RectTransform display,
            Image icon,
            TextMeshProUGUI nameLabel,
            TextMeshProUGUI quantityLabel,
            RectTransform sourceAnchor,
            RectTransform destinationAnchor,
            float duration)
        {
            itemDisplay = display;
            itemIcon = icon;
            itemName = nameLabel;
            itemQuantity = quantityLabel;
            chestAnchor = sourceAnchor;
            displayAnchor = destinationAnchor;
            flightDuration = Mathf.Max(0.01f, duration);
        }

        public void StartReveal(IList<ItemReward> rewards, ChestAnimationController chestAnimation)
        {
            StopReveal();
            chestRewards.Clear();
            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    ItemReward reward = rewards[i];
                    if (reward != null && reward.item != null && reward.quantity > 0)
                        chestRewards.Add(reward);
                }
            }

            nextIndex = 0;
            IsReadyForTap = chestAnimation != null && chestAnimation.State == ChestState.Hold;
            IsComplete = chestRewards.Count == 0;
            SetDisplayVisible(false);
            if (IsComplete)
                AllItemsRevealed?.Invoke();
        }

        /// <summary>Returns false only when no reward remains to show.</summary>
        public bool RevealNextItem()
        {
            if (!IsReadyForTap || flight != null)
                return true;

            if (nextIndex >= chestRewards.Count)
            {
                IsComplete = true;
                AllItemsRevealed?.Invoke();
                return false;
            }

            ItemReward reward = chestRewards[nextIndex++];
            Populate(reward);
            flight = StartCoroutine(FlyItem());
            return true;
        }

        public bool HasPendingItem => nextIndex < chestRewards.Count;

        public bool IsAnimating => flight != null;

        public void FinishReveal()
        {
            IsReadyForTap = false;
            IsComplete = true;
            if (flight != null)
            {
                StopCoroutine(flight);
                flight = null;
            }
        }

        private void StopReveal()
        {
            IsReadyForTap = false;
            IsComplete = false;
            if (flight != null)
            {
                StopCoroutine(flight);
                flight = null;
            }
        }

        private void Populate(ItemReward reward)
        {
            SetDisplayVisible(true);
            Sprite icon = reward.item.icon;
            if (icon == null && !string.IsNullOrEmpty(reward.item.iconSpriteName))
                icon = Resources.Load<Sprite>(reward.item.iconSpriteName);

            if (itemIcon != null)
            {
                itemIcon.sprite = icon;
                itemIcon.enabled = icon != null;
            }
            if (itemName != null)
                itemName.text = string.IsNullOrEmpty(reward.item.itemName) ? reward.item.itemId : reward.item.itemName;
            if (itemQuantity != null)
                itemQuantity.text = "x" + reward.quantity;
        }

        private IEnumerator FlyItem()
        {
            if (itemDisplay == null)
            {
                flight = null;
                yield break;
            }

            Vector3 destination = displayAnchor != null ? displayAnchor.position : itemDisplay.position;
            Vector3 origin = chestAnchor != null ? chestAnchor.position : destination + Vector3.down * 160f;
            CanvasGroup group = itemDisplay.GetComponent<CanvasGroup>();
            if (group == null)
                group = itemDisplay.gameObject.AddComponent<CanvasGroup>();

            itemDisplay.position = origin;
            itemDisplay.localScale = Vector3.one * 0.45f;
            group.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < flightDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / flightDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                itemDisplay.position = Vector3.Lerp(origin, destination, eased) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 50f;
                itemDisplay.localScale = Vector3.Lerp(Vector3.one * 0.45f, Vector3.one, eased);
                group.alpha = eased;
                yield return null;
            }

            itemDisplay.position = destination;
            itemDisplay.localScale = Vector3.one;
            group.alpha = 1f;
            flight = null;

            if (nextIndex >= chestRewards.Count && !IsComplete)
            {
                IsComplete = true;
                AllItemsRevealed?.Invoke();
            }
        }

        private void SetDisplayVisible(bool visible)
        {
            if (itemDisplay != null)
                itemDisplay.gameObject.SetActive(visible);
        }
    }
}
