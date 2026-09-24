#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalClash.Tutorial;

namespace EternalClash.EditorTools
{
    /// <summary>
    /// Self-check cho bang nhap ten dung san trong scene Town
    /// (Canvas/Man_Hinh_Khac/Nhap_Ten). Chay bang menu Tools/Tutorial/Name Prompt
    /// Self Check khi scene Town dang mo. Kiem tra tham chieu UI, nut Xac Nhan,
    /// va logic Submit (trim, bo ten rong). Khong dung test framework.
    /// </summary>
    internal static class TutorialNamePromptSelfCheck
    {
        private static int failures;

        [MenuItem("Tools/Tutorial/Name Prompt Self Check")]
        private static void Run()
        {
            failures = 0;

            if (!string.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "Town",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError("[NamePromptSelfCheck] Can mo scene Town truoc khi chay self check.", null);
                return;
            }

            GameObject root = GameObject.Find("Canvas/Man_Hinh_Khac/Nhap_Ten");
            Check(root != null, "Scene Town phai co Canvas/Man_Hinh_Khac/Nhap_Ten");
            if (root == null)
                return;

            Check(!root.activeSelf, "Nhap_Ten phai dat OFF trong editor (tutorial se bat len)");

            PlayerNamePromptUI prompt = root.GetComponent<PlayerNamePromptUI>();
            Check(prompt != null, "Nhap_Ten phai co component PlayerNamePromptUI");
            if (prompt == null)
                return;

            TMP_InputField input = root.GetComponentInChildren<TMP_InputField>(true);
            Check(input != null, "Nhap_Ten phai co TMP_InputField (O_Nhap)");
            if (input == null)
                return;

            Check(input.textComponent != null, "O_Nhap.textComponent chua duoc gan (Text TMP)");
            Check(input.placeholder != null, "O_Nhap.placeholder chua duoc gan (Placeholder)");
            Check(input.textViewport != null, "O_Nhap.textViewport chua duoc gan (Text Area)");

            Button confirm = root.GetComponentInChildren<Button>(true);
            Check(confirm != null, "Nhap_Ten phai co nut Xac Nhan");
            Check(confirm != null && confirm.onClick.GetPersistentEventCount() == 1,
                "Nut Xac Nhan phai co dung 1 listener OnClick tro den PlayerNamePromptUI.Submit");

            // Logic Submit: ten rong/khoang trang phai bi tu choi, ten hop le
            // duoc trim va bao dung mot lan.
            string received = null;
            int callCount = 0;
            System.Action<string> handler = value =>
            {
                callCount++;
                received = value;
            };
            prompt.NameSubmitted += handler;

            input.text = "   ";
            prompt.Submit();
            Check(callCount == 0, "Submit voi ten toan khoang trang khong duoc bao su kien");

            input.text = "  Ga Con  ";
            prompt.Submit();
            Check(callCount == 1 && received == "Ga Con",
                "Submit phai trim ten va bao dung mot lan (nhan: '" + received + "')");

            prompt.NameSubmitted -= handler;

            if (failures == 0)
                Debug.Log("[NamePromptSelfCheck] OK: moi lien ket hop le, logic Submit dung.", prompt);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
            {
                failures++;
                Debug.LogError("[NamePromptSelfCheck] FAIL: " + message, null);
            }
        }
    }
}
#endif
