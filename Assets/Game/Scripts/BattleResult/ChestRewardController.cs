using System;
using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.BattleResult
{
    public enum ChestState
    {
        Closed,
        Opening,
        Hold,
        Revealing,
        Complete
    }

    /// <summary>Controls the in-battle chest and one-tap-per-item reward reveal.</summary>
    public sealed class ChestRewardController : MonoBehaviour
    {
        [Header("Chest")]
        [SerializeField] private GameObject chestPrefab;
        [SerializeField] private Transform chestSpawnPoint;
        [SerializeField] private GameObject interactionLayer;
        [SerializeField] private Button tapButton;

        [Header("Item Display")]
        [SerializeField] private RectTransform itemDisplay;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemName;
        [SerializeField] private TextMeshProUGUI itemQuantity;
        [SerializeField, Min(0.01f)] private float revealDuration = 0.45f;
        [SerializeField, Min(0f)] private float finalItemHoldDuration = 0.65f;

        [Header("Animation")]
        [SerializeField] private string closedAnimation = "ruong";
        [SerializeField] private string openAnimation = "open";
        [SerializeField] private string holdAnimation = "hold";

        public ChestState State { get; private set; } = ChestState.Complete;
        public GameObject SpawnedChest => spawnedChest;

        private readonly List<UI.ItemReward> rewards = new List<UI.ItemReward>();
        private GameObject spawnedChest;
        private SkeletonAnimation chestSkeleton;
        private TrackEntry openTrack;
        private Coroutine revealRoutine;
        private Action onFinished;
        private int nextRewardIndex;
        private bool tapBound;
        private bool rewardActive;
        private Canvas rootCanvas;
        private RectTransform glow;
        private readonly List<RectTransform> sparkleParticles = new List<RectTransform>();

        private void Awake()
        {
            ResolveUi();
            BindTap();
            SetInteractionVisible(false);
        }

        private void OnDestroy()
        {
            UnsubscribeOpenTrack();
            if (tapBound && tapButton != null)
                tapButton.onClick.RemoveListener(HandleTap);
        }

        public void Configure(GameObject prefab, Transform spawnPoint, GameObject inputLayer)
        {
            chestPrefab = prefab;
            chestSpawnPoint = spawnPoint;
            interactionLayer = inputLayer;
            ResolveUi();
            BindTap();
        }

        public void BeginReward(IList<UI.ItemReward> chestRewards, Action finished)
        {
            CancelReward();
            rewardActive = true;
            onFinished = finished;
            rewards.Clear();
            if (chestRewards != null)
            {
                for (int i = 0; i < chestRewards.Count; i++)
                {
                    UI.ItemReward reward = chestRewards[i];
                    if (reward != null && reward.item != null && reward.quantity > 0)
                        rewards.Add(reward);
                }
            }

            nextRewardIndex = 0;
            SetInteractionVisible(true);
            HideLegacyChestVisual();
            SetItemVisible(false);
            SpawnChest();
        }

        public void SpawnChest()
        {
            DestroyChest();
            State = ChestState.Closed;
            if (chestPrefab == null)
            {
                Debug.LogError("[ChestRewardController] Chest prefab is not assigned.", this);
                FinishReward();
                return;
            }

            Vector3 position = chestSpawnPoint != null ? chestSpawnPoint.position : GetRightScreenPosition();
            spawnedChest = Instantiate(chestPrefab, position, Quaternion.identity);
            spawnedChest.name = "RewardChest_Runtime";
            chestSkeleton = spawnedChest.GetComponentInChildren<SkeletonAnimation>(true);
            SetChestAnimation(closedAnimation, true);
            SetGlowVisible(false);
        }

        public void OpenChest()
        {
            if (State != ChestState.Closed || chestSkeleton == null)
                return;

            State = ChestState.Opening;
            UnsubscribeOpenTrack();
            openTrack = SetChestAnimation(openAnimation, false);
            SetGlowVisible(true);
            if (openTrack == null)
            {
                HoldChest();
                return;
            }

            openTrack.Complete += HandleOpenComplete;
        }

        public void HoldChest()
        {
            if (State == ChestState.Complete && !rewardActive)
                return;

            UnsubscribeOpenTrack();
            State = ChestState.Hold;
            SetChestAnimation(holdAnimation, true);
            SetGlowVisible(true);
        }

        public void RevealNextItem()
        {
            if (State != ChestState.Hold || revealRoutine != null)
                return;

            if (nextRewardIndex >= rewards.Count)
            {
                FinishReward();
                return;
            }

            State = ChestState.Revealing;
            revealRoutine = StartCoroutine(RevealItem(rewards[nextRewardIndex++]));
        }

        public void FinishReward()
        {
            if (State == ChestState.Complete)
                return;

            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                revealRoutine = null;
            }

            UnsubscribeOpenTrack();
            SetChestAnimation(closedAnimation, false);
            State = ChestState.Complete;
            rewardActive = false;
            SetGlowVisible(false);
            SetItemVisible(false);
            SetInteractionVisible(false);
            DestroyChest();

            Action finished = onFinished;
            onFinished = null;
            rewards.Clear();
            finished?.Invoke();
        }

        public void CancelReward()
        {
            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                revealRoutine = null;
            }

            UnsubscribeOpenTrack();
            State = ChestState.Complete;
            rewardActive = false;
            onFinished = null;
            rewards.Clear();
            SetGlowVisible(false);
            SetItemVisible(false);
            SetInteractionVisible(false);
            DestroyChest();
        }

        private void HandleTap()
        {
            if (State == ChestState.Closed)
                OpenChest();
            else if (State == ChestState.Hold)
                RevealNextItem();
        }

        private void HandleOpenComplete(TrackEntry entry)
        {
            if (entry == openTrack)
                HoldChest();
        }

        private IEnumerator RevealItem(UI.ItemReward reward)
        {
            PopulateItem(reward);
            SetItemVisible(true);
            Vector2 origin = GetChestCanvasPosition();
            Vector2 destination = origin + Vector2.up * 270f;
            CanvasGroup group = GetOrAddCanvasGroup(itemDisplay != null ? itemDisplay.gameObject : null);
            if (itemDisplay != null)
            {
                itemDisplay.anchoredPosition = origin;
                itemDisplay.localScale = Vector3.zero;
            }
            if (group != null)
                group.alpha = 0f;

            ResetSparkles(origin);
            float elapsed = 0f;
            while (elapsed < revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / revealDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                if (itemDisplay != null)
                {
                    itemDisplay.anchoredPosition = Vector2.Lerp(origin, destination, eased) +
                        Vector2.up * Mathf.Sin(t * Mathf.PI) * 45f;
                    itemDisplay.localScale = Vector3.one * eased;
                }
                if (group != null)
                    group.alpha = eased;
                AnimateSparkles(origin, destination, t);
                yield return null;
            }

            if (itemDisplay != null)
            {
                itemDisplay.anchoredPosition = destination;
                itemDisplay.localScale = Vector3.one;
            }
            if (group != null)
                group.alpha = 1f;
            HideSparkles();
            revealRoutine = null;

            if (nextRewardIndex >= rewards.Count)
            {
                if (finalItemHoldDuration > 0f)
                    yield return new WaitForSecondsRealtime(finalItemHoldDuration);
                FinishReward();
            }
            else
            {
                State = ChestState.Hold;
            }
        }

        private void PopulateItem(UI.ItemReward reward)
        {
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

        private TrackEntry SetChestAnimation(string animationName, bool loop)
        {
            if (chestSkeleton == null || string.IsNullOrEmpty(animationName))
                return null;
            chestSkeleton.Initialize(false);
            if (chestSkeleton.AnimationState == null || chestSkeleton.Skeleton == null ||
                chestSkeleton.Skeleton.Data.FindAnimation(animationName) == null)
            {
                Debug.LogWarning("[ChestRewardController] Missing Spine animation: " + animationName, this);
                return null;
            }
            return chestSkeleton.AnimationState.SetAnimation(0, animationName, loop);
        }

        private void ResolveUi()
        {
            if (interactionLayer == null)
                return;
            if (rootCanvas == null)
                rootCanvas = interactionLayer.GetComponentInParent<Canvas>();
            if (tapButton == null)
                tapButton = FindChildComponent<Button>(interactionLayer.transform, "ClaimArea");
            if (itemDisplay == null)
                itemDisplay = FindChildRect(interactionLayer.transform, "ItemDisplay");
            if (itemIcon == null)
                itemIcon = FindChildComponent<Image>(interactionLayer.transform, "Icon");
            if (itemName == null)
                itemName = FindChildComponent<TextMeshProUGUI>(interactionLayer.transform, "ItemName");
            if (itemQuantity == null)
                itemQuantity = FindChildComponent<TextMeshProUGUI>(interactionLayer.transform, "ItemQty");
            EnsureEffects();
        }

        private void BindTap()
        {
            if (tapBound || tapButton == null)
                return;
            tapButton.onClick.AddListener(HandleTap);
            tapBound = true;
        }

        private void HideLegacyChestVisual()
        {
            if (interactionLayer == null)
                return;
            SkeletonGraphic graphic = interactionLayer.GetComponentInChildren<SkeletonGraphic>(true);
            if (graphic != null)
                graphic.gameObject.SetActive(false);
            Transform dim = FindChild(interactionLayer.transform, "Dim");
            if (dim != null)
                dim.gameObject.SetActive(false);
            Transform hint = FindChild(interactionLayer.transform, "TapHint");
            if (hint != null)
                hint.gameObject.SetActive(true);
        }

        private void EnsureEffects()
        {
            if (interactionLayer == null || itemDisplay == null)
                return;
            if (glow == null)
            {
                var go = new GameObject("ChestGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                glow = go.GetComponent<RectTransform>();
                glow.SetParent(interactionLayer.transform, false);
                glow.sizeDelta = new Vector2(330f, 330f);
                Image image = go.GetComponent<Image>();
                image.color = new Color(1f, 0.75f, 0.15f, 0.24f);
                image.raycastTarget = false;
                glow.SetAsFirstSibling();
            }
            if (sparkleParticles.Count == 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    var go = new GameObject("Sparkle_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    RectTransform particle = go.GetComponent<RectTransform>();
                    particle.SetParent(interactionLayer.transform, false);
                    particle.sizeDelta = Vector2.one * (10f + i % 3 * 5f);
                    Image image = go.GetComponent<Image>();
                    image.color = new Color(1f, 0.9f, 0.35f, 0.9f);
                    image.raycastTarget = false;
                    go.SetActive(false);
                    sparkleParticles.Add(particle);
                }
            }
        }

        private void SetGlowVisible(bool visible)
        {
            if (glow == null)
                return;
            glow.gameObject.SetActive(visible);
            if (visible)
                glow.anchoredPosition = GetChestCanvasPosition();
        }

        private void ResetSparkles(Vector2 origin)
        {
            for (int i = 0; i < sparkleParticles.Count; i++)
            {
                sparkleParticles[i].gameObject.SetActive(true);
                sparkleParticles[i].anchoredPosition = origin;
                sparkleParticles[i].localScale = Vector3.zero;
            }
        }

        private void AnimateSparkles(Vector2 origin, Vector2 destination, float t)
        {
            for (int i = 0; i < sparkleParticles.Count; i++)
            {
                float angle = i * Mathf.PI * 2f / sparkleParticles.Count;
                Vector2 spread = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (45f + 65f * t);
                sparkleParticles[i].anchoredPosition = Vector2.Lerp(origin, destination, t) + spread;
                sparkleParticles[i].localScale = Vector3.one * Mathf.Sin(t * Mathf.PI);
            }
        }

        private void HideSparkles()
        {
            for (int i = 0; i < sparkleParticles.Count; i++)
                sparkleParticles[i].gameObject.SetActive(false);
        }

        private Vector2 GetChestCanvasPosition()
        {
            if (spawnedChest == null || interactionLayer == null)
                return Vector2.zero;
            Camera camera = Camera.main;
            Vector2 screen = camera != null ? camera.WorldToScreenPoint(spawnedChest.transform.position) : Vector2.zero;
            RectTransform parent = interactionLayer.transform as RectTransform;
            Camera uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCamera, out Vector2 local);
            return local;
        }

        private Vector3 GetRightScreenPosition()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return new Vector3(2.3f, 0f, 0f);
            float distance = Mathf.Abs(camera.transform.position.z);
            Vector3 point = camera.ViewportToWorldPoint(new Vector3(0.78f, 0.46f, distance));
            point.z = 0f;
            return point;
        }

        private void SetInteractionVisible(bool visible)
        {
            if (interactionLayer != null)
                interactionLayer.SetActive(visible);
        }

        private void SetItemVisible(bool visible)
        {
            if (itemDisplay != null)
                itemDisplay.gameObject.SetActive(visible);
            if (!visible)
                HideSparkles();
        }

        private void DestroyChest()
        {
            if (spawnedChest != null)
                Destroy(spawnedChest);
            spawnedChest = null;
            chestSkeleton = null;
        }

        private void UnsubscribeOpenTrack()
        {
            if (openTrack != null)
                openTrack.Complete -= HandleOpenComplete;
            openTrack = null;
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            if (target == null)
                return null;
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.AddComponent<CanvasGroup>();
        }

        private static Transform FindChild(Transform root, string childName)
        {
            if (root == null)
                return null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                    return child;
            }
            return null;
        }

        private static RectTransform FindChildRect(Transform root, string childName)
        {
            return FindChild(root, childName) as RectTransform;
        }

        private static T FindChildComponent<T>(Transform root, string childName) where T : Component
        {
            Transform child = FindChild(root, childName);
            return child != null ? child.GetComponentInChildren<T>(true) : null;
        }
    }
}
