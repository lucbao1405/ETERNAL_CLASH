using UnityEngine;

namespace EternalClash.UI
{
    /// <summary>
    /// Marks a runtime-built panel as closeable by the Android Back button.
    /// MobilePlatformController scans these alongside DialogueManager,
    /// ShopPanelAnimator and SmoothSlide when looking for the top panel.
    /// </summary>
    public class BackClosePanel : MonoBehaviour
    {
        public bool IsOpen => gameObject.activeInHierarchy;

        public void Close() => gameObject.SetActive(false);
    }
}
