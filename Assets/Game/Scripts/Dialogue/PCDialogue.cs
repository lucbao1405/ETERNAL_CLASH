using System;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button)), DisallowMultipleComponent]
public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private string characterName;
    [SerializeField] private Sprite avatar;

    [SerializeField, TextArea(2, 5)]
    private string[] dialogueLines;

    [Header("After Dialogue")]
    [Tooltip("Panel opened after the final dialogue line. If empty, the script searches for Shop_<NPC object name>.")]
    [SerializeField] private GameObject panelAfterDialogue;
    [SerializeField] private bool hidePanelAtStart = true;

    [Header("Click Area")]
    [Tooltip("Fallback size used when the NPC is not a Spine SkeletonGraphic.")]
    [SerializeField] private Vector2 clickAreaSize = new Vector2(2600f, 2800f);
    [Tooltip("Fallback offset used when the NPC is not a Spine SkeletonGraphic.")]
    [SerializeField] private Vector2 clickAreaOffset = new Vector2(110f, 1340f);
    [SerializeField] private Vector2 clickAreaPadding = new Vector2(100f, 100f);

    private Button npcButton;

    private void Awake()
    {
        // The NPC in this project is already a UI Button. Registering here means
        // there is no fragile On Click entry to configure for every NPC.
        npcButton = GetComponent<Button>();
        if (npcButton == null)
            npcButton = gameObject.AddComponent<Button>();

        if (npcButton.targetGraphic == null)
            npcButton.targetGraphic = GetComponent<Graphic>();

        npcButton.enabled = true;
        npcButton.interactable = true;
        npcButton.onClick.AddListener(ShowDialogue);
        CreateTransparentClickArea();
        ResolvePanelAfterDialogue();

        if (panelAfterDialogue != null && hidePanelAtStart)
            panelAfterDialogue.SetActive(false);
    }

    private void OnDestroy()
    {
        if (npcButton != null)
            npcButton.onClick.RemoveListener(ShowDialogue);
    }

    public void ShowDialogue()
    {
        Debug.Log($"[Dialogue] NPC '{name}' was clicked.", this);

        if (dialogueManager == null)
            dialogueManager = FindObjectOfType<DialogueManager>();

        if (dialogueManager == null)
        {
            Debug.LogError($"No DialogueManager was found for NPC '{name}'.", this);
            return;
        }

        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning($"NPC '{name}' has no dialogue lines.", this);
            return;
        }

        ResolvePanelAfterDialogue();

        dialogueManager.OpenDialogue(
            characterName,
            avatar,
            dialogueLines,
            panelAfterDialogue
        );
    }

    private void CreateTransparentClickArea()
    {
        const string clickAreaName = "Dialogue Click Area";
        Transform existingClickArea = transform.Find(clickAreaName);
        GameObject clickAreaObject;

        if (existingClickArea != null)
        {
            clickAreaObject = existingClickArea.gameObject;
        }
        else
        {
            clickAreaObject = new GameObject(
                clickAreaName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            clickAreaObject.transform.SetParent(transform, false);
        }

        Vector2 resolvedClickAreaSize = clickAreaSize;
        Vector2 resolvedClickAreaOffset = clickAreaOffset;
        TryGetSpineClickArea(out resolvedClickAreaSize, out resolvedClickAreaOffset);

        RectTransform clickAreaRect = clickAreaObject.GetComponent<RectTransform>();
        clickAreaRect.anchorMin = new Vector2(0.5f, 0.5f);
        clickAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
        clickAreaRect.pivot = new Vector2(0.5f, 0.5f);
        clickAreaRect.anchoredPosition = resolvedClickAreaOffset;
        clickAreaRect.sizeDelta = resolvedClickAreaSize + clickAreaPadding;
        clickAreaRect.SetAsLastSibling();

        Image clickAreaImage = clickAreaObject.GetComponent<Image>();
        clickAreaImage.color = new Color(1f, 1f, 1f, 0f);
        clickAreaImage.raycastTarget = true;
    }

    private bool TryGetSpineClickArea(out Vector2 size, out Vector2 offset)
    {
        size = clickAreaSize;
        offset = clickAreaOffset;

        SkeletonGraphic skeletonGraphic = GetComponent<SkeletonGraphic>();
        if (skeletonGraphic == null || skeletonGraphic.SkeletonDataAsset == null)
            return false;

        Spine.SkeletonData skeletonData = skeletonGraphic.SkeletonDataAsset.GetSkeletonData(true);
        if (skeletonData == null)
            return false;

        size = new Vector2(
            skeletonData.Width,
            skeletonData.Height
        );
        offset = new Vector2(
            skeletonData.X + skeletonData.Width * 0.5f,
            skeletonData.Y + skeletonData.Height * 0.5f
        );
        return true;
    }

    private void ResolvePanelAfterDialogue()
    {
        if (panelAfterDialogue != null)
            return;

        string expectedPanelName = $"Shop_{gameObject.name}";
        GameObject[] sceneObjects = FindObjectsOfType<GameObject>(true);
        foreach (GameObject sceneObject in sceneObjects)
        {
            if (sceneObject.name.Equals(expectedPanelName, StringComparison.OrdinalIgnoreCase))
            {
                panelAfterDialogue = sceneObject;
                return;
            }
        }
    }
}
