#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;
using EternalClash.UI;

namespace EternalClash.EditorTools
{
    /// <summary>
    /// Self check rut gon so tien tren thanh Vang/Kim cuong (Town):
    /// duoi 1000 giu nguyen, tu 1000 hien dang "1,01k" (dau phay thap phan),
    /// tu 1.000.000 hien dang "1,23m". Chay bang menu Tools/Eternal Clash/
    /// Currency Format Self Check. Khong dung test framework.
    /// </summary>
    internal static class CurrencyFormatSelfCheck
    {
        [MenuItem("Tools/Eternal Clash/Currency Format Self Check")]
        private static void Run()
        {
            MethodInfo format = typeof(TownStatPanelController).GetMethod(
                "FormatCurrency", BindingFlags.NonPublic | BindingFlags.Static);
            Check(format != null, "Khong tim thay TownStatPanelController.FormatCurrency");
            if (format == null)
                return;

            Check(Invoke(format, 0) == "0", "0 -> '0'");
            Check(Invoke(format, 999) == "999", "999 -> '999' (duoi 1000 giu nguyen)");
            Check(Invoke(format, 1000) == "1k", "1000 -> '1k'");
            Check(Invoke(format, 1010) == "1,01k", "1010 -> '1,01k'");
            Check(Invoke(format, 1500) == "1,5k", "1500 -> '1,5k' (bo so 0 thua)");
            Check(Invoke(format, 15000) == "15k", "15000 -> '15k'");
            Check(Invoke(format, 123456) == "123,46k", "123456 -> '123,46k'");
            Check(Invoke(format, 1500000) == "1,5m", "1500000 -> '1,5m'");

            if (failures == 0)
                Debug.Log("[CurrencyFormatSelfCheck] OK: rut gon k/m dung nhu thiet ke.");
        }

        private static int failures;

        private static string Invoke(MethodInfo format, int value)
        {
            return (string)format.Invoke(null, new object[] { value });
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
            {
                failures++;
                Debug.LogError("[CurrencyFormatSelfCheck] FAIL: " + message);
            }
        }
    }
}
#endif
