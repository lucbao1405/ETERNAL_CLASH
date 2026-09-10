using UnityEditor;
using EternalClash.EditorTools;

/// <summary>
/// Legacy alias kept so existing editor automation can still invoke SetupChestFlow.Setup.
/// </summary>
public static class SetupChestFlow
{
    [MenuItem("Tools/Battle Result/Setup Chest Flow (Legacy)")]
    public static void Setup()
    {
        BattleResultFlowSceneSetup.SetupFromMenu();
    }
}
