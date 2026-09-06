using UnityEditor;
using UnityEngine;
using EternalClash.Core.Save;
using EternalClash.Village;

namespace EternalClash.EditorTools
{
    /// <summary>
    /// Menu Editor de xoa save va dua game ve trang thai nguoi choi moi.
    ///
    /// Save cua game nam trong PlayerPrefs voi khoa "SAVE_DATA" (xem SaveManager),
    /// nen xoa khoa do la reset toan bo tien trinh: Level, EXP, diem stat, vang,
    /// nguyen lieu, tier trang bi, thien cam, stage da qua.
    /// </summary>
    public static class SaveResetMenu
    {
        private const string SaveKey = "SAVE_DATA";

        [MenuItem("Tools/Eternal Clash/Xoa Save (choi lai tu dau)", false, 1)]
        private static void ClearSave()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Xoa toan bo tien trinh?",
                "Se xoa Level, EXP, diem stat, vang, nguyen lieu, trang bi va stage da qua.\n\n" +
                "Khong the hoan tac.",
                "Xoa",
                "Huy");

            if (!confirmed)
                return;

            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();

            // Neu dang chay game, cac he thong deu la DontDestroyOnLoad va van giu
            // gia tri cu trong bo nho - xoa moi PlayerPrefs thoi thi lan thay doi
            // ke tiep se ghi de nguoc lai. Phai nap lai du lieu rong vao chung.
            if (Application.isPlaying)
                ResetRuntimeSystems();

            Debug.Log("[SAVE] Da xoa save. " +
                      (Application.isPlaying
                          ? "Cac he thong dang chay da duoc dat lai."
                          : "Bam Play de bat dau tu dau."));
        }

        [MenuItem("Tools/Eternal Clash/Xem save hien tai", false, 2)]
        private static void ShowSave()
        {
            string json = PlayerPrefs.GetString(SaveKey, "");

            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("[SAVE] Chua co save nao.");
                return;
            }

            Debug.Log("[SAVE] Noi dung hien tai:\n" + json);
        }

        /// <summary>
        /// Nap SaveData rong vao cac he thong dang song, theo dung thu tu ma
        /// GameBootstrap.LoadSavedData() dang dung.
        /// </summary>
        private static void ResetRuntimeSystems()
        {
            SaveManager saveManager = SaveManager.Instance;
            if (saveManager == null)
            {
                Debug.LogWarning("[SAVE] Khong tim thay SaveManager dang chay.");
                return;
            }

            // DeleteSave() tu tao SaveData moi ben trong SaveManager.
            saveManager.DeleteSave();

            SaveData data = saveManager.Data;
            if (data == null)
                return;

            PlayerStatSystem.Instance?.LoadFromSave(data);
            GoldSystem.Instance?.LoadFromSave(data);
            BlacksmithCraftingSystem.Instance?.LoadFromSave(data);
            AffinityManager.Instance?.LoadFromSave(data);
            EternalClash.Core.PlayerConditionSystem.Instance?.LoadFromSave(data);
            EquipmentSystem.Instance?.RefreshFromSave();
        }
    }
}
