using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.Tutorial
{
    /// <summary>Small utility for optional scene-authored tutorial locks.</summary>
    public sealed class TutorialLockManager : MonoBehaviour
    {
        private readonly Dictionary<Selectable, bool> initialInteractable = new Dictionary<Selectable, bool>();

        public void SetLocked(Selectable selectable, bool isLocked)
        {
            if (selectable == null)
                return;

            if (!initialInteractable.ContainsKey(selectable))
                initialInteractable.Add(selectable, selectable.interactable);
            selectable.interactable = !isLocked && initialInteractable[selectable];
        }

        public void RestoreAll()
        {
            foreach (KeyValuePair<Selectable, bool> entry in initialInteractable)
                if (entry.Key != null)
                    entry.Key.interactable = entry.Value;
            initialInteractable.Clear();
        }

        private void OnDestroy()
        {
            RestoreAll();
        }
    }
}
