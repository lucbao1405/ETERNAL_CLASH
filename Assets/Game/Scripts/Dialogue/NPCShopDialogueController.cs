using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;

[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public sealed class NPCShopDialogueController : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private string dialogueId;
    [SerializeField] private string characterName;
    [SerializeField, TextArea(2, 5)] private string[] dialogueLines;
    [SerializeField] private Sprite avatar;

    [Header("Shop")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private ShopPanelAnimator shopPanelAnimator;

    [Header("Click Area")]
    [SerializeField] private Vector2 clickAreaSize = new Vector2(2600f, 2800f);
    [SerializeField] private Vector2 clickAreaOffset = new Vector2(110f, 1340f);
    [SerializeField] private Vector2 clickAreaPadding = new Vector2(100f, 100f);

    private Button npcButton;
    private bool waitingForDialogue;
    private bool dialogueSubscribed;
    private static NPCShopDialogueController activeConversation;

    private void Awake()
    {
        npcButton = GetComponent<Button>();
        npcButton.enabled = true;
        npcButton.interactable = true;
        npcButton.onClick.AddListener(StartConversation);
        CreateTransparentClickArea();
        dialogueManager ??= FindObjectOfType<DialogueManager>();
        shopPanelAnimator ??= shopPanel != null ? shopPanel.GetComponent<ShopPanelAnimator>() : null;
        if (shopPanelAnimator == null && shopPanel != null)
            shopPanelAnimator = shopPanel.AddComponent<ShopPanelAnimator>();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CreateTransparentClickArea();
        if (dialogueManager != null && !dialogueSubscribed)
        {
            dialogueManager.DialogueCompleted += OnDialogueComplete;
            dialogueSubscribed = true;
        }
    }

    private void OnDisable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueCompleted -= OnDialogueComplete;
        dialogueSubscribed = false;
        waitingForDialogue = false;
        if (activeConversation == this)
            activeConversation = null;
    }

    private void OnDestroy()
    {
        if (npcButton != null)
            npcButton.onClick.RemoveListener(StartConversation);
    }

    public void StartConversation()
    {
        ResolveReferences();
        if (waitingForDialogue || activeConversation != null ||
            (dialogueManager != null && dialogueManager.IsOpen))
            return;

        // NPC chi mo hoi thoai/panel khi nhan vat da chay den dung Page chua
        // NPC do (tho ren o Page_2, phu thuy o Page_3).
        if (!TownNpcPageGate.CanOpenNpcPanel(this))
            return;

        if (dialogueManager == null || dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogError($"Shop dialogue '{dialogueId}' is not configured on '{name}'.", this);
            return;
        }

        shopPanelAnimator?.CloseImmediate();
        activeConversation = this;
        waitingForDialogue = true;
        dialogueManager.OpenDialogue(characterName, avatar, dialogueLines);
    }

    public void OnDialogueComplete()
    {
        if (!waitingForDialogue)
            return;

        waitingForDialogue = false;
        if (activeConversation == this)
            activeConversation = null;
        OpenShop();
    }

    public void OpenShop()
    {
        ResolveReferences();
        if (shopPanelAnimator != null)
            shopPanelAnimator.Open();
        else if (shopPanel != null)
            shopPanel.SetActive(true);
    }

    public string DialogueId => dialogueId;

    private void ResolveReferences()
    {
        dialogueManager ??= FindObjectOfType<DialogueManager>();
        shopPanelAnimator ??= shopPanel != null
            ? shopPanel.GetComponent<ShopPanelAnimator>()
            : null;
    }

    private void CreateTransparentClickArea()
    {
        const string clickAreaName = "Shop Dialogue Click Area";
        Transform existing = transform.Find(clickAreaName);
        GameObject clickArea = existing != null
            ? existing.gameObject
            : new GameObject(clickAreaName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        if (existing == null)
            clickArea.transform.SetParent(transform, false);

        Vector2 resolvedSize = clickAreaSize;
        Vector2 resolvedOffset = clickAreaOffset;
        SkeletonGraphic skeleton = GetComponent<SkeletonGraphic>();
        if (skeleton != null && skeleton.SkeletonDataAsset != null)
        {
            Spine.SkeletonData data = skeleton.SkeletonDataAsset.GetSkeletonData(true);
            if (data != null)
            {
                // Spine bounds are already expressed in this UI hierarchy's local
                // units. Do not multiply by Canvas pixels-per-unit.
                resolvedSize = new Vector2(data.Width, data.Height);
                resolvedOffset = new Vector2(
                    data.X + data.Width * 0.5f,
                    data.Y + data.Height * 0.5f
                );
            }
        }

        RectTransform rect = clickArea.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = resolvedOffset;
        rect.sizeDelta = resolvedSize + clickAreaPadding;
        rect.SetAsLastSibling();

        Image image = clickArea.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;
        Button clickButton = clickArea.GetComponent<Button>();
        if (clickButton == null)
            clickButton = clickArea.AddComponent<Button>();
        clickButton.transition = Selectable.Transition.None;
        clickButton.targetGraphic = image;
        clickButton.onClick.RemoveListener(StartConversation);
        clickButton.onClick.AddListener(StartConversation);
    }
}
