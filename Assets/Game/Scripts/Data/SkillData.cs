using System;
using UnityEngine;

namespace EternalClash.Data
{
    public enum SkillBranchType
    {
        Charge,
        Shield,
        Potion,
        Passive,
        Special
    }

    public enum SpecialEffectType
    {
        None,
        DamageBonus,        // Tăng % hoặc cộng thẳng sát thương
        Stun,               // Làm choáng kẻ địch
        Knockback,          // Đẩy lùi kẻ địch
        DamageReduction,    // Tăng % giảm sát thương nhận vào
        ReflectDamage,      // Phản sát thương lại kẻ tấn công
        Invulnerable,       // Bất tử / Hyper Armor / Kháng hiệu ứng khống chế
        HealOnBlock,        // Hồi máu khi đỡ đòn thành công
        Shockwave,          // Phát ra sóng chấn động diện rộng
        CooldownReduction,  // Giảm thời gian hồi chiêu
        SpeedBoost          // Tăng tốc độ di chuyển
    }

    /// <summary>
    /// ScriptableObject lưu trữ toàn bộ dữ liệu của 1 kỹ năng:
    /// Tên, Icon hình ảnh, Nhánh kỹ năng, Mô tả, và Hiệu ứng đặc biệt.
    /// Có thể tạo nhanh trong Unity bằng cách: Chuột phải -> Create -> Eternal Clash -> Skill Data
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "NewSkillData", menuName = "Eternal Clash/Skill Data", order = 1)]
    public class SkillData : ScriptableObject
    {
        [Header("--- Thông Tin Cơ Bản (Basic Info) ---")]
        [Tooltip("Mã định danh kỹ năng, ví dụ: charge_01, shield_02")]
        public string skillId;

        [Tooltip("Tên kỹ năng")]
        public string skillName;

        [Tooltip("Nhánh kỹ năng (Charge / Shield /...)")]
        public SkillBranchType branchType = SkillBranchType.Charge;

        [Tooltip("Hình ảnh / Icon của chiêu")]
        public Sprite icon;

        [Range(1, 10)]
        [Tooltip("Cấp bậc kỹ năng: Tier 1 là thấp nhất, Tier càng cao kỹ năng càng mạnh")]
        public int tier = 1;

        [Header("--- Trạng Thái Mở Khóa (Unlock Status) ---")]
        [Tooltip("Kỹ năng này có mặc định mở khóa không? (2 skill đầu tiên mặc định luôn mở)")]
        public bool isDefaultUnlocked = false;

        [Tooltip("Mô tả điều kiện để mở khóa (hiển thị khi skill bị khóa)")]
        public string unlockRequirement = "Mở khóa khi đạt cấp độ cao hơn.";

        [Header("--- Mô Tả Chiêu Thức (Descriptions) ---")]
        [TextArea(2, 5)]
        [Tooltip("Mô tả cách thức hoạt động của chiêu thức")]
        public string description;

        [TextArea(2, 4)]
        [Tooltip("Mô tả hiệu ứng đặc biệt (ví dụ: 'Làm choáng 1.5s', 'Phản 15 DMG', 'Hồi 5% HP',...)")]
        public string specialEffectDescription;

        [Header("--- Chi Tiết Hiệu Ứng Đặc Biệt (Special Effect) ---")]
        [Tooltip("Loại hiệu ứng đặc biệt")]
        public SpecialEffectType specialEffectType = SpecialEffectType.None;

        [Tooltip("Chỉ số hiệu ứng (ví dụ: 15 là 15 DMG, 0.25 là 25% dmg, 1.5 là 1.5s stun,...)")]
        public float effectValue;

        [Tooltip("Thời gian duy trì hiệu ứng (tính bằng giây)")]
        public float effectDuration;

        [Header("--- Chỉ Số Bổ Sung (Combat Modifiers) ---")]
        [Tooltip("Sát thương cộng thêm")]
        public int bonusDamage;

        [Tooltip("Thời gian hồi chiêu riêng (nếu để 0 sẽ dùng mặc định)")]
        public float customCooldown;

        [Header("--- Hiệu Ứng Hình Ảnh & Âm Thanh (FX & Sound) ---")]
        [Tooltip("Prefab hiệu ứng hạt VFX khi kích hoạt kỹ năng")]
        public GameObject vfxPrefab;

        [Tooltip("Âm thanh SFX khi sử dụng kỹ năng")]
        public AudioClip sfxAudio;

        /// <summary>
        /// Trả về mô tả chi tiết hoàn chỉnh kết hợp cả mô tả và hiệu ứng đặc biệt để hiển thị lên UI.
        /// </summary>
        public string GetFullDescription()
        {
            if (string.IsNullOrEmpty(specialEffectDescription))
                return description;

            if (string.IsNullOrEmpty(description))
                return specialEffectDescription;

            return $"{description}\n\n<color=#FFD700>★ Hiệu ứng đặc biệt:</color> {specialEffectDescription}";
        }

        /// <summary>
        /// Kiểm tra xem kỹ năng có mở khóa không.
        /// 2 kỹ năng đầu tiên (slotIndex = 0 hoặc 1) mặc định luôn luôn mở khóa.
        /// </summary>
        public bool IsUnlocked(int slotIndex = 0)
        {
            if (slotIndex < 2) return true;
            if (isDefaultUnlocked) return true;
            if (string.IsNullOrEmpty(skillId)) return false;
            return PlayerPrefs.GetInt("SKILL_UNLOCKED_" + skillId, 0) == 1;
        }

        public void SetUnlocked(bool unlocked)
        {
            if (string.IsNullOrEmpty(skillId)) return;
            PlayerPrefs.SetInt("SKILL_UNLOCKED_" + skillId, unlocked ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
