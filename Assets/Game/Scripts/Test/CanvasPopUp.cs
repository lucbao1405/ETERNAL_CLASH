using UnityEngine;
using System.Collections;

public class CanvasPopUp : MonoBehaviour
{
    private RectTransform rect;
    private Vector2 targetPos;

    [SerializeField] private float jumpDistance = 30f;
    [SerializeField] private float duration = 0.2f;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        targetPos = rect.anchoredPosition;
    }

    public void PlayAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(JumpUp());
    }

    private IEnumerator JumpUp()
    {
        // Xuống dưới một chút
        Vector2 startPos = targetPos + Vector2.down * jumpDistance;
        rect.anchoredPosition = startPos;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;

            // Làm chuyển động mượt
            t = Mathf.SmoothStep(0f, 1f, t);

            rect.anchoredPosition =
                Vector2.Lerp(startPos, targetPos, t);

            yield return null;
        }

        rect.anchoredPosition = targetPos;
    }
}