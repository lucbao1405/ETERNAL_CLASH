namespace EternalClash.Animation
{
    /// <summary>
    /// Optional animation-layer feedback hooks.
    /// Combat / skill systems call these (null-safe) so the Spine animation
    /// layer can react without ever changing damage, timing or skill logic.
    /// </summary>
    public interface IPlayerAnimationFeedback
    {
        /// <summary>speedScale &gt; 1 thi clip chay nhanh len (danh thanh tho hon).</summary>
        void NotifyAttack(float speedScale = 1f);

        /// <summary>Do dai clip attack (giay), 0 neu khong ro. Combat dung de dinh thoi diem gay dmg.</summary>
        float GetAttackDuration();
    }

    public interface IEnemyAnimationFeedback
    {
        void NotifyAttack();

        /// <summary>Do dai clip attack (giay), 0 neu khong ro. Combat dung de dinh thoi diem gay dmg.</summary>
        float GetAttackDuration();
    }

    /// <summary>
    /// Optional hook so generic enemy death handling can play a death
    /// animation and delay destruction until it finished.
    /// </summary>
    public interface IEnemyDeathVisual
    {
        /// <summary>Starts the death animation and returns its duration in seconds (0 = unknown).</summary>
        float PlayDeath();
    }
}
