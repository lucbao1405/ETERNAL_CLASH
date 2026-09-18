using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using EternalClash.UI;

namespace EternalClash.EditorTools
{
    /// <summary>
    /// Dat san popup "Rest &amp; Recover" (hoi mau bang kim cuong) vao scene Town:
    /// tao panel Rest_Recover + nut mo Rest_Recover_Open bang art co san cua
    /// project, gan component RestRecoverPopup / RestRecoverDriver va noi day
    /// deu. Chay menu Tools/Setup Rest &amp; Recover Popup (Town) mot lan; sau do
    /// gia kim cuong, text, sprite... sua truc tiep trong Inspector cua scene.
    /// </summary>
    public static class RestRecoverSceneSetup
    {
        private const string TownScenePath = "Assets/Game/Scenes/Town.unity";
        private const string PanelName = "Rest_Recover";
        private const string OpenButtonName = "Rest_Recover_Open";

        // Art co san dung cho popup.
        private const string PanelSpritePath = "Assets/Ui/Town/info/khung_Lon.png";
        private const string ButtonSpritePath = "Assets/Ui/Battle/WINLOSE/name frame loot.png";
        private const string GemSpritePath = "Assets/Ui/Town/Vatpham/diamond.png";
        private const string CloseSpritePath = "Assets/Ui/Town/x (1).png";
        private const string BubbleSpritePath = "Assets/Ui/Town/Exten,Button/buff blood.png";

        [MenuItem("Tools/Setup Rest & Recover Popup (Town)", false, 120)]
        public static void Setup()
        {
            if (!EnsureTownSceneOpen())
                return;

            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[RestRecoverSetup] Khong tim thay Canvas trong scene Town.");
                return;
            }

            RestRecoverPopup popup = BuildPanel(canvas.transform);
            BuildOpenButton(canvas.transform);
            AttachDriver(canvas);

            EditorUtility.SetDirty(popup);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>[RestRecoverSetup] Da tao popup Rest & Recover trong Town (Rest_Recover + Rest_Recover_Open).</color>");
            Selection.activeGameObject = popup.gameObject;
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
                    Debug.LogWarning("[RestRecoverSetup] Can save scene hien tai truoc khi mo Town.");
                    return false;
                }
            }

            EditorSceneManager.OpenScene(TownScenePath, OpenSceneMode.Single);
            return true;
        }

        // ------------------------------------------------------------------
        // Panel Rest_Recover (an mac dinh)
        // ------------------------------------------------------------------

        private static RestRecoverPopup BuildPanel(Transform canvas)
        {
            Transform existing = canvas.Find(PanelName);
            if (existing != null)
            {
                RestRecoverPopup existingPopup = existing.GetComponent<RestRecoverPopup>();
                if (existingPopup == null)
                    existingPopup = existing.gameObject.AddComponent<RestRecoverPopup>();
                return existingPopup;
            }

            GameObject root = new GameObject(PanelName, typeof(RectTransform));
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.SetParent(canvas, false);
            StretchFull(rootRect);
            rootRect.SetAsLastSibling();
            root.SetActive(false);

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(rootRect, false);
            AnchorCenter(panelRect);
            panelRect.sizeDelta = new Vector2(900f, 620f);
            panelRect.anchoredPosition = new Vector2(0f, -10f);
            panel.GetComponent<Image>().sprite = LoadSprite(PanelSpritePath);

            CreateLabel(panelRect, "Title", "REST & RECOVER", 58, FontStyles.Bold,
                new Color(0.93f, 0.63f, 0.2f), new Vector2(0f, 210f), new Vector2(700f, 90f));
            CreateLabel(panelRect, "Message",
                "Listen to some tales as you rest up to recover your health points?",
                38, FontStyles.Bold, new Color(0.36f, 0.22f, 0.1f),
                new Vector2(0f, 60f), new Vector2(680f, 220f));

            // Nut tra kim cuong: so + icon diamond (kieu Postknight).
            Button healButton = CreateButton(panelRect, "HealButton",
                ButtonSpritePath, new Vector2(0f, -125f), new Vector2(460f, 120f));
            RectTransform healRect = (RectTransform)healButton.transform;

            TMP_Text costText = CreateLabel(healRect, "CostText", "5", 60, FontStyles.Bold,
                Color.white, new Vector2(-55f, 0f), new Vector2(200f, 90f));

            GameObject gem = new GameObject("GemIcon", typeof(RectTransform), typeof(Image));
            RectTransform gemRect = (RectTransform)gem.transform;
            gemRect.SetParent(healRect, false);
            AnchorCenter(gemRect);
            gemRect.sizeDelta = new Vector2(100f, 95f);
            gemRect.anchoredPosition = new Vector2(80f, 0f);
            Image gemImage = gem.GetComponent<Image>();
            gemImage.sprite = LoadSprite(GemSpritePath);
            gemImage.preserveAspect = true;
            gemImage.raycastTarget = false;

            Button closeButton = CreateButton(panelRect, "CloseButton",
                CloseSpritePath, new Vector2(0f, -360f), new Vector2(150f, 150f));

            RestRecoverPopup popup = root.AddComponent<RestRecoverPopup>();
            SerializedObject serialized = new SerializedObject(popup);
            serialized.FindProperty("gemCost").intValue = 5;
            serialized.FindProperty("costText").objectReferenceValue = costText;
            serialized.FindProperty("healButton").objectReferenceValue = healButton;
            serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        // ------------------------------------------------------------------
        // Nut mo (hien khi bi thuong, driver quan ly hien/an)
        // ------------------------------------------------------------------

        private static void BuildOpenButton(Transform canvas)
        {
            if (canvas.Find(OpenButtonName) != null)
                return;

            Button bubble = CreateButton(null, OpenButtonName, BubbleSpritePath,
                new Vector2(0f, -240f), new Vector2(150f, 170f));
            RectTransform rect = (RectTransform)bubble.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -240f);
            rect.SetAsLastSibling();
            bubble.gameObject.SetActive(false);
        }

        private static void AttachDriver(GameObject canvas)
        {
            if (canvas.GetComponent<RestRecoverDriver>() != null)
                return;

            canvas.AddComponent<RestRecoverDriver>();
            EditorUtility.SetDirty(canvas);
        }

        // ------------------------------------------------------------------
        // UI helpers
        // ------------------------------------------------------------------

        private static Button CreateButton(Transform parent, string name,
            string spritePath, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = (RectTransform)buttonObject.transform;
            if (parent != null)
                rect.SetParent(parent, false);
            AnchorCenter(rect);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = LoadSprite(spritePath);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static TMP_Text CreateLabel(Transform parent, string name, string text,
            float fontSize, FontStyles style, Color color, Vector2 position, Vector2 size)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = (RectTransform)labelObject.transform;
            rect.SetParent(parent, false);
            AnchorCenter(rect);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.enableWordWrapping = true;
            label.raycastTarget = false;
            return label;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AnchorCenter(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogError($"[RestRecoverSetup] Khong tim thay sprite: {path}");
            return sprite;
        }
    }
}
