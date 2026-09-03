using UnityEngine;

public class SmoothSlide : MonoBehaviour
{
    [Header("UI Cần Trượt")]
    public RectTransform panelRect; 
    
    [Header("Màn Tối (Tùy chọn)")]
    public CanvasGroup darkOverlay; // Thêm biến chứa lớp nền tối

    [Header("Tọa độ")]
    public Vector2 offScreenPos = new Vector2(0, 1500); 
    public Vector2 onScreenPos = new Vector2(0, 0);     

    [Header("Tốc độ trượt")]
    public float slideSpeed = 12f;

    private Vector2 targetPos;
    private bool isOpen = false;
    private float targetAlpha = 0f; // Mục tiêu độ mờ của nền tối

    void Start()
    {
        // Khởi tạo vị trí ban đầu ở ngoài màn hình
        panelRect.anchoredPosition = offScreenPos;
        targetPos = offScreenPos;
        
        // Khởi tạo nền tối tàng hình ban đầu
        if (darkOverlay != null)
        {
            darkOverlay.alpha = 0f;
            darkOverlay.blocksRaycasts = false; 
        }
    }

    void Update()
    {
        // 1. Trượt mượt bảng Setting
        panelRect.anchoredPosition = Vector2.Lerp(panelRect.anchoredPosition, targetPos, Time.deltaTime * slideSpeed);
        
        // 2. Mờ mượt nền tối
        if (darkOverlay != null)
        {
            darkOverlay.alpha = Mathf.Lerp(darkOverlay.alpha, targetAlpha, Time.deltaTime * slideSpeed);
        }
    }

    public void TogglePanel()
    {
        isOpen = !isOpen; 
        
        if (isOpen) 
        {
            // Lệnh MỞ
            targetPos = onScreenPos; 
            targetAlpha = 1f; // Hiện nền tối
            if (darkOverlay != null) darkOverlay.blocksRaycasts = true; // Chặn người chơi click xuyên qua nền đen
        }
        else 
        {
            // Lệnh ĐÓNG
            targetPos = offScreenPos; 
            targetAlpha = 0f; // Ẩn nền tối
            if (darkOverlay != null) darkOverlay.blocksRaycasts = false; // Mở lại tương tác cho game phía sau
        }
    }
}