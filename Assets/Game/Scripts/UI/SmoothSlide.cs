using UnityEngine;
using System.Collections;

public class SmoothSlide : MonoBehaviour
{
    [Header("UI Cần Trượt")]
    public RectTransform panelRect;

    [Header("Màn Tối")]
    public CanvasGroup darkOverlay;

    [Header("Tọa độ")]
    public Vector2 offScreenPos = new Vector2(0, 1500);
    public Vector2 onScreenPos = new Vector2(0, 0);

    [Header("Tốc độ")]
    public float moveSpeed = 4500f;

    [Header("Khoảng bật")]
    public float bounceDistance = 100f;

    private bool isOpen = false;
    private Coroutine slideCoroutine;

    void Start()
    {
        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        if (panelRect == null)
        {
            Debug.LogError("[SmoothSlide] Không tìm thấy Panel RectTransform.", this);
            enabled = false;
            return;
        }

        panelRect.anchoredPosition = offScreenPos;

        if (darkOverlay != null)
        {
            darkOverlay.alpha = 0f;
            darkOverlay.blocksRaycasts = false;
        }
    }

    public void TogglePanel()
    {
        isOpen = !isOpen;

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        if (isOpen)
        {
            if (darkOverlay != null)
            {
                darkOverlay.alpha = 1f;
                darkOverlay.blocksRaycasts = true;
            }

            slideCoroutine = StartCoroutine(OpenPanel());
        }
        else
        {
            if (darkOverlay != null)
            {
                darkOverlay.alpha = 0f;
                darkOverlay.blocksRaycasts = false;
            }

            slideCoroutine = StartCoroutine(ClosePanel());
        }
    }

    IEnumerator OpenPanel()
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

    IEnumerator ClosePanel()
    {
        // Đóng: từ vị trí hiện tại -> ngoài màn hình
        yield return MoveTo(offScreenPos);
    }

    IEnumerator MoveTo(Vector2 targetPosition)
    {
        while (Vector2.Distance(panelRect.anchoredPosition, targetPosition) > 0.1f)
        {
            panelRect.anchoredPosition = Vector2.MoveTowards(
                panelRect.anchoredPosition,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        panelRect.anchoredPosition = targetPosition;
    }
}