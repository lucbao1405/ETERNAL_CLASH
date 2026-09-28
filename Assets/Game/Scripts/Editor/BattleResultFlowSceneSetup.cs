using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Spine.Unity;
using EternalClash.UI;
using EternalClash.BattleResult;

namespace EternalClash.EditorTools
{
    /// <summary>
    /// Editor-only tool: wires the Postknight-style Victory Chest Reward Flow into
    /// the currently open Battle scene.
    ///
    ///  1. Re-centers + hides the scene's Win_Popup / Lose_Popup.
    ///  2. Adds RewardItemSlot + ItemPic icon Image on each of the 8 slot rows so
    ///     BattleResultUI can populate them.
    ///  3. Adds a "Gold" TMP row (Victory) so gold is visible on the Win popup.
    ///  4. Builds a "ChestRewardPopup" UI (closed/open chest art + per-tap item
    ///     reveal) with ChestRewardUI under MainCanvas.
    ///  5. Adds a scene "BattleResultFlowController" GameObject and assigns all
    ///     references (chest prefab, chest popup, win/lose popups + UIs).
    ///  6. Saves the scene.
    ///
    /// Safe to run repeatedly: existing objects are re-used, not duplicated.
    /// </summary>
    public static class BattleResultFlowSceneSetup
    {
        private const string ChestSpineDataPath = "Assets/Game/Animations/chest/spine_SkeletonData.asset";

        // Win/Lose popup nên nằm cao hơn tâm một chút (px trong canvas).
        private const float ResultPopupRaiseY = 75f;

        [MenuItem("Tools/Battle Result/Setup Battle Scene Wiring")]
        public static void SetupFromMenu()
        {
            Debug.Log(Setup());
        }

        /// <summary>Entry point used by the MCP/execute-code bridge.</summary>
        public static string Setup()
        {
            var log = new StringBuilder();
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return "ERROR: No active scene.";

            Transform canvas = FindRootChild(scene, "MainCanvas");
            if (canvas == null)
                return "ERROR: MainCanvas not found in active scene.";

            Transform manKhac = FindDescendant(canvas, "Man_Hinh_Khac");
            Transform winT = FindDescendant(canvas, "Win_Popup");
            Transform loseT = FindDescendant(canvas, "Lose_Popup");

            // 1) Result popups: on-screen position + hidden.
            CenterAndHide(winT, log, "Win_Popup");
            CenterAndHide(loseT, log, "Lose_Popup");

            // 2) Slot preparation (idempotent).
            if (winT != null) PrepareSlots(winT);
            if (loseT != null) PrepareSlots(loseT);

            // 3) Build / reuse ChestRewardPopup under MainCanvas.
            RectTransform chestPopup = EnsureChestPopup(manKhac != null ? manKhac : canvas, log);

            // 4) Wire result UIs onto Win/Lose.
            BattleResultUI winUI = WireResultUI(winT, addGoldRow: true, log, "Win_Popup");
            BattleResultUI loseUI = WireResultUI(loseT, addGoldRow: false, log, "Lose_Popup");

            // 5) Manager object + references.
            Transform managerRoot = FindRootChild(scene, "BattleResultFlowController");
            if (managerRoot == null)
            {
                var go = new GameObject("BattleResultFlowController");
                SceneManager.MoveGameObjectToScene(go, scene);
                managerRoot = go.transform;
                log.AppendLine("Created BattleResultFlowController GameObject.");
            }
            var controller = managerRoot.GetComponent<BattleResultFlowController>();
            if (controller == null)
                controller = managerRoot.gameObject.AddComponent<BattleResultFlowController>();
            if (managerRoot.GetComponent<ChestRewardController>() == null)
                managerRoot.gameObject.AddComponent<ChestRewardController>();
            WireManager(controller, chestPopup, winT, loseT, winUI, loseUI, scene);

            // 6) Save.
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            log.AppendLine("Scene saved: " + saved);
            return log.ToString();
        }

        // ------------------------------------------------------------------
        // Popups
        // ------------------------------------------------------------------

        private static void CenterAndHide(Transform root, StringBuilder log, string label)
        {
            if (root == null)
            {
                log.AppendLine("WARN: " + label + " not found.");
                return;
            }
            var rt = root as RectTransform;
            if (rt != null)
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, ResultPopupRaiseY);
            root.gameObject.SetActive(false);
            log.AppendLine(label + " centered + hidden.");
        }

