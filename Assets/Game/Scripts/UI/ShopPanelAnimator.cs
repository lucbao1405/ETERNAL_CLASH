using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopPanelAnimator : MonoBehaviour
{
    public enum PanelState { Closed, Opening, Opened, Closing }

    [Header("Inspector References")]
    [SerializeField] private RectTransform shopPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float closedY = 2000f;
    [SerializeField] private float openedY = 250f;
    [SerializeField, Min(0.01f)] private float duration = 0.4f;

    public PanelState State { get; private set; } = PanelState.Closed;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        shopPanel ??= GetComponent<RectTransform>();
        closeButton ??= FindCloseButton();
        canvasGroup ??= GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        CloseImmediate();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    private void OnEnable()
    {
        PanelDim.Acquire(this);
    }

    private void OnDisable()
    {
        PanelDim.Release(this);
    }

    public void Open()
    {
        if (shopPanel == null)
            return;

        gameObject.SetActive(true);
        // Awake closes a panel that starts active in the scene. When Open is what
        // activated it, enable it once more after that initialization pass.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        SetRaycastState(true);
        StartSlide(PanelState.Opening, openedY, PanelState.Opened, false);
    }

    public void Close()
    {
        if (shopPanel == null || State == PanelState.Closed || State == PanelState.Closing)
            return;

        EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
        StartSlide(PanelState.Closing, closedY, PanelState.Closed, true);
    }

    public void SlideDown() => Open();
    public void SlideUp() => Close();

    public void CloseImmediate()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        animationCoroutine = null;
        if (shopPanel != null)
            shopPanel.anchoredPosition = new Vector2(shopPanel.anchoredPosition.x, closedY);
        State = PanelState.Closed;
        SetRaycastState(false);
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void StartSlide(PanelState movingState, float destinationY, PanelState finalState, bool deactivateAtEnd)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(Slide(movingState, destinationY, finalState, deactivateAtEnd));
    }

    private IEnumerator Slide(PanelState movingState, float destinationY, PanelState finalState, bool deactivateAtEnd)
    {
        State = movingState;
        Vector2 start = shopPanel.anchoredPosition;
        Vector2 destination = new Vector2(start.x, destinationY);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            shopPanel.anchoredPosition = Vector2.LerpUnclamped(start, destination, eased);
            yield return null;
        }

        shopPanel.anchoredPosition = destination;
        animationCoroutine = null;
        State = finalState;
        if (deactivateAtEnd)
        {
            SetRaycastState(false);
            gameObject.SetActive(false);

            SwipePageCharacterTravel player = FindObjectOfType<SwipePageCharacterTravel>();
            if (player != null && !IsCharacterNavigating())
                player.FaceDefaultDirection();
        }
    }

    private static bool IsCharacterNavigating()
    {
        var navigation = FindObjectOfType<EternalClash.UI.TownNavigationController>();
        return navigation != null && navigation.IsMoving;
    }

    private void SetRaycastState(bool isOpen)
    {
        canvasGroup ??= GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = isOpen ? 1f : 0f;
        canvasGroup.interactable = isOpen;
        canvasGroup.blocksRaycasts = isOpen;
    }

    private Button FindCloseButton()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            string buttonName = button.name.ToLowerInvariant();
            if (buttonName.Contains("close") || buttonName == "x")
                return button;
        }
        return null;
    }
}
