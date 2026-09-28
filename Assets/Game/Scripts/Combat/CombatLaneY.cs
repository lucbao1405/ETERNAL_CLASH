using UnityEngine;

/// <summary>
/// Duong mat dat chung cua Player, quai va vat pham roi.
///
/// Can theo CHAN (mep duoi hinh anh), khong theo pivot: moi prefab co pivot khac
/// nhau (Player/Soi goc Spine o chan, Slime tu dua hinh vao giua, Goblin la sprite
/// pivot giua anh) nen dat cung transform.y van lam chan lech nhau.
/// </summary>
public static class CombatLaneY
{
    private static int cachedPlayerId;
    private static float cachedPlayerFeetOffset;

    public static float GetPlayerY(float fallback)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform.position.y : fallback;
    }

    public static Vector3 AlignToPlayerY(Vector3 position)
    {
        position.y = GetPlayerY(position.y);
        return position;
    }

    /// <summary>
    /// X ngoài ria phải màn hình để spawn quai: quai trôi vào theo world scroll
    /// thay vì hiện hình ngay tại chỗ giữa màn hình. Dùng camera; nếu không có
    /// camera thì lùi xa Player một khoảng an toàn.
    /// </summary>
    public static float GetOffScreenRightX(float extraMargin)
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            float halfWidth = cam.orthographicSize * cam.aspect;
            return cam.transform.position.x + halfWidth + extraMargin;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform.position.x + 18f + extraMargin : extraMargin;
    }

    /// <summary>
    /// Y cua duong mat dat = chan Player. False neu chua co Player hoac hinh Player
    /// chua duoc ve (Spine chi tao mesh sau frame dau tien).
    /// </summary>
    public static bool TryGetGroundY(out float groundY)
    {
        groundY = 0f;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return false;

        // Do 1 lan roi nho khoang cach goc -> chan: animation chay/danh lam mep duoi
        // nhap nho, do lai moi lan se lam duong mat dat rung theo.
        int id = player.GetInstanceID();
        if (cachedPlayerId != id)
        {
            if (!TryGetFeetY(player, out float feetY))
                return false;

            cachedPlayerId = id;
            cachedPlayerFeetOffset = player.transform.position.y - feetY;
        }

        groundY = player.transform.position.y - cachedPlayerFeetOffset;
        return true;
    }

    /// <summary>
    /// Mep duoi thap nhat cua cac hinh anh thuoc doi tuong. Bo qua particle, chu
    /// (thanh mau / so HP) va renderer chua co hinh.
    /// </summary>
    public static bool TryGetFeetY(GameObject target, out float feetY)
    {
        feetY = 0f;
        if (target == null)
            return false;

        bool found = false;
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
                continue;
            if (renderer.GetComponent<TMPro.TMP_Text>() != null)
                continue;
            // Bo qua bong dem (CharacterShadow/LootShadow tao con ten "Shadow" nam
            // DUOI chan nhan vat): neu tinh vao thi "chan" bi do thap hon that,
            // dan den keo nhan vat/quai DUNG CAO HON mat dat chung.
            if (renderer.transform.name == "Shadow")
                continue;

            Bounds bounds = renderer.bounds;
            if (bounds.size.y <= 0.0001f)
                continue;

            if (!found || bounds.min.y < feetY)
                feetY = bounds.min.y;
            found = true;
        }

        return found;
    }
}
