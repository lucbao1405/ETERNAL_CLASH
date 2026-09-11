using System;
using EternalClash.Core;
using UnityEngine;

namespace EternalClash.Audio
{
    /// <summary>
    /// Diem goi am thanh duy nhat cho code gameplay. Khong co AudioController
    /// (vd dang test scene le) thi im lang bo qua, khong bao loi.
    /// </summary>
    public static class GameAudio
    {
        public static void Play(SoundId id)
        {
            AudioController.Instance?.Play(id);
        }

        public static void PlayEnemy(GameObject enemy, EnemySound type)
        {
            AudioController.Instance?.PlayEnemy(enemy, type);
        }

        /// <summary>Am thanh khi skill cua Player duoc kich hoat (theo SkillBase.skillName).</summary>
        public static void PlaySkill(string skillName)
        {
            if (string.Equals(skillName, "Charge", StringComparison.OrdinalIgnoreCase))
                Play(SoundId.PlayerCharge);
            else if (string.Equals(skillName, "Shield", StringComparison.OrdinalIgnoreCase))
                Play(SoundId.PlayerShieldUp);
            else if (string.Equals(skillName, "Potion", StringComparison.OrdinalIgnoreCase))
                Play(SoundId.PlayerPotion);
        }
    }
}
