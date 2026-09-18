#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Monetization
{
    /// <summary>
    /// Self-check assert cho logic monetization (chay bang menu
    /// Tools > Monetization > Self Check). Khong dung test framework.
    /// Tuyet doi khong bien dich vao build: toan bo nam trong #if UNITY_EDITOR.
    /// </summary>
    internal static class MonetizationSelfCheck
    {
        private static int failures;

        [MenuItem("Tools/Monetization/Self Check")]
        private static void Run()
        {
            failures = 0;

            try
            {
                CheckHealthSystem();
                CheckIapCatalog();
                CheckSaveFields();
            }
            catch (System.Exception exception)
            {
                failures++;
                Debug.LogError("[MonetizationSelfCheck] Loi giua chung: " + exception);
            }

            Debug.Log(failures == 0
                ? "[MonetizationSelfCheck] PASS"
                : $"[MonetizationSelfCheck] FAIL x{failures}");
        }

        private static void CheckHealthSystem()
        {
            GameObject player = new GameObject("SelfCheckPlayer");
            player.tag = "Player";
            HealthSystem health = player.AddComponent<HealthSystem>();

            Check(!health.RevivePending, "Mac dinh khong co offer hoi sinh nao mo");

            // Trong luc offer hoi sinh mo: HP = 0 nhung IsDead phai false de
            // PlayerDeathHandler chua kip chay flow thua.
            health.MarkRevivePending();
            health.TakeDamage(1000);
            Check(!health.IsDead, "RevivePending: IsDead phai false ke ca khi HP = 0");
            Check(health.CurrentHealth == 0, "HP = 0 trong luc offer mo");
            Check(health.RevivePending, "RevivePending dang bat");

            health.ReviveAtHalfHealth();
            Check(!health.IsDead && !health.RevivePending, "Hoi sinh xong phai song");
            Check(health.CurrentHealth == Mathf.Max(1, health.MaxHealth / 2),
                $"Hoi sinh dung 50% HP (nhan {health.CurrentHealth}/{health.MaxHealth})");

            // Chet lan thu hai (da het luot hoi sinh): TakeDamage khong duoc mo
            // offer lan nua, ConfirmDeath chot cai chet nhu flow cu.
            health.MarkRevivePending();
            health.TakeDamage(1000);
            health.ConfirmDeath();
            Check(health.IsDead, "ConfirmDeath: IsDead true");
            Check(HealthSystem.PlayerDead, "ConfirmDeath: PlayerDead true");
            Check(!health.RevivePending, "ConfirmDeath: RevivePending phai tat");

            Object.DestroyImmediate(player);
        }

        private static void CheckIapCatalog()
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (IapProductDef product in IapService.Catalog)
            {
                Check(!string.IsNullOrEmpty(product.productId), "productId khong rong");
                Check(ids.Add(product.productId), "productId trung lap: " + product.productId);
                Check(product.gold > 0 || product.gem > 0, "San pham phai co thuong: " + product.productId);
                Check(!string.IsNullOrEmpty(product.priceLabel), "priceLabel khong rong: " + product.productId);
            }

            IapProductDef starter = IapService.Find(IapService.StarterPackId);
            Check(starter != null && starter.nonConsumable && starter.gold > 0 && starter.gem > 0,
                "Starter pack phai la NonConsumable co ca vang va gem");
        }

        private static void CheckSaveFields()
        {
            SaveData data = new SaveData();
            Check(!data.starterPackPurchased, "starterPackPurchased mac dinh false");
        }

        private static void Check(bool condition, string label)
        {
            if (!condition)
            {
                failures++;
                Debug.LogError("[MonetizationSelfCheck] " + label);
            }
        }
    }
}
#endif
