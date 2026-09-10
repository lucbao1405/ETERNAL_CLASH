using System;
using EternalClash.Data;
using EternalClash.Dialogue;
using EternalClash.Upgrade;
using EternalClash.Village;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EternalClash.UI
{
    /// <summary>Owns the two-tap blacksmith upgrade flow and its confirmation UI.</summary>
    [DisallowMultipleComponent]
    public sealed class BlacksmithUpgradeFlowController : MonoBehaviour
    {
        [SerializeField] private TutorialDialogueData sageUpgradeDialogue;

        private Canvas rootCanvas;
        private GameObject sagePanel;
        private GameObject confirmationPanel;
        private ItemSlot pendingSlot;

        public void BeginUpgrade(ItemSlot slot, Action onComplete)
        {
            pendingSlot = slot;
            if (sageUpgradeDialogue == null)
            {
                sageUpgradeDialogue = Resources.Load<TutorialDialogueData>(
                    "TutorialDialogues/SageBlacksmithUpgrade");
            }

            if (sageUpgradeDialogue == null || !sageUpgradeDialogue.IsValid)
            {
                Debug.LogError("Missing TutorialDialogueData 'sage_blacksmith_upgrade_01'.", this);
                return;
            }

            ShowSageDialogue(() => ShowConfirmation(onComplete));
        }

        private void ShowSageDialogue(Action onContinue)
        {
            EnsureCanvas();
            DestroyPanel(ref sagePanel);
            sagePanel = CreatePanel("SageUpgradeDialogue", new Color(0.04f, 0.06f, 0.1f, 0.94f));

            string dialogue = string.Join("\n\n", sageUpgradeDialogue.Lines);
            CreateText(sagePanel.transform, "Speaker", sageUpgradeDialogue.NpcName, 28, TextAnchor.UpperCenter,
                new Vector2(0f, 150f), new Vector2(760f, 50f));
            CreateText(sagePanel.transform, "Dialogue", dialogue, 21, TextAnchor.UpperLeft,
                new Vector2(0f, 25f), new Vector2(760f, 190f));
            CreateButton(sagePanel.transform, "ContinueButton", "Continue", new Vector2(-125f, -165f),
                () =>
                {
                    DestroyPanel(ref sagePanel);
                    onContinue?.Invoke();
                });
            CreateButton(sagePanel.transform, "CancelButton", "Cancel", new Vector2(125f, -165f),
                () => DestroyPanel(ref sagePanel));
        }

        private void ShowConfirmation(Action onComplete)
        {
            EnsureCanvas();
            DestroyPanel(ref confirmationPanel);
            confirmationPanel = CreatePanel("ConfirmUpgradePopup", new Color(0.08f, 0.08f, 0.08f, 0.96f));

            BlacksmithCraftingSystem smith = BlacksmithCraftingSystem.Instance;
            ItemData item = EquipmentSystem.Instance?.GetEquippedItem(pendingSlot);
            int currentTier = item != null ? item.upgradeLevel : 0;
            int nextTier = currentTier + 1;
            UpgradeRecipeData recipe = smith?.GetRecipe(pendingSlot);
            int gold = recipe != null ? recipe.goldCost : 0;
            int ore = GetRequirement(recipe, MaterialType.Ore);
            int leather = GetRequirement(recipe, MaterialType.Leather);
            int wood = GetRequirement(recipe, MaterialType.Wood);

            CreateText(confirmationPanel.transform, "Title", "Upgrade Confirmation", 28, TextAnchor.UpperCenter,
                new Vector2(0f, 165f), new Vector2(760f, 45f));
            CreateItemIcon(confirmationPanel.transform, item, new Vector2(-275f, 65f));
            CreateText(confirmationPanel.transform, "UpgradePreview",
                string.Format("{0}\nLevel {1}  ->  Level {2}\n{3}", GetSlotName(), currentTier, nextTier, FormatStats(item, currentTier, nextTier)),
                20, TextAnchor.UpperLeft, new Vector2(90f, 70f), new Vector2(500f, 140f));
            CreateText(confirmationPanel.transform, "MaterialRequire",
                string.Format("Required Materials\nOre: {0}\nLeather: {1}\nWood: {2}", ore, leather, wood), 20, TextAnchor.UpperLeft,
                new Vector2(-190f, -80f), new Vector2(310f, 115f));
            CreateText(confirmationPanel.transform, "GoldCost", "Gold Cost: " + gold, 20, TextAnchor.UpperLeft,
                new Vector2(205f, -55f), new Vector2(310f, 45f));

            Text errorText = CreateText(confirmationPanel.transform, "ErrorText", string.Empty, 18, TextAnchor.MiddleCenter,
                new Vector2(0f, -125f), new Vector2(600f, 35f));
            errorText.color = new Color(1f, 0.4f, 0.4f);
            CreateButton(confirmationPanel.transform, "ConfirmButton", "Confirm", new Vector2(-125f, -175f), () =>
            {
                bool upgraded = pendingSlot == ItemSlot.Weapon
                    ? smith != null && smith.TryUpgradeWeapon()
                    : smith != null && smith.TryUpgradeArmor();
                if (!upgraded)
                {
                    errorText.text = "Not enough materials.";
                    return;
                }

                DestroyPanel(ref confirmationPanel);
                onComplete?.Invoke();
            });
            CreateButton(confirmationPanel.transform, "CancelButton", "Cancel", new Vector2(125f, -175f),
                () => DestroyPanel(ref confirmationPanel));
        }

        private void EnsureCanvas()
        {
            if (rootCanvas != null)
                return;
            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
                rootCanvas = FindObjectOfType<Canvas>();
            if (rootCanvas == null)
                throw new InvalidOperationException("Blacksmith upgrade flow requires a Canvas.");
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private GameObject CreatePanel(string panelName, Color color)
        {
            GameObject panel = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(rootCanvas.transform, false);
            panel.transform.SetAsLastSibling();
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(850f, 460f);
            Image image = panel.GetComponent<Image>();
            image.color = color;
            return panel;
        }

        private static Text CreateText(Transform parent, string objectName, string value, int fontSize,
            TextAnchor alignment, Vector2 position, Vector2 size)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        private static void CreateItemIcon(Transform parent, ItemData item, Vector2 position)
        {
            GameObject iconObject = new GameObject("ItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(120f, 120f);
            Image image = iconObject.GetComponent<Image>();
            image.sprite = item != null ? item.icon : null;
            image.color = image.sprite != null ? Color.white : new Color(0.2f, 0.2f, 0.2f);
            if (image.sprite == null)
                CreateText(iconObject.transform, "Placeholder", "No Item", 16, TextAnchor.MiddleCenter, Vector2.zero, rect.sizeDelta);
        }

        private static void CreateButton(Transform parent, string objectName, string label, Vector2 position, Action onClick)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(190f, 55f);
            buttonObject.GetComponent<Image>().color = new Color(0.28f, 0.45f, 0.65f);
            buttonObject.GetComponent<Button>().onClick.AddListener(() => onClick());
            CreateText(buttonObject.transform, "Label", label, 20, TextAnchor.MiddleCenter, Vector2.zero, rect.sizeDelta);
        }

        private static void DestroyPanel(ref GameObject panel)
        {
            if (panel != null)
                Destroy(panel);
            panel = null;
        }

        private string GetSlotName() => pendingSlot == ItemSlot.Weapon ? "Weapon" : "Armor";

        private static int GetRequirement(UpgradeRecipeData recipe, MaterialType type)
        {
            if (recipe?.requiredMaterials == null)
                return 0;
            foreach (UpgradeMaterialRequirement requirement in recipe.requiredMaterials)
                if (requirement.materialType == type)
                    return requirement.amount;
            return 0;
        }

        private static string FormatStats(ItemData item, int currentTier, int nextTier)
        {
            int bonus = (nextTier - currentTier) * 10;
            if (item == null)
                return "+" + bonus + " weapon power";
            return string.Format("STR {0} -> {1}\nINT {2} -> {3}\nVIT {4} -> {5}\nLUCK {6} -> {7}",
                item.strBonus, item.strBonus + bonus, item.intBonus, item.intBonus + bonus,
                item.vitBonus, item.vitBonus + bonus, item.luckBonus, item.luckBonus + bonus);
        }
    }
}
