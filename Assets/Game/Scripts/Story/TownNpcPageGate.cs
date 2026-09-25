using UnityEngine;

/// <summary>
/// Chan tuong tac NPC qua xa: panel cua mot NPC (tho ren, phu thuy, Ela...)
/// chi duoc mo khi nhan vat dang dung tren cung mot Page (con duong) voi NPC do.
/// Moi con duong la mot "Page_N" con trai duoi Content cua Town; NPC khong nam
/// trong Page nao hoac khong tim thay nhan vat thi khong bi chan (giu hanh vi cu).
/// </summary>
public static class TownNpcPageGate
{
    private const string CharacterName = "Nhan_Vat_Chinh";
    private const float FallbackPageHalfWidth = 540f;

    private static Transform cachedCharacter;

    /// <summary>
    /// True khi panel cua NPC nay duoc phep mo: phai xac dinh duoc ca Page cua
    /// NPC va vi tri nhan vat, va nhan vat phai nam trong pham vi Page do.
    /// </summary>
    public static bool CanOpenNpcPanel(Component npc)
    {
        if (npc == null)
            return true;

        Transform page = FindNpcPage(npc.transform);
        if (page == null)
            return true;

        Transform content = page.parent;
        Transform character = ResolveCharacter(content);
        if (content == null || character == null)
            return true;

        float characterX = content.InverseTransformPoint(character.position).x;
        float pageX = content.InverseTransformPoint(page.position).x;
        float halfWidth = page is RectTransform pageRect && pageRect.rect.width > 0f
            ? pageRect.rect.width * 0.5f
            : FallbackPageHalfWidth;

        return Mathf.Abs(characterX - pageX) <= halfWidth;
    }

    private static Transform FindNpcPage(Transform node)
    {
        while (node != null)
        {
            if (node.name.StartsWith("Page_"))
                return node;
            node = node.parent;
        }
        return null;
    }

    private static Transform ResolveCharacter(Transform content)
    {
        if (cachedCharacter != null)
            return cachedCharacter;

        Transform found = content != null ? content.Find(CharacterName) : null;
        if (found == null && content != null)
            found = FindDeepChild(content, CharacterName);
        if (found != null)
        {
            cachedCharacter = found;
            return found;
        }

        SwipePageCharacterTravel travel = Object.FindObjectOfType<SwipePageCharacterTravel>();
        if (travel != null && travel.Character != null)
            cachedCharacter = travel.Character.transform;

        return cachedCharacter;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }
}
