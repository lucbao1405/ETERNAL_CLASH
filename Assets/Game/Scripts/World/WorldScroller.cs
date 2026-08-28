using UnityEngine;

namespace EternalClash.World
{
    public class WorldScroller : MonoBehaviour
    {
        [SerializeField] private float scrollSpeed = 3f;
        private bool scrolling = true;

        private float currentMultiplier = 1f;

        private void Update()
        {
            if (!scrolling)
                return;

            transform.position += Vector3.left * scrollSpeed * currentMultiplier * Time.deltaTime;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            currentMultiplier = multiplier;
            Debug.Log("[WORLD] Speed multiplier x" + multiplier);
        }

        public void ResetSpeed()
        {
            currentMultiplier = 1f;
            Debug.Log("[WORLD] Speed reset");
        }

        public void StopScroll()
        {
            scrolling = false;
        }

        public void ResumeScroll()
        {
            scrolling = true;
        }
    }
}
