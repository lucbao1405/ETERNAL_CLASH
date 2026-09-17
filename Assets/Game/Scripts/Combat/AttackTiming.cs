using UnityEngine;

namespace EternalClash.Combat
{
    /// <summary>
    /// Thoi diem gay dmg dung chung cho player va enemy:
    /// o giua animation attack thay vi ngay khi bat dau state.
    /// </summary>
    public static class AttackTiming
    {
        /// <param name="clipDuration">Do dai clip attack (giay), 0 neu khong ro.</param>
        /// <param name="hitMoment">Moc gay dmg trong clip (0.5 = giua state attack).</param>
        public static float HitDelay(float clipDuration, float hitMoment)
        {
            return clipDuration > 0f ? Mathf.Max(0.05f, clipDuration * hitMoment) : 0.3f;
        }
    }
}