        private static void PrepareSlots(Transform popup)
        {
            for (int i = 1; i <= 8; i++)
            {
                Transform slot = FindDescendant(popup, i.ToString());
                if (slot == null)
                    slot = FindDescendant(popup, "ItemSlot_" + i);
                if (slot == null)
                    continue;

                Image frame = slot.GetComponent<Image>();
                if (frame == null)
                    frame = slot.gameObject.AddComponent<Image>();
                frame.raycastTarget = true;
                if (frame.color.a < 0.01f)
                    frame.color = new Color(1f, 1f, 1f, 0.003f);

                Transform pic = FindDescendant(slot, "ItemPic");
                if (pic == null)
                    pic = FindDescendant(slot, "Icon");
                if (pic != null)
                {
                    Image icon = pic.GetComponent<Image>();
                    if (icon == null)
                        icon = pic.gameObject.AddComponent<Image>();
                    icon.raycastTarget = false;
                    icon.color = Color.white;
                }

                if (slot.GetComponent<RewardItemSlot>() == null)
                    slot.gameObject.AddComponent<RewardItemSlot>();
                if (slot.GetComponent<ItemTooltipController>() == null)
                    slot.gameObject.AddComponent<ItemTooltipController>();

                foreach (TextMeshProUGUI itemName in slot.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (itemName != null && Normalize(itemName.gameObject.name) == "itemname")
                        itemName.gameObject.SetActive(false);
                }
            }
        }

        // ------------------------------------------------------------------
        // ChestRewardPopup
        // ------------------------------------------------------------------

