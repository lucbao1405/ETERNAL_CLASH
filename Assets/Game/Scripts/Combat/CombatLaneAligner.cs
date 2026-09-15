using UnityEngine;

/// <summary>
/// Gan tu dong vao quai (EnemyMover). Cho toi khi hinh anh cua quai va Player da
/// duoc ve, roi dich quai len/xuong de chan cham dung duong mat dat chung.
/// </summary>
[DisallowMultipleComponent]
public sealed class CombatLaneAligner : MonoBehaviour
{
    // Spine can 1-2 frame de tao mesh; cho toi da ~0.5s roi bo cuoc.
    private const int MaxWaitFrames = 30;

    private System.Collections.IEnumerator Start()
    {
        for (int i = 0; i < MaxWaitFrames; i++)
        {
            yield return null;
            if (TryAlign())
                break;
        }

        Destroy(this);
    }

    private bool TryAlign()
    {
        if (!CombatLaneY.TryGetGroundY(out float groundY))
            return false;
        if (!CombatLaneY.TryGetFeetY(gameObject, out float feetY))
            return false;

        float newY = transform.position.y + (groundY - feetY);

        var mover = GetComponent<EternalClash.Enemy.EnemyMover>();
        if (mover != null)
            mover.SetLaneY(newY);
        else
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        return true;
    }
}
