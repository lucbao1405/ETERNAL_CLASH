using UnityEngine;

/// <summary>
/// Gan tu dong vao quai (EnemyMover). Cho toi khi hinh anh cua quai va Player da
/// duoc ve, roi dich quai len/xuong de chan cham dung duong mat dat chung.
///
/// Canh LIEN TUC cho toi khi bounds cua hinh anh on dinh (Spine animation xuat
/// hien/spawn lam bounds thay doi trong vai frame dau — neu chi canh 1 lan thi
/// boss bi khoa Y o do cao sai ngay tu khi xuat hien). Sau do tu huy.
/// </summary>
[DisallowMultipleComponent]
public sealed class CombatLaneAligner : MonoBehaviour
{
    // Spine can 1-2 frame de tao mesh; cho toi da 3s roi bo cuoc.
    private const float MaxAlignTime = 3f;
    // Bounds phai on dinh lien tuc trong khoang thoi gian nay moi ket luan "da dung".
    private const float StableTime = 0.5f;
    // Bien doi feetY nho hon nay coi nhu khong doi.
    private const float StableDelta = 0.005f;

    private float alignedFeetY = float.MinValue;
    private float stableElapsed;
    private float elapsed;

    private void LateUpdate()
    {
        elapsed += Time.deltaTime;
        if (elapsed > MaxAlignTime)
        {
            Destroy(this);
            return;
        }

        if (!CombatLaneY.TryGetGroundY(out float groundY))
            return;
        if (!CombatLaneY.TryGetFeetY(gameObject, out float feetY))
            return;

        float newY = transform.position.y + (groundY - feetY);

        var mover = GetComponent<EternalClash.Enemy.EnemyMover>();
        if (mover != null)
            mover.SetLaneY(newY);
        else
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Da canh it nhat 1 lan va bounds khong con doi -> da dung, tu huy.
        if (alignedFeetY != float.MinValue && Mathf.Abs(feetY - alignedFeetY) < StableDelta)
        {
            stableElapsed += Time.deltaTime;
            if (stableElapsed >= StableTime)
                Destroy(this);
        }
        else
        {
            stableElapsed = 0f;
        }
        alignedFeetY = feetY;
    }
}
