using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace EternalClash.EditorTools
{
    /// <summary>
    /// Tao ban cung "ItemInfoBubble" trong scene Town de nguoi dung tu chinh kich thuoc,
    /// vi tri, mau sac cua bubble Trang_Bi truc tiep trong Hierarchy (giong cac StartToolTip
    /// chi so). EquipmentInfoTooltipController se tu tim dung object nay khi hien tooltip.
    ///
    /// Cach dung: mo scene Town -> Tools/UI/Create ItemInfoBubble (Trang_Bi). Chay lai menu
    /// nay khi object da ton tai thi chi chon object do trong Hierarchy.
    /// </summary>
    public static class ItemInfoBubbleSceneSetup
    {
        private const string BubbleName = "ItemInfoBubble";
        private const string PrefabPath = "UI/StartToolTip";

        [MenuItem("Tools/UI/Create ItemInfoBubble (Trang_Bi)")]
        public static void Create()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Town")
            {
                Debug.LogWarning("[ItemInfoBubble] Mo scene Town truoc khi chay menu nay.");
                return;
            }

            Transform panel = FindEquipmentPanel(scene);
            if (panel == null)
            {
                Debug.LogWarning("[ItemInfoBubble] Khong tim thay panel 'Trang_Bi' co o vu khi trong scene Town.");
                return;
            }

            Canvas canvas = panel.GetComponentInParent<Canvas>();

            Transform existing = FindDescendant(canvas.transform, BubbleName);
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[ItemInfoBubble] Da ton tai trong scene — da chon lai trong Hierarchy.");
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[ItemInfoBubble] Khong tim thay Resources/" + PrefabPath + ".prefab.");
                return;
            }

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = BubbleName;
            go.transform.SetParent(canvas.transform, false);

            // Tach khoi prefab + bo auto-layout de chinh size va vi tri text hoan toan thu cong.
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(go.GetComponent<VerticalLayoutGroup>());
            Object.DestroyImmediate(go.GetComponent<ContentSizeFitter>());

            SetAnchors(go.transform, "Tooltip_Title", 0.06f, 0.60f, 0.94f, 0.94f);
            SetAnchors(go.transform, "Tooltip_Subtitle", 0.06f, 0.44f, 0.94f, 0.58f);
            SetAnchors(go.transform, "Tooltip_Stats", 0.06f, 0.28f, 0.94f, 0.40f);
            SetAnchors(go.transform, "Tooltip_Description", 0.06f, 0.04f, 0.94f, 0.24f);

            go.SetActive(false);
            Selection.activeGameObject = go;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ItemInfoBubble] Da tao ban cung trong scene Town (dang duoc chon trong Hierarchy). Bat object len de xem truoc, chinh xong nho tat lai.");
        }

        // Bo 4 dong chu thanh cac bang neo phan tram — keo size bubble thi chu gian theo.
        private static void SetAnchors(Transform root, string childName, float xMin, float yMin, float xMax, float yMax)
        {
            Transform child = FindDescendant(root, childName);
            if (child == null)
                return;

            RectTransform rect = child as RectTransform;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Transform FindEquipmentPanel(Scene scene)
        {
            if (!scene.IsValid())
                return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    // Co hai panel "Trang_Bi"; panel can tim la panel co o "Sword".
                    if (t.name == "Trang_Bi" && FindDescendant(t, "Sword") != null)
                        return t;
                }
            }

            return null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
                return null;

            Transform result = null;
            Walk(root, t =>
            {
                if (result != null)
                    return;
                if (t != root && Normalize(t.name) == Normalize(name))
                    result = t;
            });
            return result;
        }

        private static void Walk(Transform root, System.Action<Transform> visitor)
        {
            if (root == null)
                return;

            var stack = new System.Collections.Generic.Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                Transform cur = stack.Pop();
                visitor(cur);
                for (int i = cur.childCount - 1; i >= 0; i--)
                    stack.Push(cur.GetChild(i));
            }
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var buffer = new System.Text.StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c))
                    buffer.Append(char.ToLowerInvariant(c));
            }
            return buffer.ToString();
        }
    }
}
