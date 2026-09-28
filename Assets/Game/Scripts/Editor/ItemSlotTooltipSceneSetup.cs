using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using EternalClash.UI;

namespace EternalClash.EditorTools
{
    /// <summary>
    /// Copy khung StartToolTip (giong tooltip chi so Int/Vit trong hierarchy) vao TUNG o
    /// can bam de nguoi dung chinh kich thuoc rieng tung o:
    ///   - Trang_Bi: Sword, Shield, Giap, Lon + Healing/Cooldown/Defense_Potion
    ///   - Bag: tat ca o con trong "Bag/Item" (Item_1..Item_12)
    /// Moi StartToolTip con an KOP dung voi o (anchor 0,0-1,1) va chu duoc canh font
    /// vua voi chieu cao o. Runtime se uu tien hien con StartToolTip cua chinh o do
    /// thay vi bubble dung chung.
    ///
    /// Menu: Tools/UI/Create StartToolTip Under Slots (Trang_Bi + Bag). Chay lai thi bo
    /// qua nhom o da co StartToolTip.
    /// </summary>
    public static class ItemSlotTooltipSceneSetup
    {
        private const string TooltipChildName = "StartToolTip";
        private const string PrefabPath = "UI/StartToolTip";

        private static readonly string[] EquipmentSlots = { "Sword", "Shield", "Giap", "Lon" };
        private static readonly string[] PotionSlots = { "Healing_Potion", "Cooldown_Potion", "Defense_Potion" };

        [MenuItem("Tools/UI/Create StartToolTip Under Slots (Trang_Bi + Bag)")]
        public static void Create()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Town")
            {
                Debug.LogWarning("[SlotTooltip] Mo scene Town truoc khi chay menu nay.");
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[SlotTooltip] Khong tim thay Resources/" + PrefabPath + ".prefab.");
                return;
            }

            int created = 0;

            Transform panel = FindEquipmentPanel(scene);
            if (panel == null)
            {
                Debug.LogWarning("[SlotTooltip] Khong tim thay panel 'Trang_Bi' co o vu khi trong scene Town.");
            }
            else
            {
                created += CreateForNamedSlots(panel, EquipmentSlots, prefab);
                created += CreateForPotions(panel, PotionSlots, prefab);
            }

            created += CreateForBag(prefab);

            if (created > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"[SlotTooltip] Da tao {created} StartToolTip con. Mo rong tung o trong Hierarchy de chinh size/font rieng; de object o trang thai TAT (game tu bat khi bam/giu o).");
        }

