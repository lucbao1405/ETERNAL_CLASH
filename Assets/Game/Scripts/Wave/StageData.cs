using UnityEngine;

namespace EternalClash.Wave
{
    /// <summary>
    /// Cau hinh 1 man choi: danh sach WaveData theo dung thu tu se chay.
    /// Man mau trong yeu cau dung dung 4 phan tu (Wave 1 -> 4), nhung code khong hard-code
    /// so luong nay - het mang la tu dong "Win Stage".
    /// </summary>
    [CreateAssetMenu(fileName = "StageData", menuName = "Wave/StageData", order = -1)]
    public class StageData : ScriptableObject
    {
        public WaveData[] waves;
    }
}
