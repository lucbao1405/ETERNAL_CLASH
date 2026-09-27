using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using EternalClash.UI;

namespace EternalClash.EditorTools
{
    /// <summary>
    /// Dat san hop xac nhan "Reset Stat" (bản cứng) vao scene Town, nam trong
    /// hierarchy la Canvas/Man_Hinh_Khac/Trang_Bi/ResetConfirmDialog de co the
    /// sua truc tiep trong Hierarchy/Inspector. Chay menu Tools/Setup Reset
    /// Stat Confirm (Town) mot lan. TownStatPanelController tu tim va dung lai
    /// dialog nay khi bam nut reset; khong co thi no tu tao runtime nhu cu.
    /// </summary>
    public static class ResetStatConfirmSceneSetup
    {
        private const string TownScenePath = "Assets/Game/Scenes/Town.unity";
        private const string DialogName = "ResetConfirmDialog";
        private const string PanelSpritePath = "Assets/Ui/Town/info/khung_Lon.png";

        [MenuItem("Tools/Setup Reset Stat Confirm (Town)", false, 121)]
        public static void Setup()
        {
            if (!EnsureTownSceneOpen())
                return;

            Transform trangBi = FindTrangBiPanel();
            if (trangBi == null)
            {
                Debug.LogError("[ResetConfirmSetup] Khong tim thay panel Trang_Bi (co con Sta) trong scene Town.");
                return;
            }

            Transform existing = trangBi.Find(DialogName);
            if (existing != null)
            {
                Debug.Log("[ResetConfirmSetup] Dialog da ton tai: " + GetPath(existing));
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            GameObject dialog = BuildDialog(trangBi);
            EditorUtility.SetDirty(dialog);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>[ResetConfirmSetup] Da tao ResetConfirmDialog trong Trang_Bi (an mac dinh, bam nut reset trong game de xem).</color>");
            Selection.activeGameObject = dialog;
        }

        private static bool EnsureTownSceneOpen()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (string.Equals(scene.path, TownScenePath, System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (scene.isDirty)
            {
                bool saved = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                if (!saved)
                {
                    Debug.LogWarning("[ResetConfirmSetup] Can save scene hien tai truoc khi mo Town.");
                    return false;
                }
            }

            EditorSceneManager.OpenScene(TownScenePath, OpenSceneMode.Single);
            return true;
        }

        /// <summary>
        /// Cach tim panel "Trang_Bi" giong voi TownStatPanelController: phai co
        /// con "Sta", chon rect lon nhat de tranh object trung ten.
        /// </summary>
        private static Transform FindTrangBiPanel()
        {
            Transform best = null;
            float bestArea = -1f;

            foreach (var candidate in Object.FindObjectsOfType<Transform>(true))
            {
                if (!string.Equals(candidate.name, "Trang_Bi", System.StringComparison.Ordinal))
                    continue;
                if (candidate.Find("Sta") == null)
                    continue;

                var rect = candidate as RectTransform;
                float area = rect != null ? rect.rect.width * rect.rect.height : 0f;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = candidate;
                }
            }

            return best;
        }

        private static GameObject BuildDialog(Transform trangBi)
        {
            GameObject root = new GameObject(DialogName, typeof(RectTransform), typeof(Image), typeof(Button));
            root.layer = trangBi.gameObject.layer;
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.SetParent(trangBi, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.SetAsLastSibling();

            // Nen mo: bam ra ngoai cung dong hop.
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(rootRect, false);
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(620f, 360f);
            Image panelImage = panel.GetComponent<Image>();
            panelImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;

            TMP_Text title = CreateText(panelRect, "Title", "Reset Stat Points", 30,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -56f), new Vector2(-32f, -8f), Color.white);
            title.fontStyle = FontStyles.Bold;

            CreateText(panelRect, "Message",
                "Reset all stat points to base values?\nThis action is free this time.", 24,
                new Vector2(0f, 0.28f), new Vector2(1f, 0.78f), new Vector2(32f, 0f), new Vector2(-32f, 0f), Color.white);

            CreateButton(panelRect, "ConfirmButton", "Confirm", new Color(0.2f, 0.5f, 0.25f, 1f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 28f), new Vector2(220f, 60f));
            CreateButton(panelRect, "CancelButton", "Cancel", new Color(0.55f, 0.2f, 0.2f, 1f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(24f, 28f), new Vector2(220f, 60f));

            root.SetActive(false);

            // Canvas rieng de dialog luon ve tren cung, khong bi Down_Panel dap.
            Canvas overlay = root.AddComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = 600;
            overlay.worldCamera = trangBi.GetComponentInParent<Canvas>().rootCanvas.worldCamera;
            root.AddComponent<GraphicRaycaster>();

            return root;
        }

        private static TMP_Text CreateText(Transform parent, string name, string content, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.text = content;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.enableWordWrapping = true;
            label.raycastTarget = false;
            return label;
        }

        private static void CreateButton(Transform parent, string name, string labelText, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;

            GameObject label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
