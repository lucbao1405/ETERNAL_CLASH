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
        dialoguePanel.SetActive(false);
    }

    public void OpenDialogue(string characterName, Sprite avatar, string[] newLines)
    {
        dialoguePanel.SetActive(true);

        characterNameText.text = characterName;
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
        if (lines != null && lines.Length > 0)
            dialogueText.text = lines[currentLine];
    }

    public void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
    }
}