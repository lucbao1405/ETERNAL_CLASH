using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SwipePage : MonoBehaviour, IEndDragHandler, IBeginDragHandler
{
    public ScrollRect scrollRect;
    
    // Tổng số trang bác muốn (Ở đây là 3: Trái, Giữa, Phải)
    public int totalPages = 3; 
    
    private float[] pagePositions;
    private float targetPosition;
    private bool isDragging;

    void Start()
    {
        pagePositions = new float[totalPages];
        
        // Tự động tính toán các mốc dựa trên số trang (0.0, 0.5, 1.0)
        for (int i = 0; i < totalPages; i++)
        {
            pagePositions[i] = (float)i / (totalPages - 1);
        }
        
        // Khởi đầu game ở trang Giữa (index 1)
        targetPosition = pagePositions[1];
        scrollRect.horizontalNormalizedPosition = targetPosition;
    }

    void Update()
    {
        // Khi không chạm tay, tự động trượt mượt mà (hút) về trang mục tiêu
        if (!isDragging)
        {
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(
                scrollRect.horizontalNormalizedPosition, 
                targetPosition, 
                Time.deltaTime * 10f);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        
        // Đo vị trí hiện tại lúc thả tay ra
        float currentPos = scrollRect.horizontalNormalizedPosition;
        float minDistance = float.MaxValue;

        // So sánh xem lúc thả tay ra đang ở gần mốc trang nào nhất thì hút về trang đó
        for (int i = 0; i < totalPages; i++)
        {
            float distance = Mathf.Abs(currentPos - pagePositions[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                targetPosition = pagePositions[i];
            }
        }
    }
}