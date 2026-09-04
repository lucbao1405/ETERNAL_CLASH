using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public GameObject dialoguePanel;
    public Image avatarImage;
    public TMP_Text characterNameText;
    public TMP_Text dialogueText;

    private string[] lines;
    private int currentLine;

    void Start()
    {
        if (dialoguePanel == null)
        {
            Debug.LogWarning("[DialogueManager] dialoguePanel chua duoc gan trong Inspector - dialogue se khong hien thi.");
            return;
        }

        dialoguePanel.SetActive(false);
    }

    public void OpenDialogue(string characterName, Sprite avatar, string[] newLines)
    {
        if (dialoguePanel == null)
        {
            Debug.LogWarning("[DialogueManager] dialoguePanel chua duoc gan trong Inspector - khong the mo dialogue.");
            return;
        }

        dialoguePanel.SetActive(true);

        if (characterNameText != null)
            characterNameText.text = characterName;

        if (avatarImage != null)
            avatarImage.sprite = avatar;

        lines = newLines;
        currentLine = 0;
        ShowCurrentLine();
    }

    public void NextLine()
    {
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
        if (dialogueText != null && lines != null && lines.Length > 0)
            dialogueText.text = lines[currentLine];
    }

    public void CloseDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
}