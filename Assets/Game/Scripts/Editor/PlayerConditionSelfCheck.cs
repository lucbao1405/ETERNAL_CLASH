#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using EternalClash.Core;

namespace EternalClash.EditorTools
{
    /// <summary>
    /// Self-check cho co "Injured do THUA tran" (dieu khien luong hoi mau bang
    /// kim cuong chi hien khi thua). Chay bang menu Tools/Player Condition/Self Check.
    /// Khong dung test framework, khong bien dich vao build.
    /// </summary>
    internal static class PlayerConditionSelfCheck
    {
        private static int failures;

        [MenuItem("Tools/Player Condition/Self Check")]
        private static void Run()
        {
            failures = 0;
            object originalInstance = PlayerConditionSystem.Instance;
            GameObject host = null;

            try
            {
                host = new GameObject("PlayerConditionSelfCheckHost");
                PlayerConditionSystem cond = host.AddComponent<PlayerConditionSystem>();

                // Thua: MarkInjured -> Injured + co thua bat.
                cond.MarkInjured(100);
                Check(cond.IsInjured, "MarkInjured phai dat trang thai Injured");
                Check(cond.InjuredByDefeat, "MarkInjured (thua) phai bat co InjuredByDefeat");

                // Thang: con thieu mau -> van Injured nhung co thua PHAI tat
                // (khong hien luong hoi bang kim cuong).
                cond.SetHpAfterBattle(50, 100);
                Check(cond.IsInjured, "Thang ma con thieu mau thi van dang hoi (Injured)");
                Check(!cond.InjuredByDefeat, "SetHpAfterBattle (thang) phai tat co InjuredByDefeat");

                // Thang: day mau -> binh thuong.
                cond.MarkInjured(100);
                cond.SetHpAfterBattle(100, 100);
                Check(!cond.IsInjured && !cond.InjuredByDefeat, "Thang day mau phai ve Normal va tat co");

                // Save roundtrip: thua roi thoat app -> LoadFromSave giu nguyen co.
                var saveDefeat = new SaveData
                {
                    playerCondition = (int)PlayerCondition.Injured,
                    currentHp = 10,
                    maxHp = 100,
                    recoveryRatePerSecond = 5f,
                    recoveryTargetPercent = 80,
                    injuredByDefeat = true
                };
                cond.LoadFromSave(saveDefeat);
                Check(cond.IsInjured && cond.InjuredByDefeat,
                    "LoadFromSave giu nguyen co thua khi van Injured");

                var saveWin = new SaveData
                {
                    playerCondition = (int)PlayerCondition.Injured,
                    currentHp = 50,
                    maxHp = 100,
                    recoveryRatePerSecond = 5f,
                    recoveryTargetPercent = 80,
                    injuredByDefeat = false
                };
                cond.LoadFromSave(saveWin);
                Check(cond.IsInjured && !cond.InjuredByDefeat,
                    "LoadFromSave khong bat co khi injured do thang");
            }
            catch (System.Exception exception)
            {
                failures++;
                Debug.LogError("[PlayerConditionSelfCheck] Loi giua chung: " + exception);
            }
            finally
            {
                if (host != null)
                    Object.DestroyImmediate(host);

                // Tra lai Instance cu de khong o nhiem singleton cho tool khac.
                typeof(PlayerConditionSystem).GetProperty("Instance")
                    ?.SetValue(null, originalInstance);
            }

            Debug.Log(failures == 0
                ? "[PlayerConditionSelfCheck] PASS"
                : $"[PlayerConditionSelfCheck] FAIL x{failures}");
        }

        private static void Check(bool ok, string message)
        {
            if (ok)
                return;

            failures++;
            Debug.LogError($"[PlayerConditionSelfCheck] {message}");
        }
    }
}
#endif
