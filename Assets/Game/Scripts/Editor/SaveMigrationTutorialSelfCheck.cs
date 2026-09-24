#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using EternalClash.Core.Save;
using EternalClash.Tutorial;

namespace EternalClash.EditorTools
{
    /// <summary>
    /// Self check migration tutorial: save cu (version 0, truoc khi co tutorial)
    /// sau khi migrate phai van o buoc Introduction de intro + nhap ten hien len
    /// tren build mobile co save cu. Chay bang menu Tools/Eternal Clash/Save
    /// Migration Self Check. Khong dung test framework.
    /// </summary>
    internal static class SaveMigrationTutorialSelfCheck
    {
        [MenuItem("Tools/Eternal Clash/Save Migration Self Check")]
        private static void Run()
        {
            // Mo phong save cu tu build truoc: version 0, chua biet gi ve tutorial.
            SaveData oldSave = new SaveData();
            oldSave.version = 0;
            oldSave.gold = 120;
            oldSave.stageLevel = 3;

            SaveData migrated = SaveMigrationManager.Migrate(oldSave, out bool changed);

            Check(migrated.tutorialStep == (int)TutorialStep.Introduction && !migrated.tutorialInitialized,
                $"Save cu phai giu tutorial o buoc Introduction (dang: step={migrated.tutorialStep}, initialized={migrated.tutorialInitialized})");
            Check(migrated.gold == 120 && migrated.stageLevel == 3,
                "Migration phai giu nguyen tien trinh cu (gold, stageLevel)");
            Check(migrated.version == SaveVersion.CurrentVersion,
                $"Migration phai nang version len {SaveVersion.CurrentVersion} (dang: {migrated.version})");
            Check(changed, "Save cu phai duoc danh dau la da migrate");

            // Save moi tao trong code cung phai bat dau o Introduction.
            SaveData fresh = SaveMigrationManager.Migrate(new SaveData());
            Check(fresh.tutorialStep == (int)TutorialStep.Introduction && !fresh.tutorialInitialized,
                "Save moi phai bat dau o buoc Introduction");

            if (failures == 0)
                Debug.Log("[SaveMigrationSelfCheck] OK: save cu/save moi deu vao intro + nhap ten binh thuong.");
        }

        private static int failures;

        private static void Check(bool condition, string message)
        {
            if (!condition)
            {
                failures++;
                Debug.LogError("[SaveMigrationSelfCheck] FAIL: " + message);
            }
        }
    }
}
#endif
