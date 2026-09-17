using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect))]
    public sealed class UIScrollSound : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private float minDragDistance = 3f;
        [SerializeField] private float cooldownSeconds = 0.08f;

        private Vector2 lastPosition;
        private float nextPlayTime;

        private void Awake()
        {
            scrollRect ??= GetComponent<ScrollRect>();
        }

        private void OnEnable()
        {
            if (scrollRect != null)
                lastPosition = scrollRect.normalizedPosition;
        }

        private void Update()
        {
            if (scrollRect == null || Time.unscaledTime < nextPlayTime)
                return;

            Vector2 current = scrollRect.normalizedPosition;
            float distance = Vector2.Distance(current, lastPosition);

            if (distance > minDragDistance * 0.01f)
            {
                EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.UIClick);
                nextPlayTime = Time.unscaledTime + cooldownSeconds;
            }

            lastPosition = current;
        }
    }
}
