using UnityEditor;
using EternalClash.EditorTools;

/// <summary>
/// Backwards-compatible menu entry for the battle result setup tool.
/// </summary>
public static class ChestFlowSetup
{
    [MenuItem("Tools/Battle Result/Setup Chest Flow")]
    public static void Setup()
    {
        BattleResultFlowSceneSetup.SetupFromMenu();
    }
}
