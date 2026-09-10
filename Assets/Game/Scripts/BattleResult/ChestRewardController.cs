using EternalClash.UI;

namespace EternalClash.BattleResult
{
    public enum ChestState
    {
        Closed,
        Opening,
        Hold,
        Revealing,
        Complete
    }

    /// <summary>
    /// Compatibility component for existing Battle scene references.
    /// The implementation is Canvas-only and never instantiates a world chest.
    /// </summary>
    public sealed class ChestRewardController : ChestOpenPopupController
    {
    }
}
