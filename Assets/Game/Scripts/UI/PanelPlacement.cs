using UnityEngine;

/// <summary>
/// Panel duoc dat san TRONG man hinh trong editor: vi tri do la diem tra ve khi
/// mo, vi tri an = day ca panel len tren canh canvas. Panel van dat NGOAI man
/// hinh trong editor (cach dat cu) thi components giu thong so rieng nhu cu.
/// </summary>
public static class PanelPlacement
{
    public static bool TryDerive(RectTransform panel, out Vector2 shownPos, out Vector2 hiddenPos)
    {
        shownPos = hiddenPos = default(Vector2);

        if (panel == null)
            return false;

        RectTransform canvasRect =
            panel.GetComponentInParent<Canvas>()?.transform as RectTransform;
        if (canvasRect == null || !canvasRect.rect.Contains(
                canvasRect.InverseTransformPoint(panel.position)))
            return false;

        shownPos = panel.anchoredPosition;
        hiddenPos = new Vector2(
            shownPos.x,
            shownPos.y + (canvasRect.rect.height + panel.rect.height) * 0.5f + 100f);
        return true;
    }
}
