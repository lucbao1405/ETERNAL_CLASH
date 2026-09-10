using UnityEngine;

namespace EternalClash.Dialogue
{
    [CreateAssetMenu(fileName = "TutorialDialogueData", menuName = "Eternal Clash/Dialogue/Tutorial Dialogue")]
    public sealed class TutorialDialogueData : ScriptableObject
    {
        [SerializeField] private string npcName;
        [SerializeField] private string dialogueId;
        [SerializeField, TextArea(2, 5)] private string[] lines;
        [SerializeField] private Sprite avatar;

        public string NpcName => npcName;
        public string DialogueId => dialogueId;
        public string[] Lines => lines;
        public Sprite Avatar => avatar;

        public bool IsValid => !string.IsNullOrWhiteSpace(npcName)
            && !string.IsNullOrWhiteSpace(dialogueId)
            && lines != null
            && lines.Length > 0;
    }
}
