using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class StatHoldTooltip : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [Header("Nội dung chỉ số")]
    [SerializeField] private string statName;

    [TextArea(2, 4)]
    [SerializeField] private string description;

    [Header("Thời gian giữ")]
    [SerializeField] private float holdDuration = 0.5f;

    [Header("Tooltip dùng chung")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipTitle;
    [SerializeField] private TMP_Text tooltipDescription;

    private bool isHolding;
    private bool tooltipShown;
    private float pressStartTime;

    private void Update()
    {
        if (!isHolding || tooltipShown)
            return;

        float heldTime = Time.unscaledTime - pressStartTime;

        if (heldTime >= holdDuration)
        {
            ShowTooltip();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        tooltipShown = false;
        pressStartTime = Time.unscaledTime;

        // Đóng Tooltip cũ nếu đang mở
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopHolding();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Kéo ngón tay ra khỏi chỉ số thì hủy
        StopHolding();
    }

    private void ShowTooltip()
    {
        isHolding = false;
        tooltipShown = true;

        tooltipTitle.text = statName;
        tooltipDescription.text = description;

        tooltipPanel.SetActive(true);

        // Đảm bảo Tooltip nằm trên những UI khác
        tooltipPanel.transform.SetAsLastSibling();
    }

    private void StopHolding()
    {
        isHolding = false;
        tooltipShown = false;

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    private void OnDisable()
    {
        StopHolding();
    }
}