        private static RectTransform EnsureChestPopup(Transform parent, StringBuilder log)
        {
            Transform existing = FindDescendant(parent, "ChestRewardPopup");
            if (existing != null)
            {
                // Luôn dựng lại để tránh giữ lại trạng thái dở dang/thiếu refs.
                Object.DestroyImmediate(existing.gameObject);
                log.AppendLine("ChestRewardPopup rebuilt from scratch.");
            }

            var popup = CreateRect("ChestRewardPopup", parent);
            Stretch(popup);
            popup.gameObject.AddComponent<CanvasGroup>();

            Image dimImg = CreateImage("Dim", popup, Stretch);
            dimImg.color = new Color(0f, 0f, 0f, 0.55f);

            SkeletonGraphic chestSkeleton = CreateChestSkeleton(popup, log);

            // Lấy TMP mẫu ở phạm vi cả canvas (popup mới chưa có text nào).
            TextMeshProUGUI sample = FindSampleText(parent);
            if (sample == null)
                sample = FindSampleText(popup);
            if (sample == null)
                log.AppendLine("WARN: no TMP sample found; text clones disabled.");

            RectTransform itemRoot = CreateRect("ItemDisplay", popup);
            CenterBox(itemRoot, new Vector2(680f, 300f), new Vector2(0f, 175f));

            RectTransform iconRt = CreateRect("Icon", itemRoot);
            CenterBox(iconRt, new Vector2(160f, 160f), new Vector2(0f, 80f));
            Image iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.raycastTarget = false;

            RectTransform nameRt = CreateRect("ItemName", itemRoot);
            CenterBox(nameRt, new Vector2(660f, 60f), new Vector2(0f, -10f));
            TextMeshProUGUI nameTmp = sample != null ? CloneText(sample, nameRt, "Text") : null;
            if (nameTmp != null) Style(nameTmp, 44f);

            RectTransform qtyRt = CreateRect("ItemQty", itemRoot);
            CenterBox(qtyRt, new Vector2(420f, 54f), new Vector2(0f, -70f));
            TextMeshProUGUI qtyTmp = sample != null ? CloneText(sample, qtyRt, "Text") : null;
            if (qtyTmp != null) Style(qtyTmp, 36f);

            itemRoot.gameObject.SetActive(false);

            RectTransform tapRt = CreateRect("TapHint", popup);
            CenterBox(tapRt, new Vector2(520f, 60f), new Vector2(0f, -330f));
            TextMeshProUGUI tapTmp = sample != null ? CloneText(sample, tapRt, "Text") : null;
            if (tapTmp != null)
            {
                Style(tapTmp, 28f);
                tapTmp.text = "TAP to claim";
                tapTmp.gameObject.SetActive(false);
            }

            RectTransform claimRt = CreateRect("ClaimArea", popup);
            Stretch(claimRt);
            Image claimImg = claimRt.gameObject.AddComponent<Image>();
            claimImg.color = new Color(1f, 1f, 1f, 0.003f);
            claimImg.raycastTarget = true;
            Button claimBtn = claimRt.gameObject.AddComponent<Button>();
            claimBtn.transition = Selectable.Transition.None;
            claimBtn.targetGraphic = claimImg;
            claimRt.SetAsLastSibling();

            ChestAnimationController animation = popup.gameObject.AddComponent<ChestAnimationController>();
            animation.Configure(chestSkeleton);
            ChestRewardRevealController reveal = popup.gameObject.AddComponent<ChestRewardRevealController>();
            ChestRewardUI ui = popup.gameObject.AddComponent<ChestRewardUI>();
            var so = new SerializedObject(ui);
            SetRef(so, "chestSkeleton", chestSkeleton);
            SetRef(so, "chestAnimation", animation);
            SetRef(so, "itemDisplayRoot", itemRoot);
            SetRef(so, "itemIcon", iconImg);
            SetRef(so, "itemNameText", nameTmp);
            SetRef(so, "itemQuantityText", qtyTmp);
            SetRef(so, "claimButton", claimBtn);
            SetRef(so, "revealController", reveal);
            if (tapTmp != null)
                SetRef(so, "tapHint", tapTmp.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();

            popup.gameObject.SetActive(false);
            log.AppendLine("ChestRewardPopup authored under " + parent.name + ".");
            return popup;
        }

        // ------------------------------------------------------------------
        // Spine chest for the reward popup
        // ------------------------------------------------------------------

        private static SkeletonGraphic CreateChestSkeleton(Transform parent, StringBuilder log)
        {
            SkeletonDataAsset data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(ChestSpineDataPath);
            if (data == null)
            {
                log.AppendLine("WARN: Chest spine SkeletonData not found at " + ChestSpineDataPath);
                return null;
            }

            var go = new GameObject("ChestSkeleton", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.transform as RectTransform;
            CenterBox(rt, new Vector2(1200f, 1200f), new Vector2(0f, -60f));
            rt.localScale = new Vector3(0.35f, 0.35f, 1f);

            SkeletonGraphic skeleton = go.AddComponent<SkeletonGraphic>();
            var so = new SerializedObject(skeleton);
            SetRef(so, "skeletonDataAsset", data);
            SerializedProperty anim = so.FindProperty("startingAnimation");
            if (anim != null) anim.stringValue = "ruong";
            SerializedProperty loop = so.FindProperty("startingLoop");
            if (loop != null) loop.boolValue = true;
            SerializedProperty ray = so.FindProperty("raycastTarget");
            if (ray != null) ray.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("Chest skeleton (Spine) added: ruong/open/hold.");
            return skeleton;
        }

        // ------------------------------------------------------------------
        // Win / Lose result UI wiring
        // ------------------------------------------------------------------

        private static BattleResultUI WireResultUI(Transform popup, bool addGoldRow, StringBuilder log, string label)
        {
            if (popup == null)
                return null;

            BattleResultUI ui = popup.GetComponent<BattleResultUI>();
            if (ui == null)
                ui = popup.gameObject.AddComponent<BattleResultUI>();

            var so = new SerializedObject(ui);

            TextMeshProUGUI expText = FindLabelText(popup, "Xp_Text");
            TextMeshProUGUI timeText = FindLabelText(popup, "TImer");
            Slider expSlider = FindSliderIn(popup, "Xp_Slide");
            Transform grid = FindDescendant(popup, "Hienthivp");
            Transform itemsRoot = FindDescendant(popup, "Vat_Pham");
            Transform returnBtn = FindDescendant(popup, "Return");
            Transform firstSlot = grid != null && grid.childCount > 0 ? grid.GetChild(0) : null;
            Button returnButton = returnBtn != null ? returnBtn.GetComponent<Button>() : null;
            if (returnButton == null && returnBtn != null)
            {
                returnButton = returnBtn.gameObject.AddComponent<Button>();
                Image target = returnBtn.GetComponent<Image>();
                if (target == null)
                    target = returnBtn.gameObject.AddComponent<Image>();
                returnButton.targetGraphic = target;
                returnButton.transition = Selectable.Transition.None;
            }

            SetRef(so, "expText", expText);
            SetRef(so, "timeText", timeText);
            SetRef(so, "expBar", expSlider);
            if (grid != null) SetRef(so, "itemGrid", grid);
            if (firstSlot != null) SetRef(so, "itemSlotPrefab", firstSlot.gameObject);
            if (itemsRoot != null) SetRef(so, "itemsRoot", itemsRoot.gameObject);
            if (returnButton != null) SetRef(so, "returnButton", returnButton);
            if (addGoldRow)
            {
                TextMeshProUGUI goldText = EnsureGoldText(popup);
                SetRef(so, "goldText", goldText);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine(label + " wired with BattleResultUI.");
            return ui;
        }

        /// <summary>Creates a "Gold" container with a TMP child (mirrors TImer).</summary>
        private static TextMeshProUGUI EnsureGoldText(Transform popup)
        {
            Transform gold = FindDescendant(popup, "Gold");
            if (gold == null)
            {
                var go = new GameObject("Gold");
                go.transform.SetParent(popup, false);
                gold = go.transform;
                var rt = gold as RectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(400f, 60f);
                rt.anchoredPosition = new Vector2(0f, -330f);
            }

            Transform txt = FindDescendant(gold, "Text");
            TextMeshProUGUI sample = FindSampleText(popup);
            TextMeshProUGUI result;
            if (txt != null)
            {
                result = txt.GetComponent<TextMeshProUGUI>();
            }
            else if (sample != null)
            {
                RectTransform container = gold as RectTransform;
                var nameGo = new GameObject("Text");
                nameGo.transform.SetParent(container, false);
                RectTransform nameRt = nameGo.AddComponent<RectTransform>();
                Stretch(nameRt);
                result = nameGo.AddComponent<TextMeshProUGUI>();
                result.font = sample.font;
                result.fontStyle = sample.fontStyle;
                result.fontSize = sample.fontSize;
                result.color = Color.white;
                result.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                return null;
            }

            if (result != null)
            {
                result.alignment = TextAlignmentOptions.Center;
                result.raycastTarget = false;
            }
            return result;
        }

        // ------------------------------------------------------------------
        // Manager wiring
        // ------------------------------------------------------------------

        private static void WireManager(
            BattleResultFlowController controller,
            RectTransform chestPopup,
            Transform winT,
            Transform loseT,
            BattleResultUI winUI,
            BattleResultUI loseUI,
            Scene scene)
        {
            var so = new SerializedObject(controller);
            ChestRewardController chestController = controller.GetComponent<ChestRewardController>();
            SetRef(so, "chestRewardPopup", chestPopup != null ? chestPopup.gameObject : null);
            if (chestPopup != null)
                SetRef(so, "chestRewardUI", chestPopup.GetComponent<ChestRewardUI>());
            SetRef(so, "winPopup", winT != null ? winT.gameObject : null);
            SetRef(so, "losePopup", loseT != null ? loseT.gameObject : null);
            SetRef(so, "winUI", winUI);
            SetRef(so, "loseUI", loseUI);
            SetRef(so, "chestRewardController", chestController);
            SerializedProperty victoryDelay = so.FindProperty("victoryDelay");
            if (victoryDelay != null) victoryDelay.floatValue = 2f;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (chestController != null)
            {
                var chestSo = new SerializedObject(chestController);
                SetRef(chestSo, "interactionLayer", chestPopup != null ? chestPopup.gameObject : null);
                chestSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ------------------------------------------------------------------
        // Small helpers
        // ------------------------------------------------------------------

        private static void SetRef(SerializedObject so, string field, Object value)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null) return;
            prop.objectReferenceValue = value;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;
            return go.transform as RectTransform;
        }

        private static Image CreateImage(string name, RectTransform parent, System.Action<RectTransform> layout)
        {
            RectTransform rt = CreateRect(name, parent);
            if (layout != null) layout(rt);
            Image img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
        }

        private static void CenterBox(RectTransform rt, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            rt.localScale = Vector3.one;
        }

        private static TextMeshProUGUI CloneText(TextMeshProUGUI sample, RectTransform parent, string name)
        {
            var go = Object.Instantiate(sample.gameObject);
            go.name = name;
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            var rt = go.transform as RectTransform;
            Stretch(rt);
            tmp.text = string.Empty;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void Style(TextMeshProUGUI tmp, float size)
        {
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.color = Color.white;
        }

        private static TextMeshProUGUI FindLabelText(Transform root, string parentName)
        {
            TextMeshProUGUI best = null;
            Walk(root, t =>
            {
                if (best != null) return;
                TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
                if (tmp == null) return;
                Transform cur = t;
                while (cur != null && cur != root)
                {
                    if (Normalize(cur.name) == Normalize(parentName))
                    {
                        best = tmp;
                        return;
                    }
                    cur = cur.parent;
                }
            });
            return best;
        }

        private static Slider FindSliderIn(Transform root, string name)
        {
            Slider best = null;
            Walk(root, t =>
            {
                if (best != null) return;
                Slider slider = t.GetComponent<Slider>();
                if (slider == null) return;
                Transform cur = t;
                while (cur != null && cur != root)
                {
                    if (Normalize(cur.name) == Normalize(name))
                    {
                        best = slider;
                        return;
                    }
                    cur = cur.parent;
                }
            });
            return best;
        }

        private static TextMeshProUGUI FindSampleText(Transform root)
        {
            TextMeshProUGUI sample = null;
            Walk(root, t =>
            {
                if (sample != null) return;
                TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
                if (tmp != null && tmp.font != null)
                    sample = tmp;
            });
            return sample;
        }

        private static Transform FindRootChild(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (Normalize(root.name) == Normalize(name))
                    return root.transform;
                Transform found = FindDescendant(root.transform, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null) return null;
            Transform result = null;
            Walk(root, t =>
            {
                if (result != null) return;
                if (t != root && Normalize(t.name) == Normalize(name))
                    result = t;
            });
            return result;
        }

        private static void Walk(Transform root, System.Action<Transform> visitor)
        {
            if (root == null) return;
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
            var buffer = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c))
                    buffer.Append(char.ToLowerInvariant(c));
            }
            return buffer.ToString();
        }
    }
}
