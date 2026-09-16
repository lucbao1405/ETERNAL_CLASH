using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Spine.Unity;

namespace EternalClash.UI
{
    [DisallowMultipleComponent]
    public class TownNavigationController : MonoBehaviour
    {
        [Header("Character")]
        [SerializeField] private SkeletonGraphic character;
        [SerializeField] private float moveSpeed = 800f;
        [SerializeField] private float stopDistance = 8f;

        [Header("Animations")]
        [SerializeField] private string runAnimation = "run";
        [SerializeField] private string standAnimation = "stand";
        [SerializeField] private bool originalFacesRight = true;

        [Header("Scroll Follow")]
        [SerializeField] private ScrollRect scrollRect;

        private RectTransform characterRect;
        private float originalScaleX;
        private Coroutine moveCoroutine;
        private SwipePage swipePage;
        private SwipePageCharacterTravel swipeTravel;
        private bool scrollRectWasEnabled;
        private bool swipePageWasEnabled;
        private bool swipeTravelWasEnabled;
        private float followOffsetX;

        public bool IsMoving => moveCoroutine != null;

        private void Awake()
        {
            AutoWireReferences();
        }

        private void AutoWireReferences()
        {
            if (character == null)
            {
                Transform nv = FindDeepChild(transform.root, "Nhan_Vat_Chinh");
                if (nv != null)
                    character = nv.GetComponent<SkeletonGraphic>();
            }

            if (character == null)
                character = FindObjectOfType<SkeletonGraphic>();

            if (character != null)
            {
                characterRect = character.rectTransform;
                originalScaleX = Mathf.Abs(characterRect.localScale.x);
            }

            if (scrollRect == null && characterRect != null && characterRect.parent != null)
                scrollRect = characterRect.parent.GetComponentInParent<ScrollRect>();

            if (scrollRect != null)
            {
                swipePage = scrollRect.GetComponent<SwipePage>();
                swipeTravel = scrollRect.GetComponent<SwipePageCharacterTravel>();
            }
        }

        public void MoveTo(Transform stopPoint, UnityAction onArrive = null)
        {
            if (characterRect == null || stopPoint == null)
            {
                onArrive?.Invoke();
                return;
            }

            RectTransform stopRect = stopPoint as RectTransform;
            if (stopRect == null)
                stopRect = stopPoint.GetComponent<RectTransform>();
            if (stopRect == null)
            {
                onArrive?.Invoke();
                return;
            }

            if (!gameObject.activeInHierarchy)
            {
                onArrive?.Invoke();
                return;
            }

            if (moveCoroutine != null)
                StopCoroutine(moveCoroutine);

            moveCoroutine = StartCoroutine(MoveRoutine(stopRect, onArrive));
        }

        private IEnumerator MoveRoutine(RectTransform stopRect, UnityAction onArrive)
        {
            Vector2 targetPos = ResolveTargetPosition(stopRect);
            bool alreadyThere = Vector2.Distance(characterRect.anchoredPosition, targetPos) <= stopDistance;

            SetScrollControlEnabled(false);
            try
            {
                if (!alreadyThere)
                {
                    // Giữ nhân vật ở đúng vị trí trên màn hình như lúc bắt đầu chạy.
                    followOffsetX = GetCharacterViewportX();
                    SetFacingDirection(targetPos.x > characterRect.anchoredPosition.x);
                    PlayRun();
                }

                while (Vector2.Distance(characterRect.anchoredPosition, targetPos) > stopDistance)
                {
                    characterRect.anchoredPosition = Vector2.MoveTowards(
                        characterRect.anchoredPosition,
                        targetPos,
                        moveSpeed * Time.unscaledDeltaTime);
                    FollowScroll();
                    yield return null;
                }

                moveCoroutine = null;

                SetFacingDirection(true);
                PlayStand();
            }
            finally
            {
                SetScrollControlEnabled(true);
            }

            onArrive?.Invoke();
        }

        private void SetScrollControlEnabled(bool value)
        {
            if (scrollRect == null)
                return;

            if (!value)
            {
                scrollRect.StopMovement();
                scrollRectWasEnabled = scrollRect.enabled;
                swipePageWasEnabled = swipePage != null && swipePage.enabled;
                swipeTravelWasEnabled = swipeTravel != null && swipeTravel.enabled;

                scrollRect.enabled = false;
                if (swipePage != null)
                    swipePage.enabled = false;
                if (swipeTravel != null)
                    swipeTravel.enabled = false;
                return;
            }

            scrollRect.enabled = scrollRectWasEnabled;
            if (swipePage != null)
                swipePage.enabled = swipePageWasEnabled;
            if (swipeTravel != null)
                swipeTravel.enabled = swipeTravelWasEnabled;

            if (swipeTravel != null)
                swipeTravel.SyncCurrentPage();
            if (swipePageWasEnabled)
                swipePage.SyncToCurrentPage();
        }

        private RectTransform GetFollowViewport()
        {
            if (scrollRect == null || !scrollRect.horizontal || scrollRect.content == null)
                return null;

            return scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.content.parent as RectTransform;
        }

        private float GetCharacterViewportX()
        {
            RectTransform viewport = GetFollowViewport();
            if (viewport == null || characterRect == null)
                return 0f;

            return viewport.InverseTransformPoint(characterRect.position).x;
        }

        private void FollowScroll()
        {
            RectTransform viewport = GetFollowViewport();
            if (viewport == null)
                return;

            float overflow = scrollRect.content.rect.width - viewport.rect.width;
            if (overflow <= 0f)
                return;

            // Cuộn đúng bằng lượng nhân vật vừa chạy để vị trí trên màn hình không đổi.
            float characterX = viewport.InverseTransformPoint(characterRect.position).x;
            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(
                scrollRect.horizontalNormalizedPosition + (characterX - followOffsetX) / overflow);
        }

        private Vector2 ResolveTargetPosition(RectTransform stopRect)
        {
            if (stopRect.parent == characterRect.parent)
                return stopRect.anchoredPosition;

            Transform space = characterRect.parent != null ? characterRect.parent : characterRect;
            Vector2 localTarget = space.InverseTransformPoint(stopRect.position);
            Vector2 localCharacter = space.InverseTransformPoint(characterRect.position);
            return characterRect.anchoredPosition + (localTarget - localCharacter);
        }

        public void StopMovement()
        {
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }
            SetFacingDirection(true);
            PlayStand();
        }

        private void SetFacingDirection(bool faceRight)
        {
            if (characterRect == null)
                return;

            float direction = originalFacesRight ? 1f : -1f;
            Vector3 scale = characterRect.localScale;
            scale.x = originalScaleX * (faceRight ? direction : -direction);
            characterRect.localScale = scale;
        }

        private void PlayRun()
        {
            if (character != null && character.AnimationState != null)
                character.AnimationState.SetAnimation(0, runAnimation, true);
        }

        private void PlayStand()
        {
            if (character != null && character.AnimationState != null)
                character.AnimationState.SetAnimation(0, standAnimation, true);
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
