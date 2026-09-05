using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Typing")]
    [SerializeField, Min(0f)] private float charactersPerSecond = 40f;

    [Header("Slide Animation")]
    [Tooltip("Anchored position while visible. Leave (0, 0) to calculate it from the panel's hidden editor position.")]
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
    private Coroutine typingCoroutine;
    private Coroutine slideCoroutine;
    private bool isTyping;
    private bool isOpen;
    private GameObject panelToOpenAfterDialogue;

    private Vector2 HiddenAnchoredPosition =>
        shownAnchoredPosition + Vector2.up * hiddenOffset;

    private void Awake()
    {
        AutoAssignReferences();

        if (!ValidateReferences())
            return;

        panelRectTransform = dialoguePanel.GetComponent<RectTransform>();

        // In Town the brown panel is placed just above the screen in the editor.
        // When no explicit shown position is configured, derive it from there.
        if (shownAnchoredPosition == Vector2.zero)
        {
            shownAnchoredPosition = panelRectTransform.anchoredPosition
                - Vector2.up * hiddenOffset;
        }

        panelRectTransform.anchoredPosition = HiddenAnchoredPosition;

        if (panelClickAdvances)
        {
            panelButton = dialoguePanel.GetComponent<Button>();
            if (panelButton == null)
                panelButton = dialoguePanel.AddComponent<Button>();

            panelButton.transition = Selectable.Transition.None;
            panelButton.targetGraphic = dialoguePanel.GetComponent<Graphic>();
            panelButton.onClick.AddListener(AdvanceDialogue);
        }

        dialoguePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (panelButton != null)
            panelButton.onClick.RemoveListener(AdvanceDialogue);
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
        panelRectTransform = dialoguePanel.GetComponent<RectTransform>();
        panelRectTransform.anchoredPosition = HiddenAnchoredPosition;

        characterNameText.text = characterName;
        if (avatarImage != null)
        {
            avatarImage.sprite = avatar;
            avatarImage.enabled = avatar != null;
        }

        ShowCurrentLine();
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

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = StartCoroutine(SlidePanel(
            HiddenAnchoredPosition,
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

        dialoguePanel.SetActive(false);

        if (panelToActivate != null)
        {
            panelToActivate.SetActive(true);
            panelToActivate.transform.SetAsLastSibling();
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
