using UnityEngine;
using System.Collections;

public class SmoothSlide : MonoBehaviour
{
    [Header("UI Cần Trượt")]
    public RectTransform panelRect;

    [Header("Tọa độ")]
    public Vector2 offScreenPos = new Vector2(0, 1500);
    public Vector2 onScreenPos = new Vector2(0, 0);

    [Header("Tốc độ")]
    public float moveSpeed = 4500f;

    [Header("Khoảng bật")]
    public float bounceDistance = 100f;

    private bool isOpen = false;
    private bool initialized;

    /// <summary>Bang dang mo (hoac dang truot vao). Dung cho nut Back Android.</summary>
    public bool IsOpen => isOpen && gameObject.activeInHierarchy;
    private bool activationRequested;
    private Coroutine slideCoroutine;

    void Awake()
    {
        if (!Initialize())
            return;

        // Panels that are active in the scene are treated as closed on startup.
        // TogglePanel initializes first when it is invoked on an inactive panel.
        if (gameObject.activeSelf && !activationRequested)
            gameObject.SetActive(false);
    }

    private bool Initialize()
    {
        if (initialized)
            return panelRect != null;

        ShopPanelAnimator animator = GetComponent<ShopPanelAnimator>();
        if (animator != null)
        {
            enabled = false;
            return false;
        }

        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        if (panelRect == null)
        {
            Debug.LogError("[SmoothSlide] Không tìm thấy Panel RectTransform.", this);
            enabled = false;
            return false;
        }

        isOpen = false;
        panelRect.anchoredPosition = offScreenPos;

        initialized = true;
        return true;
    }

    private void OnEnable()
    {
        PanelDim.Acquire(this);
    }

    private void OnDisable()
    {
        PanelDim.Release(this);
    }


    public void OpenPanel()
    {
        if (!gameObject.activeSelf)
            isOpen = false;

        if (!isOpen)
            TogglePanel();
    }


    public void ClosePanel()
    {
        if (!gameObject.activeSelf)
        {
            isOpen = false;
            if (panelRect != null)
                panelRect.anchoredPosition = offScreenPos;
            return;
        }

        if (isOpen)
            TogglePanel();
        else if (panelRect != null)
            panelRect.anchoredPosition = offScreenPos;
    }


    public void TogglePanel()
    {
        if (!Initialize())
            return;

        isOpen = !isOpen;
        EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.PanelScroll);

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        if (isOpen)
        {
            activationRequested = true;
            gameObject.SetActive(true);
            activationRequested = false;

            slideCoroutine = StartCoroutine(OpenPanelRoutine());
        }
        else
        {
            slideCoroutine = StartCoroutine(ClosePanelRoutine());
        }
    }

    IEnumerator OpenPanelRoutine()
    {
        // Vị trí thấp hơn vị trí chính
        Vector2 lowPosition = onScreenPos + new Vector2(0, -bounceDistance);

        // Giai đoạn 1:
        // Trượt từ ngoài màn hình -> vị trí thấp
        yield return MoveTo(lowPosition);

        // Giai đoạn 2:
        // Bật từ vị trí thấp -> vị trí chính
        yield return MoveTo(onScreenPos);
    }

    IEnumerator ClosePanelRoutine()
    {
        // Đóng: từ vị trí hiện tại -> ngoài màn hình
        yield return MoveTo(offScreenPos);
        gameObject.SetActive(false);
    }

    IEnumerator MoveTo(Vector2 targetPosition)
    {
        while (Vector2.Distance(panelRect.anchoredPosition, targetPosition) > 0.1f)
        {
            panelRect.anchoredPosition = Vector2.MoveTowards(
                panelRect.anchoredPosition,
                targetPosition,
                moveSpeed * Time.unscaledDeltaTime
            );

            yield return null;
        }

        panelRect.anchoredPosition = targetPosition;
    }
}
