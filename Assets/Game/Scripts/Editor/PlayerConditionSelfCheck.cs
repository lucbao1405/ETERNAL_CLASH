#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using EternalClash.Core;

namespace EternalClash.EditorTools
{
    /// <summary>
    /// Self-check cho nguong offer hoi mau bang kim cuong (duoi 60% Max HP, dung
    /// chung voi nguong chan vao tran). Chay bang menu Tools/Player Condition/Self
    /// Check. Khong dung test framework, khong bien dich vao build.
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

                // Thua: ve lang 10% Max HP -> duoi 60% -> duoc offer kim cuong.
                cond.MarkInjured(100);
                Check(cond.CanOfferGemHeal, "HP 10% (thua) phai duoc offer kim cuong");

                // Thang: ve lang 70% -> tren nguong -> khong offer.
                cond.SetHpAfterBattle(70, 100);
                Check(!cond.CanOfferGemHeal, "HP 70% (thang) khong duoc offer kim cuong");

                // Thang: ve lang 50% -> duoi nguong -> duoc offer.
                cond.SetHpAfterBattle(50, 100);
                Check(cond.CanOfferGemHeal, "HP 50% (thang) phai duoc offer kim cuong");

                // Du 100% -> khong offer.
                cond.RestoreFullHp();
                Check(!cond.CanOfferGemHeal, "HP day khong duoc offer kim cuong");

                // Save roundtrip: thua roi thoat app, mo lai van con duoi nguong.
                var save = new SaveData
                {
                    playerCondition = (int)PlayerCondition.Injured,
                    currentHp = 10,
                    maxHp = 100,
                    recoveryRatePerSecond = 5f,
                    recoveryTargetPercent = 80
                };
                cond.LoadFromSave(save);
                Check(cond.IsInjured && cond.CanOfferGemHeal,
                    "LoadFromSave giu nguyen luong offer (HP 10% sau thua)");
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