        // Bag khong con dung tooltip con tung o nua (dung chung 1 bubble) — menu nay don dep.
        [MenuItem("Tools/UI/Remove StartToolTip Under Bag Slots")]
        public static void RemoveBagTooltips()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "Town")
            {
                Debug.LogWarning("[SlotTooltip] Mo scene Town truoc khi chay menu nay.");
                return;
            }

            int removed = 0;
            foreach (BagController bag in Object.FindObjectsOfType<BagController>(true))
            {
                Transform grid = bag.transform.Find("Item");
                if (grid == null)
                    continue;

                foreach (Transform slot in grid)
                {
                    Transform tooltip = slot.Find(TooltipChildName);
                    if (tooltip != null)
                    {
                        Object.DestroyImmediate(tooltip.gameObject);
                        removed++;
                    }
                }
            }

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"[SlotTooltip] Da xoa {removed} StartToolTip con trong Bag (bag dung chung 1 bubble).");
        }

        private static int CreateForNamedSlots(Transform panel, string[] slotNames, GameObject prefab)
        {
            int count = 0;
            foreach (string slotName in slotNames)
            {
                Transform slot = FindDescendant(panel, slotName);
                if (slot == null)
                {
                    Debug.LogWarning($"[SlotTooltip] Khong tim thay o '{slotName}' trong panel Trang_Bi.");
                    continue;
                }
                if (CreateUnder(slot, prefab))
                    count++;
            }
            return count;
        }

        private static int CreateForPotions(Transform panel, string[] potionNames, GameObject prefab)
        {
            int count = 0;
            foreach (string potionName in potionNames)
            {
                foreach (Transform t in panel.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name != potionName)
                        continue;
                    if (CreateUnder(t, prefab))
                        count++;
                }
            }
            return count;
        }

        private static int CreateForBag(GameObject prefab)
        {
            int count = 0;
            foreach (BagController bag in Object.FindObjectsOfType<BagController>(true))
            {
                Transform grid = bag.transform.Find("Item");
                if (grid == null)
                    continue;

                foreach (Transform slot in grid)
                {
                    if (CreateUnder(slot, prefab))
                        count++;
                }
            }
            return count;
        }

        // Tao con StartToolTip nam TREN o (khong che icon) + canh font vua chieu cao tooltip.
        // Neu con da ton tai ma van dang phu kin o (anchor 0,0-1,1 thi sua lai cho dung cho;
        // nhung con da duoc nguoi dung tu chinh thi giu nguyen).
        private static bool CreateUnder(Transform slot, GameObject prefab)
        {
            Transform existing = slot.Find(TooltipChildName);
            if (existing != null)
            {
                if (existing is RectTransform stretched &&
                    stretched.anchorMin == Vector2.zero && stretched.anchorMax == Vector2.one &&
                    slot is RectTransform slotRect0)
                {
                    ApplyPlacement(stretched, slotRect0);
                    return true;
                }
                return false;
            }

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = TooltipChildName;
            go.transform.SetParent(slot, false);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // Bo auto-layout de chinh size + font tung o mot minh.
            Object.DestroyImmediate(go.GetComponent<VerticalLayoutGroup>());
            Object.DestroyImmediate(go.GetComponent<ContentSizeFitter>());

            ApplyPlacement((RectTransform)go.transform, (RectTransform)slot);

            go.SetActive(false);
            return true;
        }

        // Dat tooltip: neo tren-cop o, nhich len phia tren (day tooltip cach mat o 8px),
        // rong ~= 2.2 lan rong o de vua dong chu, va co Canvas rieng luon ve tren cac o khac.
        private static void ApplyPlacement(RectTransform tooltip, RectTransform slot)
        {
            tooltip.anchorMin = new Vector2(0.5f, 1f);
            tooltip.anchorMax = new Vector2(0.5f, 1f);
            tooltip.pivot = new Vector2(0.5f, 0f);
            tooltip.anchoredPosition = new Vector2(0f, 8f);
            tooltip.localScale = Vector3.one;

            float w = Mathf.Clamp(slot.rect.width * 2.2f, 240f, 650f);
            float h = Mathf.Clamp(slot.rect.height * 1.6f, 130f, 260f);
            tooltip.sizeDelta = new Vector2(w, h);

            Canvas canvas = tooltip.GetComponent<Canvas>();
            if (canvas == null)
                canvas = tooltip.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;

            StyleText(tooltip.transform, "Tooltip_Title", 0.58f, 0.92f, Mathf.Clamp(h * 0.17f, 12f, 40f));
            StyleText(tooltip.transform, "Tooltip_Subtitle", 0.44f, 0.56f, Mathf.Clamp(h * 0.10f, 9f, 26f));
            StyleText(tooltip.transform, "Tooltip_Stats", 0.32f, 0.42f, Mathf.Clamp(h * 0.11f, 10f, 28f));
            StyleText(tooltip.transform, "Tooltip_Description", 0.04f, 0.30f, Mathf.Clamp(h * 0.12f, 10f, 32f));
        }

        private static void StyleText(Transform root, string childName, float yMin, float yMax, float fontSize)
        {
            Transform child = FindDescendant(root, childName);
            if (child == null)
                return;

            if (child is RectTransform rect)
            {
                rect.anchorMin = new Vector2(0.06f, yMin);
                rect.anchorMax = new Vector2(0.94f, yMax);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text != null)
                text.fontSize = fontSize;
        }

        private static Transform FindEquipmentPanel(Scene scene)
        {
            if (!scene.IsValid())
                return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
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

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t != root && t.name == name)
                    return t;
            }
            return null;
        }
    }
}
