using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    public DialogueManager dialogueManager;
    public string characterName;
    public Sprite avatar;

    [TextArea(2, 5)]
    public string[] dialogueLines;

    public void ShowDialogue()
    {
        dialogueManager.OpenDialogue(
            characterName,
            avatar,
            dialogueLines
        );
    }
}