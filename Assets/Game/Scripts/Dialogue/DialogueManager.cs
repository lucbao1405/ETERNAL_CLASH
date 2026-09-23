using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DialogueManager : MonoBehaviour
{
    public event System.Action DialogueCompleted;
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Typing")]
    [SerializeField, Min(0f)] private float charactersPerSecond = 40f;

    [Header("Slide Animation")]
    [Tooltip("Anchored position while visible. Leave (0, 0) to use the panel's editor position (where it sits on screen) as the shown position.")]
    [SerializeField] private Vector2 shownAnchoredPosition;
    [Tooltip("How far above the shown position the panel waits while hidden.")]
    [SerializeField, Min(0f)] private float hiddenOffset = 525f;
    [SerializeField, Min(0f)] private float slideDuration = 0.35f;

    [Header("Input")]
    [Tooltip("Adds a click handler to the dialogue panel at runtime.")]
    [SerializeField] private bool panelClickAdvances = true;

    private string[] lines;
    private int currentLine;
    private RectTransform panelRectTransform;
    private Button panelButton;
    private Button touchButton;
    private Coroutine typingCoroutine;
    private Coroutine slideCoroutine;
    private bool isTyping;
    private bool isOpen;

    /// <summary>Hop hoi thoai dang hien. Dung cho nut Back Android.</summary>
    public bool IsOpen => isOpen;

    /// <summary>
    /// Awake da chay va tham chieu panel hop le. Ban sao DialogueManager tren
    /// prefab "thoai" nam tren object bi tat nen khong bao gio Awake: panel van
    /// null va moi OpenDialogue se that bai im lang.
    /// </summary>
    public bool IsInitialized => dialoguePanel != null;
    private GameObject panelToOpenAfterDialogue;

    private Vector2 HiddenAnchoredPosition =>
        shownAnchoredPosition + Vector2.up * hiddenOffset;

    private Vector2 hiddenPosition;

    private void Awake()
    {
        EnsureTouchInput();
        AutoAssignReferences();

        if (!ValidateReferences())
            return;

        panelRectTransform = dialoguePanel.GetComponent<RectTransform>();

        // Panel dat san TRONG man hinh trong editor: do la diem ket thuc khi mo,
        // vi tri an = day het panel len tren canh canvas. Panel van dat NGOAI
        // man hinh trong editor thi coi do la vi tri an nhu cach dat cu.
        if (shownAnchoredPosition == Vector2.zero)
        {
            if (PanelPlacement.TryDerive(panelRectTransform,
                    out shownAnchoredPosition, out Vector2 derivedHiddenPos))
            {
                hiddenPosition = derivedHiddenPos;
            }
            else
            {
                shownAnchoredPosition = panelRectTransform.anchoredPosition
                    - Vector2.up * hiddenOffset;
                hiddenPosition = HiddenAnchoredPosition;
            }
        }
        else
        {
            hiddenPosition = HiddenAnchoredPosition;
        }

        panelRectTransform.anchoredPosition = hiddenPosition;

        if (panelClickAdvances)
        {
            panelButton = dialoguePanel.GetComponent<Button>();
            if (panelButton == null)
                panelButton = dialoguePanel.AddComponent<Button>();

            panelButton.transition = Selectable.Transition.None;
            Graphic panelGraphic = dialoguePanel.GetComponent<Graphic>();
            if (panelGraphic == null)
            {
                panelGraphic = dialoguePanel.AddComponent<Image>();
                panelGraphic.color = new Color(0f, 0f, 0f, 0f);
            }
            panelGraphic.raycastTarget = true;
            panelButton.targetGraphic = panelGraphic;
            panelButton.onClick.AddListener(AdvanceDialogue);

            GameObject touchTarget = new GameObject("DialogueTouchTarget", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            touchTarget.transform.SetParent(dialoguePanel.transform, false);
            RectTransform touchRect = touchTarget.GetComponent<RectTransform>();
            touchRect.anchorMin = Vector2.zero;
            touchRect.anchorMax = Vector2.one;
            touchRect.offsetMin = Vector2.zero;
            touchRect.offsetMax = Vector2.zero;
            touchTarget.transform.SetAsLastSibling();
            Image touchImage = touchTarget.GetComponent<Image>();
            touchImage.color = new Color(0f, 0f, 0f, 0f);
            touchButton = touchTarget.GetComponent<Button>();
            touchButton.transition = Selectable.Transition.None;
            touchButton.targetGraphic = touchImage;
            touchButton.onClick.AddListener(AdvanceDialogue);
        }

        // Keep the dialogue panel above transparent/runtime UI overlays on
        // touch devices so the first tap advances the conversation.
        dialoguePanel.transform.SetAsLastSibling();

        dialoguePanel.SetActive(false);
    }

    private static void EnsureTouchInput()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void OnDestroy()
    {
        if (panelButton != null)
            panelButton.onClick.RemoveListener(AdvanceDialogue);
        if (touchButton != null)
            touchButton.onClick.RemoveListener(AdvanceDialogue);
    }

    public void OpenDialogue(
        string characterName,
        Sprite avatar,
        string[] newLines,
        GameObject panelAfterDialogue = null
    )
    {
        if (!ValidateReferences())
            return;

        if (newLines == null || newLines.Length == 0)
        {
            Debug.LogWarning("Dialogue cannot be opened because it has no lines.", this);
            return;
        }

        StopRunningAnimations();

        lines = newLines;
        currentLine = 0;
        isOpen = true;
        panelToOpenAfterDialogue = panelAfterDialogue;

        Debug.Log($"[Dialogue] Opening {newLines.Length} line(s) for '{characterName}'.", this);

        dialoguePanel.SetActive(true);
        // Giu nen toi theo CHINH BANG hoi thoai, khong theo script nay: script nam
        // tren Canvas (luon bat) nen neu bang bi tat bang duong khac, khong ai
        // phat hien ra de nha nen toi. Lay bang lam chu so huu thi PanelDim tu bo.
        PanelDim.Acquire(dialoguePanel);
        panelRectTransform = dialoguePanel.GetComponent<RectTransform>();
        panelRectTransform.anchoredPosition = hiddenPosition;

        characterNameText.text = characterName;
        if (avatarImage != null)
        {
            avatarImage.sprite = avatar;
            avatarImage.enabled = avatar != null;
        }

        ShowCurrentLine();
        EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.PanelScroll);
        slideCoroutine = StartCoroutine(SlidePanel(shownAnchoredPosition, false));
    }

    /// <summary>
    /// First click finishes the typing effect. The next click shows the next
    /// sentence. Clicking after the final sentence closes the panel.
    /// </summary>
    public void AdvanceDialogue()
    {
        if (!isOpen)
            return;

        if (isTyping)
        {
            FinishTypingImmediately();
            return;
        }

        NextLine();
    }

    // Kept public so this can still be selected from a Unity Button On Click list.
    public void NextLine()
    {
        if (!isOpen)
            return;

        currentLine++;

        if (lines == null || currentLine >= lines.Length)
        {
            CloseDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (lines == null || currentLine < 0 || currentLine >= lines.Length)
            return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(lines[currentLine]));
    }

    public void CloseDialogue()
    {
        if (!isOpen || dialoguePanel == null)
            return;

        isOpen = false;

        // Nha nen toi NGAY, khong doi animation dong bang chay xong: coroutine do
        // co the bi ngat (doi scene, mo bang khac) khien nen toi ket lai.
        PanelDim.Release(dialoguePanel);

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.PanelScroll);
        slideCoroutine = StartCoroutine(SlidePanel(
            hiddenPosition,
            true,
            panelToOpenAfterDialogue
        ));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = line ?? string.Empty;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int characterCount = dialogueText.textInfo.characterCount;
        if (charactersPerSecond <= 0f || characterCount == 0)
        {
            FinishTypingImmediately();
            yield break;
        }

        float visibleCharacterCount = 0f;
        while (dialogueText.maxVisibleCharacters < characterCount)
        {
            visibleCharacterCount += charactersPerSecond * Time.unscaledDeltaTime;
            dialogueText.maxVisibleCharacters = Mathf.Min(
                Mathf.FloorToInt(visibleCharacterCount),
                characterCount
            );

            yield return null;
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
        typingCoroutine = null;
    }

    private void FinishTypingImmediately()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
    }

    private IEnumerator SlidePanel(
        Vector2 destination,
        bool deactivateAtEnd,
        GameObject panelToActivate = null
    )
    {
        Vector2 start = panelRectTransform.anchoredPosition;

        if (slideDuration <= 0f)
        {
            panelRectTransform.anchoredPosition = destination;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / slideDuration);
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                panelRectTransform.anchoredPosition = Vector2.LerpUnclamped(
                    start,
                    destination,
                    easedProgress
                );

                yield return null;
            }

            panelRectTransform.anchoredPosition = destination;
        }

        slideCoroutine = null;

        if (!deactivateAtEnd)
            yield break;

        PanelDim.Release(dialoguePanel);
        dialoguePanel.SetActive(false);

        DialogueCompleted?.Invoke();

        SwipePageCharacterTravel player = FindObjectOfType<SwipePageCharacterTravel>();
        if (player != null)
            player.FaceDefaultDirection();

        if (panelToActivate != null)
        {
            ShopPanelAnimator shopPanelAnimator = panelToActivate.GetComponent<ShopPanelAnimator>();
            if (shopPanelAnimator != null)
            {
                shopPanelAnimator.Open();
            }
            else
            {
                SmoothSlide smoothSlide = panelToActivate.GetComponent<SmoothSlide>();
                if (smoothSlide != null)
                    smoothSlide.OpenPanel();
                else
                    panelToActivate.SetActive(true);
            }

            panelToActivate.transform.SetAsLastSibling();

            if (panelToActivate.name.StartsWith("Shop"))
                EternalClash.UI.ShopScrollHelper.EnsureScrollRect(panelToActivate);

            Debug.Log($"[Dialogue] Opened panel '{panelToActivate.name}'.", panelToActivate);
        }

        panelToOpenAfterDialogue = null;
    }

    private void StopRunningAnimations()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
            slideCoroutine = null;
        }

        isTyping = false;
    }

    private bool ValidateReferences()
    {
        if (dialoguePanel != null
            && dialoguePanel.GetComponent<RectTransform>() != null
            && characterNameText != null
            && dialogueText != null)
        {
            return true;
        }

        Debug.LogError(
            "DialogueManager is missing a Dialogue Panel, Character Name Text, or Dialogue Text reference.",
            this
        );
        return false;
    }

    private void AutoAssignReferences()
    {
        if (dialoguePanel == null)
        {
            RectTransform[] rectTransforms = GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rectTransform in rectTransforms)
            {
                string objectName = rectTransform.gameObject.name;
                bool isDialogueName = objectName.Equals("thoai", StringComparison.OrdinalIgnoreCase)
                    || objectName.Equals("DialoguePanel", StringComparison.OrdinalIgnoreCase)
                    || objectName.StartsWith("thoai_", StringComparison.OrdinalIgnoreCase);

                if (isDialogueName
                    && rectTransform.GetComponentsInChildren<TMP_Text>(true).Length >= 2)
                {
                    dialoguePanel = rectTransform.gameObject;
                    break;
                }
            }
        }

        if (dialoguePanel == null)
            return;

        TMP_Text[] panelTexts = dialoguePanel.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text panelText in panelTexts)
        {
            string objectName = panelText.gameObject.name;
            bool isNameText = objectName.Equals("Ten_Nhan_Vat", StringComparison.OrdinalIgnoreCase)
                || objectName.Equals("CharacterName", StringComparison.OrdinalIgnoreCase)
                || objectName.Equals("Character Name", StringComparison.OrdinalIgnoreCase);

            if (characterNameText == null && isNameText)
                characterNameText = panelText;
        }

        if (dialogueText == null)
        {
            foreach (TMP_Text panelText in panelTexts)
            {
                if (panelText != characterNameText)
                {
                    dialogueText = panelText;
                    break;
                }
            }
        }

        if (avatarImage == null)
        {
            Image[] panelImages = dialoguePanel.GetComponentsInChildren<Image>(true);
            foreach (Image panelImage in panelImages)
            {
                if (panelImage.gameObject.name.Equals("Avatar", StringComparison.OrdinalIgnoreCase))
                {
                    avatarImage = panelImage;
                    break;
                }
            }
        }
    }
}
