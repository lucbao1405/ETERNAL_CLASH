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
        targetPosition = FindNearestPage(currentPos);

        if (Mathf.Abs(targetPosition - currentPos) > 0.01f)
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
    }

    // Gọi sau khi nhân vật tự đi đến chỗ mới để hút về đúng trang.
    public void SyncToCurrentPage()
    {
        targetPosition = FindNearestPage(scrollRect.horizontalNormalizedPosition);
    }

    private float FindNearestPage(float currentPos)
    {
        if (pagePositions == null || pagePositions.Length != totalPages)
        {
            // Component có thể vừa được bật lại mà Start chưa chạy xong.
            pagePositions = new float[totalPages];
            for (int i = 0; i < totalPages; i++)
                pagePositions[i] = (float)i / (totalPages - 1);
        }

        float minDistance = float.MaxValue;
        float nearest = pagePositions[0];

        // So sánh xem lúc thả tay ra đang ở gần mốc trang nào nhất thì hút về trang đó
        for (int i = 0; i < totalPages; i++)
        {
            float distance = Mathf.Abs(currentPos - pagePositions[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = pagePositions[i];
            }
        }

        return nearest;
    }
}