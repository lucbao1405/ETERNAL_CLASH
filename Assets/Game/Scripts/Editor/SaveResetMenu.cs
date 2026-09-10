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

            new PlayerPrefsSaveRepository().Delete();

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
            SaveData data = new PlayerPrefsSaveRepository().Load();
            if (data == null)
            {
                Debug.Log("[SAVE] Chua co save nao.");
                return;
            }

            Debug.Log("[SAVE] Noi dung hien tai:\n" + JsonUtility.ToJson(data));
        }

        // Chon Stage de test: StageManager doc SaveData.stageLevel moi lan vao Battle
        // de chon StageData (Data/Wave/Stage N). Thang man thi stageLevel tu tang,
        // toi Stage cuoi thi dung lai do - nen sau vai tran la luon choi Stage cuoi.
        [MenuItem("Tools/Eternal Clash/Choi Stage/Stage 1", false, 20)]
        private static void PlayStage1() => SetStageLevel(1);

        [MenuItem("Tools/Eternal Clash/Choi Stage/Stage 2", false, 21)]
        private static void PlayStage2() => SetStageLevel(2);

        [MenuItem("Tools/Eternal Clash/Choi Stage/Stage 3", false, 22)]
        private static void PlayStage3() => SetStageLevel(3);

        [MenuItem("Tools/Eternal Clash/Choi Stage/Stage 4", false, 23)]
        private static void PlayStage4() => SetStageLevel(4);

        /// <summary>
        /// Chi doi stageLevel, giu nguyen moi tien trinh khac (Level, vang, trang bi...).
        /// Co hieu luc tu lan vao Battle ke tiep.
        /// </summary>
        private static void SetStageLevel(int level)
        {
            if (Application.isPlaying && SaveManager.Instance != null && SaveManager.Instance.Data != null)
            {
                SaveManager.Instance.Data.stageLevel = level;
                SaveManager.Instance.Save();
            }
            else
            {
                var repository = new PlayerPrefsSaveRepository();
                SaveData data = repository.Load();
                if (data == null)
                {
                    Debug.Log("[SAVE] Chua co save nao - game moi luon bat dau tu Stage 1.");
                    return;
                }

                data.stageLevel = level;
                if (data.progress != null)
                    data.progress.stageLevel = level;
                repository.Save(data);
            }

            Debug.Log($"[SAVE] Lan vao Battle ke tiep se choi Stage {level}.");
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

            NewGameEquipmentDefaults.Apply(data);
            saveManager.Save();

            PlayerStatSystem.Instance?.LoadFromSave(data);
            GoldSystem.Instance?.LoadFromSave(data);
            BlacksmithCraftingSystem.Instance?.LoadFromSave(data);
            AlchemistUpgradeSystem.Instance?.LoadFromSave(data);
            AffinityManager.Instance?.LoadFromSave(data);
            EternalClash.Core.PlayerConditionSystem.Instance?.LoadFromSave(data);
            EquipmentSystem.Instance?.RefreshFromSave();
        }
    }
}
