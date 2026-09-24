#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace EternalClash.EditorTools
{
    /// <summary>
    /// Self-check cho thong tin mo rong trong panel ho so Town
    /// (Canvas/Man_Hinh_Khac/Avt/ThongTin + Badge_VIP). Chay bang menu
    /// Tools/Town/Profile Panel Self Check khi scene Town dang mo. Kiem tra
    /// cau truc UI va viec TownProfilePanelController tim dung cac o so.
    /// Khong dung test framework.
    /// </summary>
    internal static class TownProfilePanelSelfCheck
    {
        private static int failures;

        [MenuItem("Tools/Town/Profile Panel Self Check")]
        private static void Run()
        {
            failures = 0;

            if (!string.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "Town",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError("[ProfileSelfCheck] Can mo scene Town truoc khi chay self check.", null);
                return;
            }

            GameObject screens = GameObject.Find("Canvas/Man_Hinh_Khac");
            Check(screens != null, "Scene Town phai co Canvas/Man_Hinh_Khac");
            if (screens == null)
                return;

            Transform avt = FindChild(screens.transform, "Avt");
            Check(avt != null, "Man_Hinh_Khac phai co panel Avt");
            if (avt == null)
                return;

            Transform info = FindChild(avt, "ThongTin");
            Check(info != null, "Avt phai co nhom ThongTin");
            if (info == null)
                return;

            CheckRow(info, "Hang_Stage");
            CheckRow(info, "Hang_Ela");

            Transform potions = FindChild(info, "Hang_Thuoc");
            Check(potions != null, "ThongTin phai co hang Hang_Thuoc");
            if (potions != null)
            {
                CheckGroup(potions, "Healing");
                CheckGroup(potions, "Cooldown");
                CheckGroup(potions, "Defense");
            }

            Transform badge = FindChild(avt, "Badge_VIP");
            Check(badge != null, "Avt phai co Badge_VIP");
            if (badge != null)
            {
                Check(FindChild(badge, "Icon") != null, "Badge_VIP phai co Icon");
                Check(badge.GetComponentInChildren<TMP_Text>(true) != null, "Badge_VIP phai co TMP_Text");
            }

            // Controller phai resolve duoc tat ca o so qua reflection
            // (Refresh goi ResolveTargets khi cac text con null).
            GameObject hook = new GameObject("ProfileSelfCheck_Hook");
            hook.AddComponent<EternalClash.UI.TownProfilePanelController>();
            try
            {
                MethodInfo resolve = typeof(EternalClash.UI.TownProfilePanelController)
                    .GetMethod("ResolveTargets", BindingFlags.NonPublic | BindingFlags.Instance);
                resolve.Invoke(hook.GetComponent<EternalClash.UI.TownProfilePanelController>(), null);

                CheckText(hook, "stageText", "Hang_Stage/Value");
                CheckText(hook, "healText", "Hang_Thuoc/Healing/Value");
                CheckText(hook, "cooldownText", "Hang_Thuoc/Cooldown/Value");
                CheckText(hook, "defenseText", "Hang_Thuoc/Defense/Value");
                CheckText(hook, "elaText", "Hang_Ela/Value");
                CheckBadge(hook);
            }
            finally
            {
                Object.DestroyImmediate(hook);
            }

            if (failures == 0)
                Debug.Log("[ProfileSelfCheck] OK: cau truc UI va lien ket controller hop le.", avt.gameObject);
        }

        private static void CheckRow(Transform info, string rowName)
        {
            Transform row = FindChild(info, rowName);
            Check(row != null, "ThongTin phai co hang " + rowName);
            if (row == null)
                return;
            Check(FindChild(row, "Icon") != null, rowName + " phai co Icon");
            Check(FindChild(row, "Label") != null, rowName + " phai co Label");
            Check(FindChild(row, "Value") != null && FindChild(row, "Value").GetComponent<TMP_Text>() != null,
                rowName + " phai co Value kem TMP_Text");
        }

        private static void CheckGroup(Transform potions, string groupName)
        {
            Transform group = FindChild(potions, groupName);
            Check(group != null, "Hang_Thuoc phai co nhom " + groupName);
            if (group == null)
                return;
            Check(FindChild(group, "Icon") != null, groupName + " phai co Icon");
            Check(FindChild(group, "Value") != null && FindChild(group, "Value").GetComponent<TMP_Text>() != null,
                groupName + " phai co Value kem TMP_Text");
        }

        private static void CheckText(GameObject hook, string fieldName, string path)
        {
            FieldInfo field = typeof(EternalClash.UI.TownProfilePanelController)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            var value = field != null ? field.GetValue(hook.GetComponent<EternalClash.UI.TownProfilePanelController>()) as TMP_Text : null;
            Check(value != null, "Controller phai bind " + fieldName + " -> " + path);
        }

        private static void CheckBadge(GameObject hook)
        {
            FieldInfo field = typeof(EternalClash.UI.TownProfilePanelController)
                .GetField("vipBadge", BindingFlags.NonPublic | BindingFlags.Instance);
            var value = field != null ? field.GetValue(hook.GetComponent<EternalClash.UI.TownProfilePanelController>()) as GameObject : null;
            Check(value != null, "Controller phai bind vipBadge -> Badge_VIP");
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null || string.Equals(root.name, name, System.StringComparison.Ordinal))
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
                Debug.LogError("[ProfileSelfCheck] FAIL: " + message, null);
            }
        }
    }
}
#endif
