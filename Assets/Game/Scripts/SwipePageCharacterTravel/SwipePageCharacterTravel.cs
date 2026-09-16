using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Spine.Unity;

public class SwipePageCharacterTravel : MonoBehaviour,
    IBeginDragHandler,
    IEndDragHandler
{
    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float minimumSwipeDistance = 100f;
    [SerializeField] private float pageMoveDuration = 0.4f;

    [Header("Character")]
    [SerializeField] private SkeletonGraphic character;
    [SerializeField] private RectTransform[] characterStops;

    [Header("Character movement")]
    [SerializeField] private float characterStartDelay = 0.1f;
    [SerializeField] private float characterMoveDuration = 2f;

    [Header("Animations")]
    [SerializeField] private string standAnimation = "stand";
    [SerializeField] private string runAnimation = "run";
    [SerializeField] private bool originalFacesRight = true;

    private Vector2 pointerStartPosition;
    private RectTransform characterRect;

    private int currentPage;
    private float originalCharacterScaleX;

    private Coroutine pageCoroutine;
    private Coroutine characterCoroutine;

    private void Start()
    {
        characterRect = character.rectTransform;

        originalCharacterScaleX =
            Mathf.Abs(characterRect.localScale.x);

        int pageCount = characterStops.Length;

        currentPage = Mathf.RoundToInt(
            scrollRect.horizontalNormalizedPosition *
            (pageCount - 1)
        );

        currentPage = Mathf.Clamp(
            currentPage,
            0,
            pageCount - 1
        );

        // Đặt nhân vật vào đúng điểm của trang ban đầu.
        characterRect.anchoredPosition =
            characterStops[currentPage].anchoredPosition;

        PlayStand();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        pointerStartPosition = eventData.position;

        if (pageCoroutine != null)
        {
            StopCoroutine(pageCoroutine);
            pageCoroutine = null;
        }

        if (characterCoroutine != null)
        {
            StopCoroutine(characterCoroutine);
            characterCoroutine = null;
        }

        scrollRect.StopMovement();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float dragDistanceX =
            eventData.position.x - pointerStartPosition.x;

        int previousPage = currentPage;
        int targetPage = currentPage;

        if (Mathf.Abs(dragDistanceX) >= minimumSwipeDistance)
        {
            if (dragDistanceX < 0f)
            {
                // Vuốt trái: đi sang khúc bên phải.
                targetPage++;
            }
            else
            {
                // Vuốt phải: quay về khúc bên trái.
                targetPage--;
            }
        }

        targetPage = Mathf.Clamp(
            targetPage,
            0,
            characterStops.Length - 1
        );

        currentPage = targetPage;

        scrollRect.StopMovement();

        pageCoroutine = StartCoroutine(
            MovePageTo(targetPage)
        );

        if (targetPage != previousPage)
        {
            bool characterRunsRight =
                targetPage > previousPage;

            SetFacingDirection(characterRunsRight);

            characterCoroutine = StartCoroutine(
                MoveCharacterToPage(targetPage)
            );
        }
        else
        {
            PlayStand();
        }
    }

    private IEnumerator MovePageTo(int pageIndex)
    {
        float startPosition =
            scrollRect.horizontalNormalizedPosition;

        float targetPosition;

        if (characterStops.Length <= 1)
        {
            targetPosition = 0f;
        }
        else
        {
            targetPosition =
                pageIndex /
                (float)(characterStops.Length - 1);
        }

        float elapsed = 0f;

        while (elapsed < pageMoveDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / pageMoveDuration
            );

            float smoothT = SmoothStep(t);

            scrollRect.horizontalNormalizedPosition =
                Mathf.Lerp(
                    startPosition,
                    targetPosition,
                    smoothT
                );

            yield return null;
        }

        scrollRect.horizontalNormalizedPosition =
            targetPosition;

        pageCoroutine = null;
    }

    private IEnumerator MoveCharacterToPage(int pageIndex)
    {
        // Màn hình bắt đầu di chuyển trước một chút.
        yield return new WaitForSecondsRealtime(
            characterStartDelay
        );

        PlayRun();

        Vector2 startPosition =
            characterRect.anchoredPosition;

        Vector2 targetPosition =
            characterStops[pageIndex].anchoredPosition;

        float elapsed = 0f;

        while (elapsed < characterMoveDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / characterMoveDuration
            );

            float smoothT = SmoothStep(t);

            characterRect.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    targetPosition,
                    smoothT
                );

            yield return null;
        }

        characterRect.anchoredPosition = targetPosition;

        PlayStand();

        characterCoroutine = null;
    }

    private float SmoothStep(float value)
    {
        return value * value * (3f - 2f * value);
    }

    // Gọi sau khi nhân vật tự đi đến chỗ mới để cập nhật trang hiện tại.
    public void SyncCurrentPage()
    {
        int pageCount = characterStops.Length;
        currentPage = Mathf.Clamp(
            Mathf.RoundToInt(
                scrollRect.horizontalNormalizedPosition * (pageCount - 1)
            ),
            0,
            pageCount - 1
        );
    }

    private void PlayRun()
    {
        character.AnimationState.SetAnimation(
            0,
            runAnimation,
            true
        );
    }

    private void PlayStand()
    {
        FaceDefaultDirection();

        character.AnimationState.SetAnimation(
            0,
            standAnimation,
            true
        );
    }

    private void SetFacingDirection(bool faceRight)
    {
        Vector3 scale = characterRect.localScale;

        float originalDirection =
            originalFacesRight ? 1f : -1f;

        scale.x = originalCharacterScaleX *
                  (faceRight
                      ? originalDirection
                      : -originalDirection);

        characterRect.localScale = scale;
    }

    public void FaceDefaultDirection()
    {
        if (character == null)
            return;

        characterRect ??= character.rectTransform;
        if (originalCharacterScaleX <= 0f)
            originalCharacterScaleX = Mathf.Abs(characterRect.localScale.x);

        SetFacingDirection(true);
    }
}
