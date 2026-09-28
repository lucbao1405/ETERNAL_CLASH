using System;
using System.Collections;
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

        [Header("EXP Bar Animation")]
        [Tooltip("So giay de thanh EXP chay tu rong toi day.")]
        [SerializeField, Min(0.1f)] private float expFullBarDuration = 2f;
        [Tooltip("So giay nhan vat dien animation thang khi thanh EXP day (len cap).")]
        [SerializeField, Min(0f)] private float levelUpAnimDuration = 2f;
        [SerializeField] private string levelUpAnimation = "victory";
        [Tooltip("Nhan vat Spine trong popup. De trong thi tim con ten Nhan_Vat_Chinh, khong co thi dung Player ngoai tran.")]
        [SerializeField] private Spine.Unity.SkeletonGraphic popupCharacter;

        [Header("Items")]
        [SerializeField] private RectTransform itemGrid;
        [SerializeField] private GameObject itemSlotPrefab;
        [SerializeField] private TextMeshProUGUI itemListText;
        [SerializeField] private GameObject itemsRoot;

        [Header("Action")]
        [SerializeField] private Button returnButton;

        [Header("Banner Animation (Spine)")]
        [Tooltip("Banner Spine cua popup (vd VictoryBanner). De trong thi tu tim con ten VictoryBanner.")]
        [SerializeField] private Spine.Unity.SkeletonGraphic bannerAnimation;
        [SerializeField] private string bannerIntroAnimation = "hien";
        [SerializeField] private string bannerLoopAnimation = "keep";

        public bool HasExitControl => returnButton != null;

        private const string BannerObjectName = "VictoryBanner";

        private Action onContinue;
        private bool buttonBound;
        private bool visualsResolved;
        private readonly List<RewardItemSlot> slotPool = new List<RewardItemSlot>();
        private Coroutine expRoutine;
        private Coroutine levelUpRoutine;
        private Action restoreCharacterAnim;

        private void Awake()
        {
            ResolveVisuals();
            BindButton();
        }

        // Popup duoc SetActive(true) roi moi fade in, nen phat banner o day de hieu ung
        // "bat ra" chay ngay tu luc popup bat dau hien, khong doi toi ShowVictory.
        private void OnEnable()
        {
            PlayBanner();
        }

        /// <summary>Phat intro 1 lan ("hien") roi lap animation giu ("keep").</summary>
        private void PlayBanner()
        {
            if (bannerAnimation == null)
            {
                Transform found = FindChildByName(transform, BannerObjectName);
                bannerAnimation = found != null ? found.GetComponent<Spine.Unity.SkeletonGraphic>() : null;
            }

            if (bannerAnimation == null || bannerAnimation.skeletonDataAsset == null)
                return;

            bannerAnimation.Initialize(false);
            Spine.AnimationState state = bannerAnimation.AnimationState;
            Spine.SkeletonData data = bannerAnimation.Skeleton?.Data;
            if (state == null || data == null)
                return;

            bool hasIntro = data.FindAnimation(bannerIntroAnimation) != null;
            bool hasLoop = data.FindAnimation(bannerLoopAnimation) != null;

            if (hasIntro)
            {
                state.SetAnimation(0, bannerIntroAnimation, false);
                if (hasLoop)
                    state.AddAnimation(0, bannerLoopAnimation, true, 0f);
            }
            else if (hasLoop)
            {
                state.SetAnimation(0, bannerLoopAnimation, true);
            }

            // Ap ngay khung dau cua intro (banner nho 50%) de khong loe len 1 frame o tu the goc.
            bannerAnimation.Update(0f);
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            foreach (Transform child in root)
            {
                if (child.name == childName)
                    return child;
                Transform nested = FindChildByName(child, childName);
                if (nested != null)
                    return nested;
            }
            return null;
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

        /// <summary>
        /// Cap nhat lai danh sach vat pham tren popup (sau khi xem ad X2) ma khong
        /// chay lai animation thanh EXP hay banner.
        /// </summary>
        public void RefreshRewards(IList<ItemReward> items)
        {
            PopulateItems(items);
        }

        public void Close()
        {
            StopExpAnimation();
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
                expText.text = $"+{exp}";
                // Thua cung hien (ke ca +0) de Lose popup giong Win popup.
                expText.gameObject.SetActive(true);
            }

            // Gold is already represented by the Gold loot slot. Do not show a
            // second textual reward line in the result popup.
            if (goldText != null)
                goldText.gameObject.SetActive(false);

            // Time
            if (timeText != null)
            {
                timeText.text = FormatTime(battleTime);
                timeText.gameObject.SetActive(true);
            }

            // XP bar: chay dan tu muc dau tran len muc hien tai (ca Win lan Lose)
            if (expBar != null)
            {
                expBar.gameObject.SetActive(true);
                StopExpAnimation();
                expRoutine = StartCoroutine(AnimateExpBar());
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
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
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
        // EXP bar animation
        // ------------------------------------------------------------------

        private void OnDisable()
        {
            StopExpAnimation();
        }

        private void StopExpAnimation()
        {
            if (expRoutine != null)
            {
                StopCoroutine(expRoutine);
                expRoutine = null;
            }

            if (levelUpRoutine != null)
            {
                StopCoroutine(levelUpRoutine);
                levelUpRoutine = null;
            }

            // Dang dien animation len cap thi tra nhan vat ve animation cu.
            restoreCharacterAnim?.Invoke();
            restoreCharacterAnim = null;
        }

        /// <summary>
        /// Thanh EXP chay tu muc dau tran len muc hien tai voi toc do khong doi
        /// (rong -> day mat <see cref="expFullBarDuration"/> giay). Moi lan day
        /// (len cap): thanh ve 0 ngay va chay tiep phan EXP con lai, dong thoi
        /// nhan vat dien animation thang trong <see cref="levelUpAnimDuration"/> giay.
        /// </summary>
        private IEnumerator AnimateExpBar()
        {
            PlayerStatSystem stats = PlayerStatSystem.Instance;
            if (stats == null)
            {
                expBar.value = 0f;
                expRoutine = null;
                yield break;
            }

            int endLevel = stats.Level;
            float endRatio = stats.RequiredExp > 0 ? Mathf.Clamp01((float)stats.CurrentExp / stats.RequiredExp) : 0f;

            int level = stats.SessionStartLevel;
            // Khong co moc dau tran hop le (vd vao thang scene Battle khi test) thi
            // chi chay tu 0 toi muc hien tai.
            if (level < 1 || level > endLevel)
            {
                level = endLevel;
                expBar.value = 0f;
            }
            else
            {
                int required = PlayerStatSystem.RequiredExpForLevel(level);
                expBar.value = required > 0 ? Mathf.Clamp01((float)stats.SessionStartExp / required) : 0f;
            }

            while (level < endLevel)
            {
                yield return FillExpBarTo(1f);
                // Reset thanh ngay; animation thang chay song song, khong cho.
                StartLevelUpAnimation();
                level++;
                expBar.value = 0f;
            }

            yield return FillExpBarTo(endRatio);
            expRoutine = null;
        }

        private IEnumerator FillExpBarTo(float target)
        {
            float speed = 1f / Mathf.Max(0.1f, expFullBarDuration);
            while (expBar.value < target)
            {
                // Thoi gian thuc: van chay neu game dang tam dung.
                expBar.value = Mathf.MoveTowards(expBar.value, target, speed * Time.unscaledDeltaTime);
                yield return null;
            }
        }

        private void StartLevelUpAnimation()
        {
            if (levelUpRoutine != null)
                StopCoroutine(levelUpRoutine);
            levelUpRoutine = StartCoroutine(PlayLevelUpAnimation());
        }

        private IEnumerator PlayLevelUpAnimation()
        {
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.PlayerLevelUp);

            Spine.AnimationState state = ResolveCharacterAnimationState(out Spine.Skeleton skeleton);
            // Dang dien do (len cap lien tiep) thi giu nguyen cach tra ve animation goc,
            // chi phat lai tu dau va tinh lai 2 giay.
            if (restoreCharacterAnim != null && state != null)
            {
                state.SetAnimation(0, levelUpAnimation, true);
            }
            else if (state != null && skeleton != null && skeleton.Data.FindAnimation(levelUpAnimation) != null)
            {
                Spine.TrackEntry current = state.GetCurrent(0);
                string previous = current != null && current.Animation != null ? current.Animation.Name : null;
                bool previousLoop = current == null || current.Loop;

                state.SetAnimation(0, levelUpAnimation, true);
                restoreCharacterAnim = () =>
                {
                    if (!string.IsNullOrEmpty(previous) && previous != levelUpAnimation)
                        state.SetAnimation(0, previous, previousLoop);
                };
            }

            yield return new WaitForSecondsRealtime(levelUpAnimDuration);

            restoreCharacterAnim?.Invoke();
            restoreCharacterAnim = null;
            levelUpRoutine = null;
        }

        /// <summary>Nhan vat trong popup (Nhan_Vat_Chinh); khong co thi dung Player ngoai tran.</summary>
        private Spine.AnimationState ResolveCharacterAnimationState(out Spine.Skeleton skeleton)
        {
            skeleton = null;

            if (popupCharacter == null)
            {
                foreach (Spine.Unity.SkeletonGraphic graphic in GetComponentsInChildren<Spine.Unity.SkeletonGraphic>(true))
                {
                    if (graphic != null && graphic.name == "Nhan_Vat_Chinh")
                    {
                        popupCharacter = graphic;
                        break;
                    }
                }
            }

            if (popupCharacter != null && popupCharacter.isActiveAndEnabled)
            {
                if (!popupCharacter.IsValid)
                    popupCharacter.Initialize(false);
                skeleton = popupCharacter.Skeleton;
                return popupCharacter.AnimationState;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Spine.Unity.SkeletonAnimation world = player != null
                ? player.GetComponentInChildren<Spine.Unity.SkeletonAnimation>()
                : null;
            if (world == null || !world.valid)
                return null;

            skeleton = world.Skeleton;
            return world.AnimationState;
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
