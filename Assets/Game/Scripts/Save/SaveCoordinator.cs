using UnityEngine;

namespace EternalClash.Core.Save
{
    /// <summary>
    /// Compatibility-safe save entry point for gameplay systems. SaveManager remains
    /// available to legacy callers while new code requests persistence here.
    /// </summary>
    public static class SaveCoordinator
    {
        public static void RequestSave()
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogWarning("[SAVE] Save requested before SaveManager was initialized.");
                return;
            }

            SaveManager.Instance.Save();
        }
    }
}
