#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace EternalClash.EditorTools
{
    /// <summary>
    /// Self-check cho nhan ten nguoi choi trong scene Town. Chay bang menu
    /// Tools/Town/Player Identity Self Check. Loi goc: TMP_Text dat Text Style
    /// "Title" ket hop richText = false trong PlayerIdentityUIController lam
    /// TMP chen dinh nghia style (<size=125%><b><align=center>) hien nhu chu
    /// thuong. Khong dung test framework.
    /// </summary>
    internal static class PlayerIdentitySelfCheck
    {
        private static int failures;

        [MenuItem("Tools/Town/Player Identity Self Check")]
        private static void Run()
        {
            failures = 0;

            if (!string.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "Town",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError("[IdentitySelfCheck] Can mo scene Town truoc khi chay self check.", null);
                return;
            }

            GameObject downPanel = GameObject.Find("Canvas/Down_Panel");
            Check(downPanel != null, "Scene Town phai co Canvas/Down_Panel");
            if (downPanel == null)
                return;

            Transform stat = FindChild(downPanel.transform, "Stat");
            Check(stat != null, "Down_Panel phai co nhom Stat");
            if (stat == null)
                return;

            CheckName(stat, "Stat");
            Transform level = FindChild(stat, "Level");
            Check(level != null && level.GetComponentInChildren<TMP_Text>(true) != null,
                "Stat phai co Level kem TMP_Text");

            // Controller phai bind duoc nhan ten ho so qua reflection.
            GameObject hook = new GameObject("IdentitySelfCheck_Hook");
            hook.AddComponent<EternalClash.UI.PlayerIdentityUIController>();
            try
            {
                var controller = hook.GetComponent<EternalClash.UI.PlayerIdentityUIController>();
                System.Reflection.MethodInfo resolve = typeof(EternalClash.UI.PlayerIdentityUIController)
                    .GetMethod("ResolveTargets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                resolve.Invoke(controller, null);

                System.Reflection.FieldInfo profileField = typeof(EternalClash.UI.PlayerIdentityUIController)
                    .GetField("profileNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var profileText = profileField != null ? profileField.GetValue(controller) as TMP_Text : null;
                Check(profileText != null, "Controller phai bind profileNameText -> Canvas/Man_Hinh_Khac/Avt/Khung nho/Name");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hook);
            }

            // Moi TMP_Text "Name" khac trong scene (vi du Avt/Khung nho/Name)
            // cung phai dung Text Style Normal de khong lap lai loi.
            foreach (var tmp in UnityEngine.Object.FindObjectsOfType<TMP_Text>(true))
            {
                if (!string.Equals(tmp.gameObject.name, "Name", StringComparison.Ordinal))
                    continue;
                Check(tmp.textStyle != null && string.Equals(tmp.textStyle.name, "Normal", StringComparison.Ordinal),
                    tmp.transform.name + " (" + GetPath(tmp.transform) + ") phai dung Text Style Normal");
            }

            if (failures == 0)
                Debug.Log("[IdentitySelfCheck] OK: cac nhan ten dung Text Style Normal.", downPanel);
        }

        private static void CheckName(Transform stat, string group)
        {
            Transform name = FindChild(stat, "Name");
            Check(name != null, group + " phai co Name");
            var tmp = name != null ? name.GetComponentInChildren<TMP_Text>(true) : null;
            Check(tmp != null, group + "/Name phai co TMP_Text");
            if (tmp != null)
                Check(tmp.textStyle != null && string.Equals(tmp.textStyle.name, "Normal", StringComparison.Ordinal),
                    group + "/Name phai dung Text Style Normal (Title + richText=false lam lo the rich text)");
        }

        private static string GetPath(Transform t)
        {
            var parts = new System.Collections.Generic.List<string>();
            while (t != null) { parts.Add(t.name); t = t.parent; }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null || string.Equals(root.name, name, StringComparison.Ordinal))
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindChild(root.GetChild(i), name);
                if (match != null)
                    return match;
            }
            return null;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
            {
                failures++;
                Debug.LogError("[IdentitySelfCheck] FAIL: " + message, null);
            }
        }
    }
}
#endif
