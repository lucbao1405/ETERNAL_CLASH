namespace EternalClash.Animation
{
    /// <summary>
    /// Optional animation-layer feedback hooks.
    /// Combat / skill systems call these (null-safe) so the Spine animation
    /// layer can react without ever changing damage, timing or skill logic.
    /// </summary>
    public interface IPlayerAnimationFeedback
    {
        void NotifyAttack();
    }

    public interface IEnemyAnimationFeedback
    {
        void NotifyAttack();
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
