using UnityEngine;

/// <summary>
/// Provides the vertical combat lane used by Player, enemies and dropped items.
/// </summary>
public static class CombatLaneY
{
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
}
