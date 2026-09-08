using UnityEngine;

public class UICloudLoop : MonoBehaviour
{
    [Header("Hai dải mây giống nhau")]
    [SerializeField] private RectTransform cloud1;
    [SerializeField] private RectTransform cloud2;

    [Header("Tốc độ trôi")]
    [SerializeField] private float speed = 25f;

    private float cloudWidth;
    private float startX;

    private void Start()
    {
        Canvas.ForceUpdateCanvases();

        cloudWidth = cloud1.rect.width;
        startX = cloud1.anchoredPosition.x;

        // Đặt cloud2 nối ngay sau cloud1
        Vector2 position = cloud2.anchoredPosition;
        position.x = startX + cloudWidth;
        position.y = cloud1.anchoredPosition.y;
        cloud2.anchoredPosition = position;
    }

    private void Update()
    {
        float distance = speed * Time.unscaledDeltaTime;

        MoveLeft(cloud1, distance);
        MoveLeft(cloud2, distance);

        // Dải nào đi hết bên trái thì đưa ra sau dải còn lại
        if (cloud1.anchoredPosition.x <= startX - cloudWidth)
        {
            SetPositionX(
                cloud1,
                cloud2.anchoredPosition.x + cloudWidth
            );
        }

        if (cloud2.anchoredPosition.x <= startX - cloudWidth)
        {
            SetPositionX(
                cloud2,
                cloud1.anchoredPosition.x + cloudWidth
            );
        }
    }

    private void MoveLeft(RectTransform cloud, float distance)
    {
        Vector2 position = cloud.anchoredPosition;
        position.x -= distance;
        cloud.anchoredPosition = position;
    }

    private void SetPositionX(RectTransform cloud, float x)
    {
        Vector2 position = cloud.anchoredPosition;
        position.x = x;
        cloud.anchoredPosition = position;
    }
}