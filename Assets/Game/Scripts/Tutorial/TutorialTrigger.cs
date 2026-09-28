using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.Tutorial
{
    public enum TutorialTriggerEvent
    {
        IronSwordSelected,
        UpgradePressed,
        BagOpened
    }

    /// <summary>Optional scene hook for UI whose name cannot be auto-detected.</summary>
    [DisallowMultipleComponent]
    public sealed class TutorialTrigger : MonoBehaviour
    {
        [SerializeField] private TutorialTriggerEvent tutorialEvent;
        [SerializeField] private Button button;

        private void Awake()
        {
            button ??= GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(NotifyTutorial);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(NotifyTutorial);
        }

        public void NotifyTutorial()
        {
            TutorialManager.Instance?.Notify(tutorialEvent);
        }
    }
}
